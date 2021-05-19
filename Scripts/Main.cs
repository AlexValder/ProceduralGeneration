using System;
using System.Diagnostics;
using Godot;
using ProceduralGeneration.Scripts.MapGeneration;
using Serilog;
#if !DEBUG
using Serilog.Events;
#endif
using SDirectory = System.IO.Directory;
using SFile = System.IO.File;
using SPath = System.IO.Path;

namespace ProceduralGeneration.Scripts {
    public class Main : Spatial {
        private MapGenerator _mapGen;
        private Pointer _pointer;
        private TextureRect _minimap;

        public bool ShowNoiseMinimap {
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
                } else {
                    _minimap.Texture?.Dispose();
                    _minimap.Visible = false;
                }
            }
        }

        public static bool InputProcessing { get; set; } = true;

        public string LogsDirectory {
            get => _logsDirectory;
            set => _logsDirectory = value;
        }

        public string SavesDirectory {
            get => _savesDirectory;
            set => _savesDirectory = value;
        }

        private bool _showNoiseMinimap;
        private readonly Vector2 _minimapScale = new Vector2(256, 256);
        private string _logsDirectory;
        private string _savesDirectory;
        private readonly AppSettings _appSettings = new AppSettings();
        private readonly GuiHandler _guiHandler = new GuiHandler();

        #region Godot Overrides

        public override void _Ready() {
            SetupLogger();
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                Log.Logger.Fatal(args.ExceptionObject as Exception, "Unhandled exception");
                GetTree().Quit();
            };

            _appSettings.GetValue(SettingsEntries.Fullscreen, out bool fs);
            _appSettings.GetValue(SettingsEntries.LogFolder, out _logsDirectory);
            _appSettings.GetValue(SettingsEntries.SavesFolder, out _savesDirectory);

            OS.WindowMaximized  = true;
            OS.WindowPosition   = Vector2.Zero;
            OS.WindowFullscreen = fs;

            try {
                _mapGen  = GetChild<MapGenerator>(0);
                _minimap = GetChild<TextureRect>(1);
                _pointer = GetChild<Pointer>(4);

                Debug.Assert(_mapGen != null, "MapGen Not Found");
                Debug.Assert(_minimap != null, "Minimap Not Found");
                Debug.Assert(_pointer != null, "Pointer Not Found");
            } catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to get node");
                throw;
            }

            _guiHandler.Initialize(this, _appSettings, _pointer, _mapGen);
            _guiHandler.FullscreenCheckButton = fs;


            try {
                SDirectory.CreateDirectory(_savesDirectory);
            } catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to create Saves director");
            }
        }

        public override void _Input(InputEvent @event) {
            if (!InputProcessing) {
                return;
            }

            if (!(@event is InputEventKey)) {
                return;
            }

            if (Input.IsKeyPressed((int)KeyList.Escape)) {
                GetTree().Quit();
            } else if (Input.IsKeyPressed((int)KeyList.Enter)) {
                GenerateMap();
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
            try {
                var builder = new LoggerConfiguration();

                SDirectory.CreateDirectory(_logsDirectory);

                builder
#if DEBUG
                    .WriteTo.Console()
                    .MinimumLevel.Debug()
#else
                .WriteTo.File(
                    path: SPath.Combine(_logsDirectory, $"{DateTime.Now}.log"),
                    restrictedToMinimumLevel: LogEventLevel.Warning
                    )
#endif
                    ;

                Log.Logger = builder.CreateLogger();
            } catch (Exception ex) {
                Log.Logger.Fatal(ex, "Failed to setup logger");
            }
        }

        internal void GenerateMap() {
            _mapGen.GenerateMap();
            if (VectorsAreClose(_pointer.Translation, Vector3.Zero)) {
                _pointer.Reset(
                    _mapGen.Config.Width / 2,
                    _mapGen.Config.Height
                );
            }

            _guiHandler._on_ShowNoisePreviewCheckBox_toggled(ShowNoiseMinimap);
        }

        private static bool VectorsAreClose(Vector3 left, Vector3 right) {
            return Mathf.Abs(left.x - right.x) < Mathf.Epsilon &&
                   Mathf.Abs(left.y - right.y) < Mathf.Epsilon &&
                   Mathf.Abs(left.z - right.z) < Mathf.Epsilon;
        }
    }
}
