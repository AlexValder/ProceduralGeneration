using System;
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
#if DEBUG
        private readonly string _savesDirectory = SPath.GetFullPath("../Saves/");
        private readonly string _logsDirectory = SPath.GetFullPath("../");
#else
        private readonly string _savesDirectory = SPath.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Saves/");
        private readonly string _logsDirectory = AppDomain.CurrentDomain.BaseDirectory;
#endif
        private static readonly NodePath MapMenuConfig = "GUI/TabContainer/Map/VBoxContainer/";
        private static readonly NodePath AdvancedMenuConfig = "GUI/TabContainer/Advanced/VBoxContainer/";
        private static readonly NodePath SystemMenuConfig = "GUI/TabContainer/System/GridContainer/";
        private static readonly NodePath GeneralControls = "GUI/ControlPanel/CenterContainer/GridContainer/";

        private readonly NodePath _persistenceContainer = $"{MapMenuConfig}/MapParametersGrid/PersistenceHBox/";
        private readonly NodePath _octavesContainer = $"{MapMenuConfig}/MapParametersGrid/OctavesHBox/";

        private readonly NodePath _waterVisibility = $"{AdvancedMenuConfig}/ShowWaterCheckBox";
        private readonly NodePath _noiseMinimap = $"{AdvancedMenuConfig}/ShowNoisePreviewCheckBox";

        private readonly NodePath _taskNum = $"{SystemMenuConfig}/ParallelNumSpinBox";

        private readonly NodePath _saveButton = $"{GeneralControls}/SaveButton";
        private readonly NodePath _loadButton = $"{GeneralControls}/LoadButton";
        private readonly NodePath _genMapButton = $"{GeneralControls}/GenerateMapButton";
        private readonly NodePath _clearButton = $"{GeneralControls}/ClearButton";
        private readonly NodePath _exitButton = $"{GeneralControls}/ExitButton";
        private readonly NodePath _openSavesFolderButton = $"{GeneralControls}/SavesFolderButton";

        private Label _persistenceValueLabel;
        private Label _octavesValueLabel;

        private MapGenerator _mapGen;
        private MeshInstance _pointer;
        private TextureRect _minimap;
        private OptionButton _memoryUnit;

        private bool ShowWater {
            get => _showWater;
            set {
                _showWater = value;
                _mapGen.ToggleWaterVisibility(value);
            }
        }

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

        private bool _showWater;
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
                _mapGen                = GetChild<MapGenerator>(0);
                _minimap               = GetChild<TextureRect>(1);
                _pointer               = GetChild<MeshInstance>(5);
                _memoryUnit            = GetNode<OptionButton>($"{SystemMenuConfig}/MemoryHBox/MemoryMapOptionButton");
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get node");
                throw;
            }

            // Dropdown populating

            try {
                _memoryUnit.AddItem("KiB", 0);
                _memoryUnit.AddItem("MiB", 1);
                _memoryUnit.AddItem("GiB", 2);
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to populate dropdown");
            }

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
            _pointer.Translation = new Vector3(
                // ReSharper disable PossibleLossOfFraction
                _mapGen.Config.Width / 2,
                0,
                _mapGen.Config.Height / 2
                // ReSharper restore PossibleLossOfFraction
            );
            _on_ShowNoisePreviewCheckBox_toggled(ShowNoiseMinimap);
        }

        private void _on_ClearButton_pressed() {
            _mapGen.Clear();
            _pointer.Translation = Vector3.Zero;
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
            var mg   = GetChild<MapGenerator>(0);
            var json = SFile.ReadAllText(path);
            mg.Config = JsonConvert.DeserializeObject<MapConfig>(json);
            Debug.Assert(mg.Config != null, "mg.Config != null");
            _pointer.Translation = new Vector3(
                (float)mg.Config.Width / 2,
                0,
                (float)mg.Config.Height / 2
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
            ShowWater = buttonPressed;
        }

        private void _on_ShowNoisePreviewCheckBox_toggled(bool buttonPressed) {
            ShowNoiseMinimap = buttonPressed;
        }

        #endregion
    }
}