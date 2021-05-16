using System;
using System.Diagnostics;
using Godot;
using Newtonsoft.Json;
using ProceduralGeneration.Scripts.MapGeneration;
using Serilog;
using SPath = System.IO.Path;
using SFile = System.IO.File;

namespace ProceduralGeneration.Scripts {
    public class GuiHandler : Node {
        #region Fields
        private static readonly NodePath MapMenuConfig = "GUI/TabContainer/Map/VBoxContainer/";
        private static readonly NodePath AdvancedMenuConfig = "GUI/TabContainer/Advanced/VBoxContainer/";
        private static readonly NodePath SystemMenuConfig = "GUI/TabContainer/System/VBoxContainer/";
        private static readonly NodePath GeneralControls = "GUI/ControlPanel/VBoxContainer/";

        private readonly NodePath _persistenceContainer = $"{MapMenuConfig}/MapParametersGrid/PersistenceHBox";
        private readonly NodePath _octavesContainer = $"{MapMenuConfig}/MapParametersGrid/OctavesHBox";

        private readonly NodePath _snowButtonPath = $"{AdvancedMenuConfig}/GridContainer/SnowButton";
        private readonly NodePath _stoneButtonPath = $"{AdvancedMenuConfig}/GridContainer/StoneButton";
        private readonly NodePath _grassButtonPath = $"{AdvancedMenuConfig}/GridContainer/GrassButton";
        private readonly NodePath _sandButtonPath = $"{AdvancedMenuConfig}/GridContainer/SandButton";
        private readonly NodePath _colorButtonWindow = $"{AdvancedMenuConfig}/ColorPickWindow";

        private readonly NodePath _waterVisibility = $"{AdvancedMenuConfig}/ShowWaterCheckBox";
        private readonly NodePath _noiseMinimap = $"{AdvancedMenuConfig}/ShowNoisePreviewCheckBox";

        private readonly NodePath _fullscreenPath = $"{SystemMenuConfig}/FullscreenCheckButton";
        private readonly NodePath _selectPathContainer = $"{SystemMenuConfig}/SelectGrid/";

        private readonly NodePath _snowBorderPath = $"{AdvancedMenuConfig}/GridContainer/SnowSpinBox";
        private readonly NodePath _stoneBorderPath = $"{AdvancedMenuConfig}/GridContainer/StoneSpinBox";
        private readonly NodePath _grassBorderPath = $"{AdvancedMenuConfig}/GridContainer/GrassSpinBox";
        private readonly NodePath _resetDefaultPath = $"{AdvancedMenuConfig}/ResetButton";

        private readonly NodePath _saveButton = $"{GeneralControls}/GridContainer/SaveButton";
        private readonly NodePath _loadButton = $"{GeneralControls}/GridContainer/LoadButton";
        private readonly NodePath _genMapButton = $"{GeneralControls}/GridContainer/GenerateMapButton";
        private readonly NodePath _clearButton = $"{GeneralControls}/GridContainer/ClearButton";
        private readonly NodePath _exitButton = $"{GeneralControls}/GridContainer/ExitButton";
        private readonly NodePath _openSavesFolderButton = $"{GeneralControls}/GridContainer/SavesFolderButton";
        private readonly NodePath _helpButtonPath = $"{GeneralControls}/HelpButton";

        private Label _persistenceValueLabel;
        private Label _octavesValueLabel;
        private Label _waterValueLabel;

        private CheckButton _fullscreenCheckButton;

        private SpinBox _snowBorder;
        private SpinBox _stoneBorder;
        private SpinBox _grassBorder;

        private FileDialog _loadSaveDialog;

        private Button _snowButton;
        private Button _stoneButton;
        private Button _grassButton;
        private Button _sandButton;
        private Popup _colorPickerDialog;
        private ColorPicker _colorPicker;

        private Main _main;
        private AppSettings _appSettings;
        private MapGenerator _mapGen;
        private Pointer _pointer;
        #endregion

        public bool FullscreenCheckButton {
            get => _fullscreenCheckButton.Pressed;
            set => _fullscreenCheckButton.Pressed = value;
        }

        public void Initialize(in Main main, in AppSettings appSettings, in Pointer pointer, in MapGenerator mapGen) {
            _main        = main;
            _appSettings = appSettings;
            _pointer     = pointer;
            _mapGen      = mapGen;

            GetNodes();
            ConnectSignals();
        }

