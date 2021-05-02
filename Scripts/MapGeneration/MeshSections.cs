using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;

namespace ProceduralGeneration.Scripts.MapGeneration {
    public enum MeshSections {
        Snow,
        Stone,
        Grass,
        Sand,
    }

    public static class ShaderDefaults {
        public static readonly ImmutableDictionary<MeshSections, float> DefaultBorders =
            new Dictionary<MeshSections, float> {
                [MeshSections.Snow]  = 1.80f,
                [MeshSections.Stone] = 0.70f,
                [MeshSections.Grass] = 0.05f,
                [MeshSections.Sand]  = float.NegativeInfinity,
            }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<MeshSections, Color> DefaultColors =
            new Dictionary<MeshSections, Color> {
                [MeshSections.Snow]  = new Color(0.90f, 0.90f, 0.90f),
                [MeshSections.Stone] = new Color(0.26f, 0.26f, 0.26f),
                [MeshSections.Grass] = new Color(0.17f, 0.51f, 0.00f),
                [MeshSections.Sand]  = new Color(0.82f, 0.80f, 0.35f),
            }.ToImmutableDictionary();
    }
}
