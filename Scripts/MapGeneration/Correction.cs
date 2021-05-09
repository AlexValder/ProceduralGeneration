using System;
using Godot;
using Newtonsoft.Json;
using Serilog;

namespace ProceduralGeneration.Scripts.MapGeneration {
    public enum CorrectionType {
        Linear,
        Square,
        Cubic,
    }

    public class Correction {

        [JsonIgnore] private static readonly double Sqrt2 = Math.Sqrt(2);
        [JsonIgnore] private static readonly double Sqrt2By2 = Math.Sqrt(2) / 2;
        private const float WATER_LEVEL = -.01f;
        public CorrectionType Type { get; set; }

        [JsonIgnore] public int MapWidth { get; set; }

        [JsonIgnore] public int MapHeight { get; set; }

        public float GetCorrection(float initial) {
            return CorFunction(initial);
        }

        private float CorFunction(float initial) {
            switch (Type) {
                case CorrectionType.Linear: // Don't change the noise
                    return initial;
                case CorrectionType.Square: // f(x) = x^2 * sign(x)
                    return Mathf.Pow(initial, 2) * Mathf.Sign(initial);
                case CorrectionType.Cubic: // f(x) = x^3
                    return Mathf.Pow(initial, 3);
                default:
                    throw new NotSupportedException($"Please handle ${Type}");
            }
        }

        public float ApplyGradient(int x, int y, float z) {
            var tmp = Math.Pow(Math.Abs(x - MapWidth), 2) / Math.Pow(MapWidth, 2)
                      + Math.Pow(Math.Abs(y - MapWidth), 2) / Math.Pow(MapHeight, 2);
            var grad = (float)(-Math.Sqrt(tmp) / Sqrt2By2) + 1f;
            if (grad > 0) {
                return z * grad;
            }

            if (z < 0) return -z * grad + WATER_LEVEL;
            return z * grad;
        }
    }
}