        private void GetNodes() {
            try {
                _persistenceValueLabel = GetNode<Label>($"{_persistenceContainer}/PersistenceValueLabel");
                _octavesValueLabel     = GetNode<Label>($"{_octavesContainer}/OctavesValueLabel");
                _waterValueLabel       = GetNode<Label>($"{AdvancedMenuConfig}/WaterLabel");
                _snowButton            = GetNode<Button>(_snowButtonPath);
                _stoneButton           = GetNode<Button>(_stoneButtonPath);
                _grassButton           = GetNode<Button>(_grassButtonPath);
                _sandButton            = GetNode<Button>(_sandButtonPath);
                _loadSaveDialog        = GetNode<FileDialog>($"{_loadButton}/FileDialog");
                _fullscreenCheckButton = GetNode<CheckButton>(_fullscreenPath);

                _colorPickerDialog = GetNode<Popup>(_colorButtonWindow);
                _colorPicker       = GetNode<ColorPicker>($"{_colorButtonWindow}/ColorPicker");

                _snowBorder  = GetNode<SpinBox>(_snowBorderPath);
                _stoneBorder = GetNode<SpinBox>(_stoneBorderPath);
                _grassBorder = GetNode<SpinBox>(_grassBorderPath);

                Debug.Assert(_persistenceValueLabel != null, "Persistence Not Found");
                Debug.Assert(_octavesValueLabel != null, "Octaves Not Found");
                Debug.Assert(_snowButton != null, "Snow Button Not Found");
                Debug.Assert(_stoneButton != null, "Stone Button Not Found");
                Debug.Assert(_grassButton != null, "Grass Button Not Found");
                Debug.Assert(_sandButton != null, "Sand Button Not Found");
                Debug.Assert(_colorPickerDialog != null, "Color Picker Dialog Not Found");
                Debug.Assert(_colorPicker != null, "Color Picker Not Found");
                Debug.Assert(_snowBorder != null, "Snow Border Not Found");
                Debug.Assert(_stoneBorder != null, "Stone Border Not Found");
                Debug.Assert(_grassBorder != null, "Grass Border Not Found");
                Debug.Assert(_fullscreenCheckButton != null, "Fullscreen Check Box Not Found");
            } catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get nodes");
            }
        }

