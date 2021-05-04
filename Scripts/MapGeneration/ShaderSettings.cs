using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using Color = Godot.Color;

namespace ProceduralGeneration.Scripts.MapGeneration {
    public class ShaderSettings {
        public enum MeshSections {
            Snow,
            Stone,
            Grass,
            Sand,
        }

        public static ImmutableDictionary<MeshSections, double> DefaultBorders { get; } =
            new Dictionary<MeshSections, double> {
                [MeshSections.Snow]  = 1.80,
                [MeshSections.Stone] = 0.70,
                [MeshSections.Grass] = 0.05,
                [MeshSections.Sand]  = float.NegativeInfinity,
            }.ToImmutableDictionary();

        public static ImmutableDictionary<MeshSections, Color> DefaultColors { get; } =
            new Dictionary<MeshSections, Color> {
                [MeshSections.Snow]  = new Color(0.90f, 0.90f, 0.90f),
                [MeshSections.Stone] = new Color(0.26f, 0.26f, 0.26f),
                [MeshSections.Grass] = new Color(0.17f, 0.51f, 0.00f),
                [MeshSections.Sand]  = new Color(0.82f, 0.80f, 0.35f),
            }.ToImmutableDictionary();

        public static string BorderValue(MeshSections section) {
            switch (section) {
                case MeshSections.Snow:  return "snow_value";
                case MeshSections.Stone: return "stone_value";
                case MeshSections.Grass: return "grass_value";
                case MeshSections.Sand:  return "sand_value";
                default: throw new ArgumentOutOfRangeException(nameof(section), section, "Handle this");
            }
        }

        public static string ColorValue(MeshSections section) {
            switch (section) {
                case MeshSections.Snow:  return "snow_color";
                case MeshSections.Stone: return "stone_color";
                case MeshSections.Grass: return "grass_color";
                case MeshSections.Sand:  return "sand_color";
                default:                 throw new ArgumentOutOfRangeException(nameof(section), section, "Handle this");
            }
        }

        public Dictionary<MeshSections, Color> Colors { get; } = new Dictionary<MeshSections, Color> {
            [MeshSections.Snow] = DefaultColors[MeshSections.Snow],
            [MeshSections.Stone] = DefaultColors[MeshSections.Stone],
            [MeshSections.Grass] = DefaultColors[MeshSections.Grass],
            [MeshSections.Sand] = DefaultColors[MeshSections.Sand],
        };

        public Dictionary<MeshSections, double> Borders { get; } = new Dictionary<MeshSections, double> {
            [MeshSections.Snow]  = DefaultBorders[MeshSections.Snow],
            [MeshSections.Stone] = DefaultBorders[MeshSections.Stone],
            [MeshSections.Grass] = DefaultBorders[MeshSections.Grass],
            [MeshSections.Sand]  = DefaultBorders[MeshSections.Sand],
        };
    }
}
