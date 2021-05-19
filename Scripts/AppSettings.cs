using System;
using System.Collections.Immutable;
using Godot;
using Godot.Collections;
using Serilog;
using SPath = System.IO.Path;
using SFile = System.IO.File;

namespace ProceduralGeneration.Scripts {
    public enum SettingsEntries {
        Fullscreen,
        LogFolder,
        SavesFolder,
    }

    public class AppSettings {
#if DEBUG
        private const string CONFIG_NAME = "../config.ini";
#else
        private const string CONFIG_NAME = "config.ini";
#endif

        private const string SECTION = "app_settings";
        private readonly string _configPath = SPath.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_NAME);
        private readonly ConfigFile _config = new ConfigFile();

        private static readonly ImmutableDictionary<SettingsEntries, dynamic> DefaultSettings =
            new Dictionary<SettingsEntries, dynamic> {
                [SettingsEntries.Fullscreen] = false,
#if DEBUG
                [SettingsEntries.LogFolder]   = SPath.GetFullPath("../Logs/").Replace("\\", "/"),
                [SettingsEntries.SavesFolder] = SPath.GetFullPath("../Saves/").Replace("\\", "/"),
#else
                [SettingsEntries.LogFolder]   = SPath.GetFullPath("./Logs/").Replace("\\", "/"),
                [SettingsEntries.SavesFolder] = SPath.GetFullPath("./Saves/").Replace("\\", "/"),
#endif
            }.ToImmutableDictionary();

        private readonly Dictionary<SettingsEntries, dynamic> _settings = new Dictionary<SettingsEntries, dynamic> {
            [SettingsEntries.Fullscreen]  = DefaultSettings[SettingsEntries.Fullscreen],
            [SettingsEntries.LogFolder]   = DefaultSettings[SettingsEntries.LogFolder],
            [SettingsEntries.SavesFolder] = DefaultSettings[SettingsEntries.SavesFolder],
        };

        public AppSettings() => LoadConfig();

        private void LoadConfig() {
            if (!SFile.Exists(_configPath)) {
                PopulateConfig();
                return;
            }

            var err = _config.Load(_configPath);
            if (err == Error.Ok) {
                foreach (var key in _settings.Keys) {
                    _config.GetValue(SECTION, StringSettingsEntry(key), DefaultSettings[key]);
                }
            } else {
                Log.Logger.Error("Failed to load config: {Error}", err);
            }
        }

        private void PopulateConfig() {
            using (_ = SFile.Create(_configPath)) {}

            var err = _config.Load(_configPath);
            if (err == Error.Ok) {
                foreach (var key in DefaultSettings.Keys) {
                    _config.SetValue(SECTION, StringSettingsEntry(key), DefaultSettings[key]);
                }
                _config.Save(_configPath);
            } else {
                Log.Logger.Error("Failed to populate config");
            }
        }

        public void GetValue<T>(in SettingsEntries entry, out T value) {
            var key = StringSettingsEntry(entry);
            var tmp = _config.GetValue(SECTION, key, DefaultSettings[entry]);
            value = tmp is T obj ? obj : default;
        }

        public void SetValue(in SettingsEntries entry, in dynamic value) {
            var key = StringSettingsEntry(entry);

            _settings[entry] = value;
            _config.SetValue(SECTION, key, value);
            var err = _config.Save(_configPath);
            if (err == Error.Ok) {
                Log.Logger.Debug("Saved {Key} as {Value}", key, value);
            } else {
                Log.Logger.Error("Failed to save {Key} ({Error})", key, err);
            }
        }

        private static string StringSettingsEntry(in SettingsEntries entry) {
            switch (entry) {
                case SettingsEntries.Fullscreen:
                    return "fullscreen";
                case SettingsEntries.LogFolder:
                    return "log_folder";
                case SettingsEntries.SavesFolder:
                    return "saves_folder";
                default:
                    throw new NotSupportedException(nameof(entry));
            }
        }
    }
}