        private void ConnectSignals() {
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

                _loadSaveDialog.Connect(
                    "file_selected",
                    this,
                    nameof(_on_LoadFileDialog_file_selected)
                );

                GetNode($"{_saveButton}/FileDialog").Connect(
                    "about_to_show",
                    this,
                    nameof(StopInputProcessing)
                );

                _loadSaveDialog.Connect(
                    "about_to_show",
                    this,
                    nameof(StopInputProcessing)
                );

                GetNode($"{_saveButton}/FileDialog").Connect(
                    "popup_hide",
                    this,
                    nameof(StartInputProcessing)
                );

                _loadSaveDialog.Connect(
                    "popup_hide",
                    this,
                    nameof(StartInputProcessing)
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

                GetNode($"{MapMenuConfig}/MapParametersGrid/SeedInput").Connect(
                    "focus_entered",
                    this,
                    nameof(StopInputProcessing)
                );

                GetNode($"{MapMenuConfig}/MapParametersGrid/SeedInput").Connect(
                    "focus_exited",
                    this,
                    nameof(StartInputProcessing)
                );

                GetNode(_genMapButton).Connect(
                    "pressed",
                    _main,
                    nameof(_main.GenerateMap)
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

                GetNode(_helpButtonPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_HelpButton_pressed)
                );

                _snowBorder.Connect(
                    "value_changed",
                    this,
                    nameof(_on_SnowBorderValue_changed)
                );

                _stoneBorder.Connect(
                    "value_changed",
                    this,
                    nameof(_on_StoneBorderValue_changed)
                );

                _grassBorder.Connect(
                    "value_changed",
                    this,
                    nameof(_on_GrassBorderValue_changed)
                );

                GetNode(_resetDefaultPath).Connect(
                    "pressed",
                    this,
                    nameof(_on_ResetToDefaults_pressed)
                );

                GetNode(_fullscreenPath).Connect(
                    "toggled",
                    this,
                    nameof(_on_FullscreenCheckBox_toggled)
                );

                GetNode($"{_selectPathContainer}/LogButton").Connect(
                    "pressed",
                    this,
                    nameof(_on_LogButton_pressed)
                );

                GetNode($"{SystemMenuConfig}/LogFileDialog").Connect(
                    "about_to_show",
                    this,
                    nameof(StopInputProcessing)
                );

                GetNode($"{SystemMenuConfig}/LogFileDialog").Connect(
                    "popup_hide",
                    this,
                    nameof(StartInputProcessing)
                );

                GetNode($"{SystemMenuConfig}/LogFileDialog").Connect(
                    "dir_selected",
                    this,
                    nameof(_on_LogPathFileDialog_dir_selected)
                );

                GetNode($"{_selectPathContainer}/SaveButton").Connect(
                    "pressed",
                    this,
                    nameof(_on_SelectSaveButton_pressed)
                );

                GetNode($"{SystemMenuConfig}/SavesFileDialog").Connect(
                    "about_to_show",
                    this,
                    nameof(StopInputProcessing)
                );

                GetNode($"{SystemMenuConfig}/SavesFileDialog").Connect(
                    "popup_hide",
                    this,
                    nameof(StartInputProcessing)
                );

                GetNode($"{SystemMenuConfig}/SavesFileDialog").Connect(
                    "dir_selected",
                    this,
                    nameof(_on_SavesFileDialog_dir_selected)
                );

                GetNode(_noiseMinimap).Connect(
                    "toggled",
                    this,
                    nameof(_on_ShowNoisePreviewCheckBox_toggled)
                );
            } catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to connect signals");
                throw;
            }
        }

                private void _on_ClearButton_pressed() {
            _mapGen.Clear();
            _pointer.Reset();
        }

        private void _on_SaveButton_pressed() {
            var fd = GetNode<FileDialog>($"{_saveButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(_main.SavesDirectory);
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
            _loadSaveDialog.CurrentDir = SPath.GetFullPath(_main.SavesDirectory);
            _loadSaveDialog.PopupCentered();
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

        private void StopInputProcessing() => Main.InputProcessing = false;

        private void StartInputProcessing() => Main.InputProcessing = true;

        private void _on_ExitButton_pressed() {
            GetTree().Quit();
        }

        private void _on_SavesFolderButton_pressed() {
            OS.ShellOpen(_main.SavesDirectory);
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

        internal void _on_ShowNoisePreviewCheckBox_toggled(bool buttonPressed) {
            _main.ShowNoiseMinimap = buttonPressed;
        }

        private void _on_WaterSlider_value_changed(float value) {
            Debug.Assert(0 <= value && value <= 1);
            _waterValueLabel.Text = $"Water Transparency: {value * 100,3}%";
            _mapGen?.SetWaterTransparency(value);
        }

        private void _on_HelpButton_pressed() {
            var help = GetNode<AcceptDialog>($"{_helpButtonPath}/AcceptDialog");
            help.PopupCentered();
        }

        private void _on_SnowButton_pressed() {
            ConfigureColor(ShaderSettings.MeshSections.Snow);
        }

        private void _on_StoneButton_pressed() {
            ConfigureColor(ShaderSettings.MeshSections.Stone);
        }

        private void _on_GrassButton_pressed() {
            ConfigureColor(ShaderSettings.MeshSections.Grass);
        }

        private void _on_SandButton_pressed() {
            ConfigureColor(ShaderSettings.MeshSections.Sand);
        }

        private void ConfigureColor(ShaderSettings.MeshSections section) {
            BreakExistingConnections();

            _colorPickerDialog.PopupCentered();
            _colorPicker.Color = _mapGen.GetMeshColor(section) ?? new Color(1, 1, 1);

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

        private void _on_SnowColor_changed(Color color) {
            ButtonColorChanged(ShaderSettings.MeshSections.Snow, color);
        }

        private void _on_StoneColor_changed(Color color) {
            ButtonColorChanged(ShaderSettings.MeshSections.Stone, color);
        }

        private void _on_GrassColor_changed(Color color) {
            ButtonColorChanged(ShaderSettings.MeshSections.Grass, color);
        }

        private void _on_SandColor_changed(Color color) {
            ButtonColorChanged(ShaderSettings.MeshSections.Sand, color);
        }

        private void ButtonColorChanged(ShaderSettings.MeshSections section, Color color) {
            GetMaterial(section)?.SetShaderParam("selected_color", color);
            _mapGen.SetMeshColor(section, color);
        }

        private ShaderMaterial GetMaterial(ShaderSettings.MeshSections section) {
            switch (section) {
                case ShaderSettings.MeshSections.Snow:  return _snowButton.Material as ShaderMaterial;
                case ShaderSettings.MeshSections.Stone: return _stoneButton.Material as ShaderMaterial;
                case ShaderSettings.MeshSections.Grass: return _grassButton.Material as ShaderMaterial;
                case ShaderSettings.MeshSections.Sand:  return _sandButton.Material as ShaderMaterial;
                default:                                throw new NotSupportedException($"Handle {section}");
            }
        }

        private static string GetActionName(ShaderSettings.MeshSections section) {
            switch (section) {
                case ShaderSettings.MeshSections.Snow:  return nameof(_on_SnowColor_changed);
                case ShaderSettings.MeshSections.Stone: return nameof(_on_StoneColor_changed);
                case ShaderSettings.MeshSections.Grass: return nameof(_on_GrassColor_changed);
                case ShaderSettings.MeshSections.Sand:  return nameof(_on_SandColor_changed);
                default:                                throw new NotSupportedException($"Handle {section}");
            }
        }

        private void _on_SnowBorderValue_changed(double value) {
            if (value <= _stoneBorder.Value) {
                _stoneBorder.Value = value - _stoneBorder.Step;
            }

            _mapGen.SetBorderValue(ShaderSettings.MeshSections.Snow, value);
        }

        private void _on_StoneBorderValue_changed(double value) {
            if (value <= _grassBorder.Value) {
                _grassBorder.Value = value - _grassBorder.Step;
            } else if (value >= _snowBorder.Value) {
                _snowBorder.Value = value + _snowBorder.Step;
            }

            _mapGen.SetBorderValue(ShaderSettings.MeshSections.Stone, value);
        }

        private void _on_GrassBorderValue_changed(double value) {
            if (value >= _stoneBorder.Value) {
                _stoneBorder.Value = value + _stoneBorder.Step;
            }

            _mapGen.SetBorderValue(ShaderSettings.MeshSections.Grass, value);
        }

        private void _on_ResetToDefaults_pressed() {
            _snowBorder.Value  = ShaderSettings.DefaultBorders[ShaderSettings.MeshSections.Snow];
            _stoneBorder.Value = ShaderSettings.DefaultBorders[ShaderSettings.MeshSections.Stone];
            _grassBorder.Value = ShaderSettings.DefaultBorders[ShaderSettings.MeshSections.Grass];

            foreach (var pair in ShaderSettings.DefaultColors) {
                GetMaterial(pair.Key).SetShaderParam("selected_color", pair.Value);
                _mapGen.SetMeshColor(pair.Key, pair.Value);
            }
        }

        private void _on_FullscreenCheckBox_toggled(bool buttonPressed) {
            _appSettings.SetValue(SettingsEntries.Fullscreen, buttonPressed);
            OS.WindowFullscreen = buttonPressed;
            OS.WindowMaximized  = true;
            OS.WindowPosition   = Vector2.Zero;
        }

        private void _on_LogButton_pressed() {
            var fd = GetNode<FileDialog>($"{SystemMenuConfig}/LogFileDialog");
            fd.CurrentDir = SPath.GetFullPath(_main.LogsDirectory);
            fd.PopupCentered();
        }

        private void _on_SelectSaveButton_pressed() {
            var fd = GetNode<FileDialog>($"{SystemMenuConfig}/SavesFileDialog");
            fd.CurrentDir = SPath.GetFullPath(_main.SavesDirectory);
            fd.PopupCentered();
        }

        private void _on_LogPathFileDialog_dir_selected(string dir) {
            _main.LogsDirectory = dir;
            _appSettings.SetValue(SettingsEntries.LogFolder, dir);
        }

        private void _on_SavesFileDialog_dir_selected(string dir) {
            _main.SavesDirectory = dir;
            _appSettings.SetValue(SettingsEntries.SavesFolder, dir);
        }
    }
}
