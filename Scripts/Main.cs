using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Godot;
using Newtonsoft.Json;
using ProceduralGeneration.Scripts.MapGeneration;
using Serilog;
using SDirectory = System.IO.Directory;
using SFile = System.IO.File;
using SPath = System.IO.Path;

namespace ProceduralGeneration.Scripts {
    public class Main : Spatial {
        #region Fields
#if DEBUG
        private readonly string _savesDirectory = SPath.GetFullPath("../Saves/");
        private readonly string _logsDirectory = SPath.GetFullPath("../");
#else
        private readonly string _savesDirectory = SPath.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Saves/");
        private readonly string _logsDirectory = AppDomain.CurrentDomain.BaseDirectory;
#endif
        private static readonly NodePath MapMenuConfig = "GUI/TabContainer/Map/VBoxContainer";
        private static readonly NodePath LandmassMenuConfig = "GUI/TabContainer/Landmass/GridContainer";
        private static readonly NodePath AdvancedMenuConfig = "GUI/TabContainer/Advanced/VBoxContainer";
        // private static readonly NodePath SystemMenuConfig = "GUI/TabContainer/System/GridContainer";
        private static readonly NodePath GeneralControls = "GUI/ControlPanel/CenterContainer/GridContainer";

        private readonly NodePath _persistenceContainer = $"{MapMenuConfig}/MapParametersGrid/PersistenceHBox";
        private readonly NodePath _octavesContainer = $"{MapMenuConfig}/MapParametersGrid/OctavesHBox";

        private readonly NodePath _snowButtonPath = $"{LandmassMenuConfig}/SnowButton";
        private readonly NodePath _stoneButtonPath = $"{LandmassMenuConfig}/StoneButton";
        private readonly NodePath _grassButtonPath = $"{LandmassMenuConfig}/GrassButton";
        private readonly NodePath _sandButtonPath = $"{LandmassMenuConfig}/SandButton";
        private readonly NodePath _colorButtonWindow = "GUI/TabContainer/Landmass/ColorPickWindow";

        private readonly NodePath _waterVisibility = $"{AdvancedMenuConfig}/ShowWaterCheckBox";
        private readonly NodePath _noiseMinimap = $"{AdvancedMenuConfig}/ShowNoisePreviewCheckBox";

        // private readonly NodePath _taskNum = $"{SystemMenuConfig}/ParallelNumSpinBox";

        private readonly NodePath _saveButton = $"{GeneralControls}/SaveButton";
        private readonly NodePath _loadButton = $"{GeneralControls}/LoadButton";
        private readonly NodePath _genMapButton = $"{GeneralControls}/GenerateMapButton";
        private readonly NodePath _clearButton = $"{GeneralControls}/ClearButton";
        private readonly NodePath _exitButton = $"{GeneralControls}/ExitButton";
        private readonly NodePath _openSavesFolderButton = $"{GeneralControls}/SavesFolderButton";

        private Label _persistenceValueLabel;
        private Label _octavesValueLabel;
        private Label _waterValueLabel;

        private MapGenerator _mapGen;
        private Pointer _pointer;
        private TextureRect _minimap;
        // private OptionButton _memoryUnit;

        private Button _snowButton;
        private Button _stoneButton;
        private Button _grassButton;
        private Button _sandButton;
        private Popup _colorPickerDialog;
        private ColorPicker _colorPicker;

        #endregion

        private bool ShowNoiseMinimap {
            get => _showNoiseMinimap;
            set {
                _showNoiseMinimap = value;
                if (value) {
                    var texture = new ImageTexture();
                    texture.CreateFromImage(_mapGen.GetNoiseImage());
                    texture.SetSizeOverride(_minimapScale);
                    _minimap.Visible     = true;
                    _minimap.Texture     = texture;
                    _minimap.RectMinSize = _minimapScale;
                }
                else {
                    _minimap.Texture?.Dispose();
                    _minimap.Visible = false;
                }
            }
        }

        private bool _showNoiseMinimap;
        private readonly Vector2 _minimapScale = new Vector2(256, 256);

        #region Godot Overrides

        public override void _Ready() {
            SetupLogger();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Log.Logger.Fatal(args.ExceptionObject as Exception, "Unhandled exception");
                GetTree().Quit();
            };

