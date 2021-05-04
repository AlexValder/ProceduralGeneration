using System;
using Godot;

namespace ProceduralGeneration.Scripts.MapGeneration {
    public enum CorrectionType {
        Linear,
        Square,
        Cubic
    }

    public class Correction {
        public CorrectionType Type { get; set; }

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
    }
}
