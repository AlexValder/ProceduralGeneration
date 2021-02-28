using Godot;
using System;
using System.Text;

namespace MapGenerator
{
    public class MapConfig
    {
        public int Seed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class MapGenerator : Node
    {
        private Random _random;
        private readonly MapConfig _config = new MapConfig();
        private bool _shouldEmptySeed = false;

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

        public override void _Ready()
        {
            try
            {
                _seedLineEdit = GetNode<LineEdit>(_seedNodePath);
                _widthSpinBox = GetNode<SpinBox>(_widthNodePath);
                _heigthSpinBox = GetNode<SpinBox>(_heigthNodePath);
                _meshInstance = GetNode<MeshInstance>(_meshPath);
            }
            catch (Exception ex)
            {
                GD.Print($"Exception: {ex.Message}");
            }
        }

        private void CreateTestMap()
        {
            var map = new int[_config.Width, _config.Height];

            GD.Print("Started.");

            GD.Print($"Seed retrieved: {_config.Seed}");

            for (int j = 0; j < _config.Height; ++j)
            {
                for (int i = 0; i < _config.Width; ++i)
                {
                    map[i, j] = _random.Next(0, 2);
                }
            }

            GD.Print($"Noise map ({_config.Width}x{_config.Height}) generated. Building polygons...");

            BuildPolygons(map);

            GD.Print("Finished.");
        }

        private void _on_GenerateMapButton_button_up()
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
                _config.Seed = _seedLineEdit.Text.GetHashCode();
                _random = new Random(_config.Seed);
                _shouldEmptySeed = false;
            }
            else
            {
                _config.Seed = (int)DateTime.UtcNow.Ticks;
                _random = new Random(_config.Seed);
                _seedLineEdit.Text = _config.Seed.ToString();
                _shouldEmptySeed = true;
            }

            _config.Width = (int)_widthSpinBox.Value;
            _config.Height = (int)_heigthSpinBox.Value;
        }
    }
}
