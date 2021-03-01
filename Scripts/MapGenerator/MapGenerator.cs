using Godot;
using System;
using Newtonsoft.Json;

namespace ProceduralGeneration.MapGen
{
    public class MapConfig
    {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int MinAmplitude { get; set; }
        public int MaxAmplitude { get; set; }

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
                _heigthSpinBox.Value = _config.Height = value.Height;
                _minSpinBox.Value = _config.MinAmplitude = value.MinAmplitude;
                _maxSpinBox.Value = _config.MaxAmplitude = value.MaxAmplitude;
                CreateTestMap();
            }
        }

        private readonly MapConfig _config = new MapConfig();

        private Random _random;
        private bool _shouldEmptySeed = false;

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
        private NodePath _heigthNodePath = new NodePath();
        private SpinBox _heigthSpinBox;

        [Export]
        private NodePath _minNodePath = new NodePath();
        private SpinBox _minSpinBox;

        [Export]
        private NodePath _maxNodePath = new NodePath();
        private SpinBox _maxSpinBox;

        #endregion

        public override void _Ready()
        {
            try
            {
                _seedLineEdit   = GetNode<LineEdit>(_seedNodePath);
                _widthSpinBox   = GetNode<SpinBox>(_widthNodePath);
                _heigthSpinBox  = GetNode<SpinBox>(_heigthNodePath);
                _minSpinBox     = GetNode<SpinBox>(_minNodePath);
                _maxSpinBox     = GetNode<SpinBox>(_maxNodePath);

                _meshInstance   = GetNode<MeshInstance>(_meshPath);
            }
            catch (Exception ex)
            {
                GD.Print($"Exception: {ex.Message}");
            }
        }

        private void CreateTestMap()
        {
            var map = new int[Config.Width, Config.Height];

            _random = new Random(Config.Seed);

            GD.Print("Started.");
            GD.Print($"Seed retrieved: {Config.Seed}");

            for (int j = 0; j < Config.Height; ++j)
            {
                for (int i = 0; i < Config.Width; ++i)
                {
                    map[i, j] = _random.Next(Config.MinAmplitude, Config.MaxAmplitude + 1);
                }
            }

            GD.Print($"Noise map ({Config.Width}x{Config.Height}) generated. Building polygons...");

            BuildPolygons(map);

            GD.Print("Finished.");
        }

        internal void _on_GenerateMapButton_button_up()
        {
            try
            {
                PopulateConfig();
            }
            catch (Exception ex)
            {
                GD.Print($"Exception in signal: {ex.Message}\n{ex.StackTrace}");
            }

            CreateTestMap();
        }

        private void BuildPolygons(int[,] map)
        {
            SurfaceTool st = new SurfaceTool();

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
            Config.Height = (int)_heigthSpinBox.Value;
            Config.MinAmplitude = (int)_minSpinBox.Value;
            Config.MaxAmplitude = (int)_maxSpinBox.Value;

            GD.Print($"{Config}");
        }
    }
}
