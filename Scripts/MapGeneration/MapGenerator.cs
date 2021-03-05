using System;
using System.Runtime.CompilerServices;
using Godot;
using Newtonsoft.Json;
using Serilog;

[assembly: InternalsVisibleTo("Main")]
namespace ProceduralGeneration.Scripts.MapGeneration
{
    public class MapConfig
    {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinAmplitude { get; set; }
        public int MaxAmplitude { get; set; }
        public float Scale { get; set; }
        public float Persistence { get; set; }
        public int Octaves { get; set; }
        public float Lacunarity { get; set; }

        public override string ToString()
            => JsonConvert.SerializeObject(this);
    }
    
    public class MapGenerator : Node
    {
        public MapConfig Config
        {
            get => _config;
            set
            {
                _seedLineEdit.Text = (_config.Seed = value.Seed).ToString();
                _widthSpinBox.Value = _config.Width = value.Width;
                _heightSpinBox.Value = _config.Height = value.Height;
                _minSpinBox.Value = _config.MinAmplitude = value.MinAmplitude;
                _maxSpinBox.Value = _config.MaxAmplitude = value.MaxAmplitude;
                _persistenceSlider.Value = _config.Persistence = value.Persistence;
                _octavesSlider.Value = _config.Octaves = value.Octaves;

                _noise.Seed = value.Seed;
                _noise.Lacunarity = 0.8f;
                _noise.Persistence = value.Persistence;
                _noise.Octaves = value.Octaves;

                CreateTestMap();
            }
        }

        private readonly MapConfig _config = new MapConfig();

        private readonly OpenSimplexNoise _noise = new OpenSimplexNoise();
        private bool _shouldEmptySeed;

        #region NodePaths and Nodes

        [Export]
        private NodePath _meshPath = new NodePath();
        private MeshInstance _meshInstance;

        [Export]
        private NodePath _seedNodePath = new NodePath();
        private LineEdit _seedLineEdit;

        [Export]
        private NodePath _widthNodePath = new NodePath();
        private SpinBox _widthSpinBox;

        [Export] private NodePath _heightNodePath = new NodePath();
        private SpinBox _heightSpinBox;

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

        #endregion

        #region Godot Overrides

        public override void _Ready()
        {
            try
            {
                _seedLineEdit        = GetNode<LineEdit>(_seedNodePath);
                _widthSpinBox        = GetNode<SpinBox>(_widthNodePath);
                _heightSpinBox       = GetNode<SpinBox>(_heightNodePath);
                _minSpinBox          = GetNode<SpinBox>(_minNodePath);
                _maxSpinBox          = GetNode<SpinBox>(_maxNodePath);
                _scaleSpinBox        = GetNode<SpinBox>(_scaleNodePath);
                _persistenceSlider   = GetNode<Slider>(_persistenceNodePath);
                _octavesSlider       = GetNode<Slider>(_octavesNodePath);
                _lacunaritySpinBox   = GetNode<SpinBox>(_lacunarityNodePath);

                _meshInstance        = GetNode<MeshInstance>(_meshPath);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to initialize MapGenerator");
            }
        }

        #endregion

        private void CreateTestMap()
        {
            Log.Logger.Debug("Started");
            Log.Logger.Debug("Seed retrieved: {Seed}", Config.Seed);
            Log.Logger.Debug("Config: {Config}", Config);
            
            var map = new float[Config.Width, Config.Height];

            var diff = Mathf.Abs(Config.MaxAmplitude - Config.MinAmplitude); 

            for (var i = 0; i < Config.Width; ++i)
            {
                for (var j = 0; j < Config.Height; ++j)
                {
                    map[i, j] = _noise.GetNoise2d(i / Config.Scale, j / Config.Scale) * diff + Config.MinAmplitude;
                }
            }

            Log.Logger.Debug("Noise map ({Width}x{Height}) generated. Building polygons...",
                Config.Width,
                Config.Height);

            BuildPolygons(map);

            Log.Logger.Debug("Finished");
        }

        public void GenerateMap()
        {
            try
            {
                PopulateConfig();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to populate config");
            }

            CreateTestMap();
        }

        private void BuildPolygons(in float[,] map)
        {
            // Scale "water"

            var waterMeshInstance = _meshInstance.GetChild<MeshInstance>(0);
            waterMeshInstance.Visible = true;
            waterMeshInstance.Scale = new Vector3(Config.Width, Config.Height, 1);
            waterMeshInstance.Translation = new Vector3(Config.Width / 2, 0, Config.Height / 2);
            
            var st = new SurfaceTool();

            st.Begin(Mesh.PrimitiveType.Triangles);

            // Generate "Land"
            
            for (var i = 0; i < map.GetLength(0) - 1; ++i)
            {
                for (var j = 0; j < map.GetLength(1) - 1; ++j)
                {
                    st.AddVertex(new Vector3(i, map[i, j], j));
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j], j));
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j + 1], j + 1));
                    
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j + 1], j + 1));
                    st.AddVertex(new Vector3(i, map[i, j + 1], j + 1));
                    st.AddVertex(new Vector3(i, map[i, j], j));
                }
            }
            
            st.GenerateNormals();
            st.Index();
            var mesh = st.Commit();
            var mat = new SpatialMaterial
            {
                AlbedoColor = new Color(0, .75f, .1f)
            };
            mesh.SurfaceSetMaterial(0, mat);
            _meshInstance.Mesh = mesh;
        }

        private void PopulateConfig()
        {
            if (_shouldEmptySeed)
            {
                _seedLineEdit.Clear();
            }

            if (!_seedLineEdit.Text.Empty())
            {
                Config.Seed = _seedLineEdit.Text.GetHashCode();
                _shouldEmptySeed = false;
            }
            else
            {
                Config.Seed = new Random((int)DateTime.UtcNow.Ticks).Next();
                _seedLineEdit.Text = Config.Seed.ToString();
                _shouldEmptySeed = true;
            }

            Config.Width = (int)_widthSpinBox.Value;
            Config.Height = (int)_heightSpinBox.Value;
            Config.MinAmplitude = (int)_minSpinBox.Value;
            Config.MaxAmplitude = (int)_maxSpinBox.Value;
            Config.Scale = (float)_scaleSpinBox.Value;
            Config.Persistence = (float)_persistenceSlider.Value;
            Config.Octaves = (int)_octavesSlider.Value;
            Config.Lacunarity = (float)_lacunaritySpinBox.Value;
            
            _noise.Seed = Config.Seed;
            _noise.Octaves = Config.Octaves;
            _noise.Lacunarity = Config.Lacunarity;
            _noise.Persistence = Config.Persistence;
        }
        
        internal void _on_SeedInput_text_changed(string newSeed)
        {
            var oldSeed = Config.Seed;
            try
            {
                Config.Seed = int.Parse(newSeed);
                _noise.Seed = Config.Seed;
                _shouldEmptySeed = false;
            }
            catch (Exception ex)
            {
                Config.Seed = oldSeed;
                _noise.Seed = oldSeed;
                _shouldEmptySeed = true;
                Log.Logger.Error(ex, "Failed to parse seed");
            }
        }
    }
}
