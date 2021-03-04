using System;
using Godot;
using Newtonsoft.Json;
using Serilog;

namespace ProceduralGeneration.Scripts.MapGenerator
{
    public class MapConfig
    {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinAmplitude { get; set; }
        public int MaxAmplitude { get; set; }
        public float Scale { get; set; }

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
                CreateTestMap();
            }
        }

        private readonly MapConfig _config = new MapConfig();

        private Random _random;
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

        [Export]
        private NodePath _heightNodePath = new NodePath();
        private SpinBox _heightSpinBox;

        [Export]
        private NodePath _minNodePath = new NodePath();
        private SpinBox _minSpinBox;

        [Export]
        private NodePath _maxNodePath = new NodePath();
        private SpinBox _maxSpinBox;

        [Export]
        private NodePath _scaleNodePath = new NodePath();
        private SpinBox _scaleSpinBox;

        #endregion

        #region Godot Overrides

        public override void _Ready()
        {
            try
            {
                _seedLineEdit   = GetNode<LineEdit>(_seedNodePath);
                _widthSpinBox   = GetNode<SpinBox>(_widthNodePath);
                _heightSpinBox  = GetNode<SpinBox>(_heightNodePath);
                _minSpinBox     = GetNode<SpinBox>(_minNodePath);
                _maxSpinBox     = GetNode<SpinBox>(_maxNodePath);
                _scaleSpinBox   = GetNode<SpinBox>(_scaleNodePath);

                _meshInstance   = GetNode<MeshInstance>(_meshPath);
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

            var map = new float[Config.Width, Config.Height];

            _random = new Random(Config.Seed);
            var tmp = new OpenSimplexNoise
            {
                Seed = Config.Seed
            };

            for (int i = 0; i < Config.Width; ++i)
            {
                for (int j = 0; j < Config.Height; ++j)
                {
                    map[i, j] = _random.Next(Config.MinAmplitude, Config.MaxAmplitude + 1);
                    map[i, j] *= tmp.GetNoise2d(i, j) * Config.Scale;
                }
                Console.WriteLine();
            }

            Log.Logger.Debug("Noise map ({Width}x{Height}) generated. Building polygons...",
                Config.Width,
                Config.Height);

            BuildPolygons(map);

            Log.Logger.Debug("Finished");
        }

        internal void _on_GenerateMapButton_button_up()
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

        private void BuildPolygons(float[,] map)
        {
            var st = new SurfaceTool();

            st.Begin(Mesh.PrimitiveType.Triangles);

            for (int i = 0; i < map.GetLength(0) - 1; ++i)
            {
                for (int j = 0; j < map.GetLength(1) - 1; ++j)
                {
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j + 1], j + 1));
                    st.AddVertex(new Vector3(i, map[i, j + 1], j + 1));
                    st.AddVertex(new Vector3(i, map[i, j], j));

                    st.AddVertex(new Vector3(i, map[i, j], j));
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j], j));
                    st.AddVertex(new Vector3(i + 1, map[i + 1, j + 1], j + 1));
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
                Config.Seed = (int)DateTime.UtcNow.Ticks;
                _seedLineEdit.Text = Config.Seed.ToString();
                _shouldEmptySeed = true;
            }

            Config.Width = (int)_widthSpinBox.Value;
            Config.Height = (int)_heightSpinBox.Value;
            Config.MinAmplitude = (int)_minSpinBox.Value;
            Config.MaxAmplitude = (int)_maxSpinBox.Value;
            Config.Scale = (float)_scaleSpinBox.Value;
        }
    }
}
