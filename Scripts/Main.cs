using Godot;
using System;
using Newtonsoft.Json;
using ProceduralGeneration.MapGen;

using SDirectory = System.IO.Directory;
using SFile = System.IO.File;
using SPath = System.IO.Path;

namespace ProceduralGeneration
{
    public class Main : Spatial
    {
#if DEBUG
        public readonly string SavesDirectory = "../Saves/";
#else
        public readonly string SavesDirectory = SPath.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Saves/");
#endif
        [Export]
        private readonly NodePath _saveButton = "ControlPanel/VBoxContainer/SaveButton";
        [Export]
        private readonly NodePath _loadButton = "ControlPanel/VBoxContainer/LoadButton";
        [Export]
        private readonly NodePath _genMapButton = "ControlPanel/VBoxContainer/GenerateMapButton";

        public override void _Ready()
        {
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

            // MapGenerator

            GetNode(_genMapButton).Connect(
                "pressed",
                GetChild(0),
                nameof(MapGenerator._on_GenerateMapButton_button_up)
                );

            try
            {
                SDirectory.CreateDirectory(SavesDirectory);
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);
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

        #region Save and Load

        private void _on_SaveButton_pressed()
        {
            var fd = GetNode<FileDialog>($"{_saveButton}/FileDialog");
            fd.CurrentDir = SPath.GetFullPath(SavesDirectory);
            fd.PopupCentered();
        }

        private void _on_SaveFileDialog_file_selected(string path)
        {
            var mg = GetChild<MapGenerator>(0);
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
            fd.CurrentDir = SPath.GetFullPath(SavesDirectory);
            fd.PopupCentered();
        }

        private void _on_LoadFileDialog_file_selected(string path)
        {
            var mg = GetChild<MapGenerator>(0);
            var json = SFile.ReadAllText(path);
            GetNode<Label>("ControlPanel/VBoxContainer/LoadedFileLabel").Text = path;
            mg.Config = JsonConvert.DeserializeObject<MapConfig>(json);
        }

        #endregion
    }
}