            OS.WindowMaximized  = true;
            OS.WindowFullscreen = true;
            OS.WindowResizable  = false;
            OS.WindowSize       = OS.GetScreenSize();

            try {
                _persistenceValueLabel = GetNode<Label>($"{_persistenceContainer}/PersistenceValueLabel");
                _octavesValueLabel     = GetNode<Label>($"{_octavesContainer}/OctavesValueLabel");
                _waterValueLabel       = GetNode<Label>($"{AdvancedMenuConfig}/WaterLabel");
                _snowButton            = GetNode<Button>($"{LandmassMenuConfig}/SnowButton");
                _stoneButton           = GetNode<Button>($"{LandmassMenuConfig}/StoneButton");
                _grassButton           = GetNode<Button>($"{LandmassMenuConfig}/GrassButton");
                _sandButton            = GetNode<Button>($"{LandmassMenuConfig}/SandButton");
                _mapGen                = GetChild<MapGenerator>(0);
                _minimap               = GetChild<TextureRect>(1);
                _pointer               = GetChild<Pointer>(4);
                // _memoryUnit            = GetNode<OptionButton>($"{SystemMenuConfig}/MemoryHBox/MemoryMapOptionButton");

                _colorPickerDialog = GetNode<Popup>(_colorButtonWindow);
                _colorPicker       = GetNode<ColorPicker>($"{_colorButtonWindow}/ColorPicker");

                Debug.Assert(_persistenceValueLabel != null, "Persistence Not Found");
                Debug.Assert(_octavesValueLabel != null, "Octaves Not Found");
                Debug.Assert(_mapGen != null, "MapGen Not Found");
                Debug.Assert(_minimap != null, "Minimap Not Found");
                Debug.Assert(_pointer != null, "Pointer Not Found");
                // Debug.Assert(_memoryUnit != null, "MemoryUnit Not Found");
                Debug.Assert(_snowButton != null, "Snow Button Not Found");
                Debug.Assert(_stoneButton != null, "Stone Button Not Found");
                Debug.Assert(_grassButton != null, "Grass Button Not Found");
                Debug.Assert(_sandButton != null, "Sand Button Not Found");
                Debug.Assert(_colorPickerDialog != null, "Color Picker Dialog Not Found");
                Debug.Assert(_colorPicker != null, "Color Picker Not Found");
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get node");
                throw;
            }

            // Dropdown populating

            // try {
            //     _memoryUnit.AddItem("KiB", 0);
            //     _memoryUnit.AddItem("MiB", 1);
            //     _memoryUnit.AddItem("GiB", 2);
            // }
            // catch (Exception ex) {
            //     Log.Logger.Error(ex, "Failed to populate dropdown");
            // }

            #region Signal Connection

            // Connection Input signals to GUI elements

            try {
                GetNode(_saveButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_SaveButton_pressed)
                );

                GetNode($"{_saveButton}/FileDialog").Connect(
                    "file_selected",
                    this,
                    nameof(_on_SaveFileDialog_file_selected)
                );

                // Config loading

                GetNode(_loadButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_LoadButton_pressed)
                );

                GetNode($"{_loadButton}/FileDialog").Connect(
                    "file_selected",
                    this,
                    nameof(_on_LoadFileDialog_file_selected)
                );

                GetNode($"{_persistenceContainer}/PersistenceSlider").Connect(
                    "value_changed",
                    this,
                    nameof(_on_PersistenceSlider_value_changed)
                );

                GetNode($"{_octavesContainer}/OctavesSlider").Connect(
                    "value_changed",
                    this,
                    nameof(_on_OctavesSlider_value_changed)
                );

                GetNode($"{MapMenuConfig}/MapParametersGrid/SeedInput").Connect(
                    "text_changed",
                    _mapGen,
                    nameof(_mapGen._on_SeedInput_text_changed)
                );

