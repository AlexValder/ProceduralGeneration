using System;
using Godot;
using Newtonsoft.Json;
using ProceduralGeneration.Scripts.MapGenerator;
using Serilog;
using Serilog.Events;
using SDirectory = System.IO.Directory;
using SFile = System.IO.File;
using SPath = System.IO.Path;

namespace ProceduralGeneration.Scripts
{
    public class Main : Spatial
    {
        private const string LogFile = "error_log.log";
#if DEBUG
        private readonly string _savesDirectory = SPath.GetFullPath("../Saves/");
        private readonly string _logsDirectory = SPath.GetFullPath("../");
#else
        public readonly string _savesDirectory = SPath.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Saves/");
        public readonly string _logsDirectory = AppDomain.CurrentDomain.BaseDirectory;
#endif
        [Export] private static readonly NodePath MenuConfig = "ControlPanel/VBoxContainer";
        
        private readonly NodePath _saveButton = $"{MenuConfig}/SaveButton";
        private readonly NodePath _loadButton = $"{MenuConfig}/LoadButton";
        private readonly NodePath _genMapButton = $"{MenuConfig}/GenerateMapButton";
        private readonly NodePath _persistenceContainer = $"{MenuConfig}/MapParametersGrid/PersistenceHBox/";
        private readonly NodePath _octavesContainer = $"{MenuConfig}/MapParametersGrid/OctavesHBox/";

        private Label _persistenceValueLabel;
        private Label _octavesValueLabel;

        private MapGenerator.MapGenerator _mapGen;

        #region Godot Overrides

        public override void _Ready()
        {
            SetupLogger();

            OS.WindowMaximized = true;

            // Config saving

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

            _mapGen = GetChild<MapGenerator.MapGenerator>(0);

            GetNode($"{MenuConfig}/SeedInput").Connect(
                "text_changed",
                _mapGen,
                nameof(_mapGen._on_SeedInput_text_changed)
            );

            try
            {
                _persistenceValueLabel = GetNode<Label>($"{_persistenceContainer}/PersistenceValueLabel");
                _octavesValueLabel = GetNode<Label>($"{_octavesContainer}/OctavesValueLabel");
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to get node");
            }

            // MapGenerator

            GetNode(_genMapButton).Connect(
                "pressed",
                GetChild(0),
                nameof(MapGenerator.MapGenerator._on_GenerateMapButton_button_up)
                );

            try
            {
                SDirectory.CreateDirectory(_savesDirectory);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to create Saves director");
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey)
            {
                if (Input.IsKeyPressed((int)KeyList.Escape))
                {
                    GetTree().Quit();
                }
            }
        }

        #endregion

        private void SetupLogger()
        {
            var builder = new LoggerConfiguration();

            SDirectory.CreateDirectory(_logsDirectory);
            var fileName = SPath.Combine(_logsDirectory, LogFile);

            builder
#if DEBUG
                .WriteTo.Console()
                .MinimumLevel.Debug()
#endif
                .WriteTo.File(
                    path: fileName,
                    restrictedToMinimumLevel: LogEventLevel.Warning
                    );

            Log.Logger = builder.CreateLogger();
        }

        #region Save and Load

        private void _on_SaveButton_pressed()
        {
            var fd = GetNode<FileDialog>($"{_saveButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(_savesDirectory);
            fd.PopupCentered();
        }

        private void _on_SaveFileDialog_file_selected(string path)
        {
            var mg = GetChild<MapGenerator.MapGenerator>(0);
            var json = JsonConvert.SerializeObject(mg.Config, Formatting.Indented);

            if (path.EndsWith("json") && !path.EndsWith(".json"))
            {
                path = path.Substring(0, path.Length - 4) + ".json";
            }

            if (SFile.Exists(path))
            {
                SFile.Delete(path);
            }

            using (var file = SFile.CreateText(path))
            {
                file.WriteLine(json);
            }
        }

        private void _on_LoadButton_pressed()
        {
            var fd = GetNode<FileDialog>($"{_loadButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(_savesDirectory);
            fd.PopupCentered();
        }

        private void _on_LoadFileDialog_file_selected(string path)
        {
            var mg = GetChild<MapGenerator.MapGenerator>(0);
            var json = SFile.ReadAllText(path);
            GetNode<Label>("ControlPanel/VBoxContainer/LoadedFileLabel").Text = path;
            mg.Config = JsonConvert.DeserializeObject<MapConfig>(json);
        }
        
        private void _on_PersistenceSlider_value_changed(float value)
        {
            _persistenceValueLabel.Text = value.ToString("N2");
        }
        
        private void _on_OctavesSlider_value_changed(float value)
        {
            _octavesValueLabel.Text = $"{(int)value}";
        }

        #endregion
    }
}
