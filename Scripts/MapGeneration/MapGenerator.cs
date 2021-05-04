using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;
using Serilog;
#if DEBUG
using Stopwatch = System.Diagnostics.Stopwatch;
#endif

[assembly: InternalsVisibleTo("Main")]

namespace ProceduralGeneration.Scripts.MapGeneration {
    public class MapConfig {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Tesselation { get; set; }
        public float MinAmplitude { get; set; }
        public float MaxAmplitude { get; set; }
        public float Scale { get; set; }
        public float Persistence { get; set; }
        public int Octaves { get; set; }
        public float Lacunarity { get; set; }
        public Correction Correction { get; set; } = new Correction();

        public override string ToString() {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class MapGenerator : Node {
        private readonly MapConfig _config = new MapConfig();

        private readonly OpenSimplexNoise _noise = new OpenSimplexNoise();
        private bool _shouldEmptySeed;
        private bool _toggleWater = true;
        private float _waterTransparency = 0.8f;

        public MapConfig Config {
            get => _config;
            set {
                _seedLineEdit.Text        = (_config.Seed = value.Seed).ToString();
                _widthSpinBox.Value       = _config.Width        = value.Width;
                _heightSpinBox.Value      = _config.Height       = value.Height;
                _tesselationSpinBox.Value = _config.Tesselation  = value.Tesselation;
                _minSpinBox.Value         = _config.MinAmplitude = value.MinAmplitude;
                _maxSpinBox.Value         = _config.MaxAmplitude = value.MaxAmplitude;
                _scaleSpinBox.Value       = _config.Scale        = value.Scale;
                _persistenceSlider.Value  = _config.Persistence  = value.Persistence;
                _octavesSlider.Value      = _config.Octaves      = value.Octaves;
                _lacunaritySpinBox.Value  = _config.Lacunarity   = value.Lacunarity;
                _config.Correction        = value.Correction;

                _correctionTypeOptionButton.Selected = (int)_config.Correction.Type;

                _noise.Seed        = value.Seed;
                _noise.Lacunarity  = value.Lacunarity;
                _noise.Persistence = value.Persistence;
                _noise.Octaves     = value.Octaves;

                CreateMap();
            }
        }

        #region Godot Overrides

        public override void _Ready() {
            try {
                _seedLineEdit               = GetNode<LineEdit>(_seedNodePath);
                _widthSpinBox               = GetNode<SpinBox>(_widthNodePath);
                _heightSpinBox              = GetNode<SpinBox>(_heightNodePath);
                _tesselationSpinBox         = GetNode<SpinBox>(_tesselationNodePath);
                _minSpinBox                 = GetNode<SpinBox>(_minNodePath);
                _maxSpinBox                 = GetNode<SpinBox>(_maxNodePath);
                _scaleSpinBox               = GetNode<SpinBox>(_scaleNodePath);
                _persistenceSlider          = GetNode<Slider>(_persistenceNodePath);
                _octavesSlider              = GetNode<Slider>(_octavesNodePath);
                _lacunaritySpinBox          = GetNode<SpinBox>(_lacunarityNodePath);
                _correctionTypeOptionButton = GetNode<OptionButton>(_correctionTypeNodePath);

                _correctionTypeOptionButton.AddItem("Linear", 0);
                _correctionTypeOptionButton.AddItem("Square", 1);
                _correctionTypeOptionButton.AddItem("Cubic", 2);

                _meshInstance      = GetNode<MeshInstance>(_meshPath);
                _waterMeshInstance = _meshInstance.GetChild<MeshInstance>(0);
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to initialize MapGenerator");
            }
        }

        #endregion

        private void CreateMap() {
            // TODO: Limit max size?
            var map = new float[Config.Width * Config.Tesselation, Config.Height * Config.Tesselation];
            var x   = map.GetLength(0);
            var y   = map.GetLength(1);

#if DEBUG
            var sw = new Stopwatch();

            sw.Start();
#endif
            Task.WaitAll(
                Task.Run(() => FillMapParallel(0, x / 2, 0, y / 2, map)),
                Task.Run(() => FillMapParallel(x / 2, x, 0, y / 2, map)),
                Task.Run(() => FillMapParallel(0, x / 2, y / 2, y, map)),
                Task.Run(() => FillMapParallel(x / 2, x, y / 2, y, map))
            );

            var minNoise = map.Cast<float>().Min();
            var maxNoise = map.Cast<float>().Max();

            Log.Logger.Debug("Max = {Max}, Min = {Min}", maxNoise, minNoise);

            Task.WaitAll(
                Task.Run(() => CreateMapParallel(0, x / 2, 0, y / 2, map, minNoise, maxNoise)),
                Task.Run(() => CreateMapParallel(x / 2, x, 0, y / 2, map, minNoise, maxNoise)),
                Task.Run(() => CreateMapParallel(0, x / 2, y / 2, y, map, minNoise, maxNoise)),
                Task.Run(() => CreateMapParallel(x / 2, x, y / 2, y, map, minNoise, maxNoise))
            );
#if DEBUG
            sw.Stop();
            Log.Logger.Debug("Spent time calculating: {Elapsed}s", sw.Elapsed.TotalSeconds);

            var newMin = map.Cast<float>().Min();
            var newMax = map.Cast<float>().Max();

            Log.Logger.Debug("Final: from {Min} to {Max}", newMin, newMax);
#endif
            BuildPolygons(map);
        }

        private void FillMapParallel(int iBegin, int iEnd, int jBegin, int jEnd, float[,] map) {
            for (var i = iBegin; i < iEnd; ++i)
            for (var j = jBegin; j < jEnd; ++j) {
                map[i, j] = _noise.GetNoise2d(
                    i / (Config.Scale * Config.Tesselation),
                    j / (Config.Scale * Config.Tesselation)
                );
            }
        }

        private void CreateMapParallel(int iBegin, int iEnd, int jBegin, int jEnd, float[,] map, float min, float max) {
            var move   = (max + min) / 2;
            var rel    = 2 / (max - min);
            var radius = Mathf.Abs(Config.MaxAmplitude - Config.MinAmplitude) / 2;
            var diff   = Config.MaxAmplitude - radius;

            Log.Logger.Debug("Move: {Move}, Rel: {Rel}, Rad: {Radius}, Diff: {Diff}",
                             move, rel, radius, diff);

            for (var i = iBegin; i < iEnd; ++i)
            for (var j = jBegin; j < jEnd; ++j) {
                map[i, j] = Config.Correction.GetCorrection(
                    (map[i, j] - move) * rel
                ) * radius + diff;
            }
        }

        public void GenerateMap() {
            try {
                PopulateConfig();
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to populate config");
            }

            CreateMap();
        }

        private void BuildPolygons(in float[,] map) {
            // Scale "water"
            _waterMeshInstance.Visible = _toggleWater;
            _waterMeshInstance.Scale   = new Vector3(Config.Width, Config.Height, 1);
            _waterMeshInstance.Translation = new Vector3(
                (float)Config.Width / 2,
                0,
                (float)Config.Height / 2
            );

            var st = new SurfaceTool();

            st.Begin(Mesh.PrimitiveType.Triangles);

            // Generate "Land"

            for (var i = 0; i < map.GetLength(0) - 1; ++i)
            for (var j = 0; j < map.GetLength(1) - 1; ++j) {
                st.AddVertex(new Vector3(
                                 (float)i / Config.Tesselation,
                                 map[i, j],
                                 (float)j / Config.Tesselation
                             ));
                st.AddVertex(new Vector3(
                                 (float)(i + 1) / Config.Tesselation,
                                 map[i + 1, j],
                                 (float)j / Config.Tesselation
                             ));
                st.AddVertex(new Vector3(
                                 (float)(i + 1) / Config.Tesselation,
                                 map[i + 1, j + 1],
                                 (float)(j + 1) / Config.Tesselation
                             ));

                st.AddVertex(new Vector3(
                                 (float)(i + 1) / Config.Tesselation,
                                 map[i + 1, j + 1],
                                 (float)(j + 1) / Config.Tesselation
                             ));
                st.AddVertex(new Vector3(
                                 (float)i / Config.Tesselation,
                                 map[i, j + 1],
                                 (float)(j + 1) / Config.Tesselation
                             ));
                st.AddVertex(new Vector3(
                                 (float)i / Config.Tesselation,
                                 map[i, j],
                                 (float)j / Config.Tesselation
                             ));
            }

            st.GenerateNormals();
            st.Index();
            var mesh = st.Commit();
            var mat  = GD.Load<ShaderMaterial>("Resources/land.tres");
            mesh.SurfaceSetMaterial(0, mat);
            _meshInstance.Mesh = mesh;
        }

        private void PopulateConfig() {
            if (_shouldEmptySeed) {
                _seedLineEdit.Clear();
            }

            if (!_seedLineEdit.Text.Empty()) {
                Config.Seed = int.TryParse(_seedLineEdit.Text, out var seed) ? seed : _seedLineEdit.Text.GetHashCode();
                _shouldEmptySeed = false;
            }
            else {
                Config.Seed        = new Random((int)DateTime.UtcNow.Ticks).Next();
                _seedLineEdit.Text = Config.Seed.ToString();
                _shouldEmptySeed   = true;
            }

            Config.Width           = (int)_widthSpinBox.Value;
            Config.Height          = (int)_heightSpinBox.Value;
            Config.Tesselation     = (int)_tesselationSpinBox.Value;
            Config.MinAmplitude    = (float)_minSpinBox.Value;
            Config.MaxAmplitude    = (float)_maxSpinBox.Value;
            Config.Scale           = (float)_scaleSpinBox.Value;
            Config.Persistence     = (float)_persistenceSlider.Value;
            Config.Octaves         = (int)_octavesSlider.Value;
            Config.Lacunarity      = (float)_lacunaritySpinBox.Value;
            Config.Correction.Type = (CorrectionType)_correctionTypeOptionButton.Selected;

            _noise.Seed        = Config.Seed;
            _noise.Octaves     = Config.Octaves;
            _noise.Lacunarity  = Config.Lacunarity;
            _noise.Persistence = Config.Persistence;
        }

        internal void _on_SeedInput_text_changed(string newSeed) {
            var oldSeed = Config.Seed;
            try {
                Config.Seed      = int.TryParse(newSeed, out var seed) ? seed : newSeed.GetHashCode();
                _noise.Seed      = Config.Seed;
                _shouldEmptySeed = false;
            }
            catch (Exception ex) {
                Config.Seed      = oldSeed;
                _noise.Seed      = oldSeed;
                _shouldEmptySeed = true;
                Log.Logger.Error(ex, "Failed to parse seed");
            }
        }

        public void Clear() {
            _meshInstance.Mesh.Dispose();
            _meshInstance.Mesh         = null;
            _waterMeshInstance.Visible = false;
        }

        public void ToggleWaterVisibility(bool visible) {
            if (_meshInstance.Mesh == null) {
                return;
            }

            _waterMeshInstance.Visible = visible;
            _toggleWater               = visible;
        }

        public void SetWaterTransparency(float value) {
            _waterTransparency = value;

            if (_waterMeshInstance == null || !_waterMeshInstance.Visible) {
                return;
            }

            var mat = _waterMeshInstance.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
            mat?.SetShaderParam("alpha", _waterTransparency);
        }

        public Image GetNoiseImage() {
            return _noise.GetImage(Config.Width, Config.Height);
        }

        #region Mesh Colors

        internal void SetMeshColor(MeshSections section, Color color) {
            var mat = _meshInstance.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
            try {
                mat?.SetShaderParam($"{section.ToString().ToLower()}_color", color);
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to set land shader parameter");
            }
        }

        internal Color? GetMeshColor(MeshSections section) {
            var mat = _meshInstance.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
            try {
                var obj = mat?.GetShaderParam($"{section.ToString().ToLower()}_color");
                if (obj is Color color) {
                    return color;
                }
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get land shader parameter");
            }
            return null;
        }

        internal void SetBorderValue(MeshSections section, double value) {
            var mat = _meshInstance.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
            try {
                mat?.SetShaderParam($"{section.ToString().ToLower()}_value", value);
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to set land shader parameter");
            }
        }

        internal double? GetBorderValue(MeshSections section) {
            var mat = _meshInstance.Mesh?.SurfaceGetMaterial(0) as ShaderMaterial;
            try {
                var obj = mat?.GetShaderParam($"{section.ToString().ToLower()}_value");
                if (obj is double value) {
                    return value;
                }
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get land shader parameter");
            }
            return null;
        }

        #endregion

        #region NodePaths and Nodes

        [Export] private NodePath _meshPath = new NodePath();
        private MeshInstance _meshInstance;
        private MeshInstance _waterMeshInstance;

        [Export] private NodePath _seedNodePath = new NodePath();
        private LineEdit _seedLineEdit;

        [Export] private NodePath _widthNodePath = new NodePath();
        private SpinBox _widthSpinBox;

        [Export] private NodePath _heightNodePath = new NodePath();
        private SpinBox _heightSpinBox;

        [Export] private NodePath _tesselationNodePath = new NodePath();
        private SpinBox _tesselationSpinBox;

        [Export] private NodePath _minNodePath = new NodePath();
        private SpinBox _minSpinBox;

        [Export] private NodePath _maxNodePath = new NodePath();
        private SpinBox _maxSpinBox;

        [Export] private NodePath _scaleNodePath = new NodePath();
        private SpinBox _scaleSpinBox;

        [Export] private NodePath _persistenceNodePath = new NodePath();
        private Slider _persistenceSlider;

        [Export] private NodePath _octavesNodePath = new NodePath();
        private Slider _octavesSlider;

        [Export] private NodePath _lacunarityNodePath = new NodePath();
        private SpinBox _lacunaritySpinBox;

        [Export] private NodePath _correctionTypeNodePath = new NodePath();
        private OptionButton _correctionTypeOptionButton;

        #endregion
    }
}