                GetNode(_genMapButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_GenerateMapButton_button_up)
                );

                GetNode(_waterVisibility).Connect(
                    "toggled",
                    this,
                    nameof(_on_ShowWaterCheckBox_toggled)
                );

                GetNode($"{AdvancedMenuConfig}/WaterSlider").Connect(
                    "value_changed",
                    this,
                    nameof(_on_WaterSlider_value_changed)
                );

                GetNode(_noiseMinimap).Connect(
                    "toggled",
                    this,
                    nameof(_on_ShowNoisePreviewCheckBox_toggled)
                );

                GetNode(_clearButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_ClearButton_pressed)
                );

                GetNode(_exitButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_ExitButton_pressed)
                );

                GetNode(_openSavesFolderButton).Connect(
                    "pressed",
                    this,
                    nameof(_on_SavesFolderButton_pressed)
                );

                GetNode(_snowButtonPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_SnowButton_pressed)
                );

                GetNode(_stoneButtonPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_StoneButton_pressed)
                );

                GetNode(_grassButtonPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_GrassButton_pressed)
                );

                GetNode(_sandButtonPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_SandButton_pressed)
                );
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to connect signals");
            }

            #endregion

            // Directories setup

            try {
                SDirectory.CreateDirectory(_savesDirectory);
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to create Saves director");
            }
        }

        public override void _Input(InputEvent @event) {
            if (!(@event is InputEventKey)) {
                return;
            }

            if (Input.IsKeyPressed((int)KeyList.Escape)) {
                GetTree().Quit();
            }
            else if (Input.IsKeyPressed((int)KeyList.Enter)) {
                _on_GenerateMapButton_button_up();
                GetTree().SetInputAsHandled();
            } else if (Input.IsKeyPressed((int)KeyList.R)) {
                _pointer.Reset(
                    _mapGen.Config.Width / 2,
                    _mapGen.Config.Height
                );
                GetTree().SetInputAsHandled();
            }
        }

        #endregion

        private void SetupLogger() {
            var builder = new LoggerConfiguration();

            SDirectory.CreateDirectory(_logsDirectory);

            builder
#if DEBUG
                .WriteTo.Console()
                .MinimumLevel.Debug()
#else
                .WriteTo.File(
                    path: SPath.Combine(_logsDirectory, "error_log.log"),
                    restrictedToMinimumLevel: LogEventLevel.Warning
                    )
#endif
                ;

            Log.Logger = builder.CreateLogger();
        }

        #region Signals

        private void _on_GenerateMapButton_button_up() {
            _mapGen.GenerateMap();
            if (VectorsAreClose(_pointer.Translation, Vector3.Zero)) {
                _pointer.Reset(
                    _mapGen.Config.Width / 2,
                    _mapGen.Config.Height
                );
            }
            _on_ShowNoisePreviewCheckBox_toggled(ShowNoiseMinimap);
        }

        private static bool VectorsAreClose(Vector3 left, Vector3 right) {
            return Mathf.Abs(left.x - right.x) < Mathf.Epsilon &&
                   Mathf.Abs(left.y - right.y) < Mathf.Epsilon &&
                   Mathf.Abs(left.z - right.z) < Mathf.Epsilon;
        }

        private void _on_ClearButton_pressed() {
            _mapGen.Clear();
            _pointer.Reset();
        }

        private void _on_SaveButton_pressed() {
            var fd = GetNode<FileDialog>($"{_saveButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(_savesDirectory);
            fd.PopupCentered();
        }

        private void _on_SaveFileDialog_file_selected(string path) {
            var mg   = GetChild<MapGenerator>(0);
            var json = JsonConvert.SerializeObject(mg.Config, Formatting.Indented);

            if (path.EndsWith("json") && !path.EndsWith(".json")) {
                path = path.Substring(0, path.Length - 4) + ".json";
            }

            if (SFile.Exists(path)) {
                SFile.Delete(path);
            }

            using (var file = SFile.CreateText(path)) {
                file.WriteLine(json);
            }
        }

        private void _on_LoadButton_pressed() {
            var fd = GetNode<FileDialog>($"{_loadButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(_savesDirectory);
            fd.PopupCentered();
        }

        private void _on_LoadFileDialog_file_selected(string path) {
            var json = SFile.ReadAllText(path);
            _mapGen.Config = JsonConvert.DeserializeObject<MapConfig>(json);
            Debug.Assert(_mapGen.Config != null, "_mapGen.Config != null");
            _pointer.Translation = new Vector3(
                // ReSharper disable once PossibleNullReferenceException
                (float)_mapGen.Config.Width / 2,
                0,
                (float)_mapGen.Config.Height / 2
            );
        }

        private void _on_ExitButton_pressed() {
            GetTree().Quit();
        }

        private void _on_SavesFolderButton_pressed() {
            OS.ShellOpen(_savesDirectory);
        }

        private void _on_PersistenceSlider_value_changed(float value) {
            _persistenceValueLabel.Text = value.ToString("N2");
        }

        private void _on_OctavesSlider_value_changed(float value) {
            _octavesValueLabel.Text = $"{(int)value}";
        }

        private void _on_ShowWaterCheckBox_toggled(bool buttonPressed) {
            _mapGen.ToggleWaterVisibility(buttonPressed);
        }

        private void _on_ShowNoisePreviewCheckBox_toggled(bool buttonPressed) {
            ShowNoiseMinimap = buttonPressed;
        }

        private void _on_WaterSlider_value_changed(float value) {
            Debug.Assert(0 <= value && value <= 1);
            _waterValueLabel.Text = $"Water Transparency: {value * 100,3}%";
            _mapGen?.SetWaterTransparency(value);
        }

        private void _on_SnowButton_pressed() => ConfigureColor(MeshSections.Snow);
        private void _on_StoneButton_pressed() => ConfigureColor(MeshSections.Stone);
        private void _on_GrassButton_pressed() => ConfigureColor(MeshSections.Grass);
        private void _on_SandButton_pressed() => ConfigureColor(MeshSections.Sand);

        private void ConfigureColor(MeshSections section) {
            BreakExistingConnections();

            _colorPickerDialog.Visible = false;
            _colorPicker.Color         = _mapGen.GetMeshColor(section) ?? new Color(1, 1, 1);

            _colorPicker.Connect(
                "color_changed",
                this,
                GetActionName(section)
            );

            _colorPickerDialog.Show();
        }

        private void BreakExistingConnections() {
            TryDisconnect(nameof(_on_SnowColor_changed));
            TryDisconnect(nameof(_on_StoneColor_changed));
            TryDisconnect(nameof(_on_GrassColor_changed));
            TryDisconnect(nameof(_on_SandColor_changed));

            void TryDisconnect(string name) {
                if (_colorPicker.IsConnected("color_changed", this, name)) {
                    _colorPicker.Disconnect("color_changed", this, name);
                }
            }
        }

        private void _on_SnowColor_changed(Color color) => ButtonColorChanged(MeshSections.Snow, color);
        private void _on_StoneColor_changed(Color color) => ButtonColorChanged(MeshSections.Stone, color);
        private void _on_GrassColor_changed(Color color) => ButtonColorChanged(MeshSections.Grass, color);
        private void _on_SandColor_changed(Color color) => ButtonColorChanged(MeshSections.Sand, color);

        private void ButtonColorChanged(MeshSections section, Color color) {
            GetMaterial(section)?.SetShaderParam("selected_color", color);
            _mapGen.SetMeshColor(section, color);
        }

        private ShaderMaterial GetMaterial(MeshSections section) {
            switch (section) {
                case MeshSections.Snow:  return _snowButton.Material as ShaderMaterial;
                case MeshSections.Stone: return _stoneButton.Material as ShaderMaterial;
                case MeshSections.Grass: return _grassButton.Material as ShaderMaterial;
                case MeshSections.Sand:  return _sandButton.Material as ShaderMaterial;
                default:                 throw new NotSupportedException($"Handle {section}");
            }
        }

        private Action<Color> GetAction(MeshSections section) {
            switch (section) {
                case MeshSections.Snow:  return _on_SnowColor_changed;
                case MeshSections.Stone: return _on_StoneColor_changed;
                case MeshSections.Grass: return _on_GrassColor_changed;
                case MeshSections.Sand:  return _on_SandColor_changed;
                default:                 throw new NotSupportedException($"Handle {section}");
            }
        }

        private string GetActionName(MeshSections section) {
            switch (section) {
                case MeshSections.Snow:  return nameof(_on_SnowColor_changed);
                case MeshSections.Stone: return nameof(_on_StoneColor_changed);
                case MeshSections.Grass: return nameof(_on_GrassColor_changed);
                case MeshSections.Sand:  return nameof(_on_SandColor_changed);
                default:                 throw new NotSupportedException($"Handle {section}");
            }
        }

        #endregion
    }
}
