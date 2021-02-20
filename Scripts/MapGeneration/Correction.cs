using Godot;

namespace ProceduralGeneration.Scripts.MapGeneration {
    public enum CorrectionType {
        Linear,
        Square,
        Cubic
    }

    public class Correction {
        private const float WATER = 0.001f;
        public CorrectionType Type { get; set; }
        public bool AllowNegative { get; set; }

        public float GetCorrection(float initial) {
            switch (Type) {
                case CorrectionType.Linear: // Don't change the noise
                    return AllowNegative ? initial : Mathf.Max(initial, -WATER);
                case CorrectionType.Square: // f(x) = x^2 * sign(x)
                    return AllowNegative ? Mathf.Pow(initial, 2) * Mathf.Sign(initial) :
                        initial < WATER ? -WATER : Mathf.Pow(initial, 2);
                case CorrectionType.Cubic: // f(x) = x^3
                    return AllowNegative ? Mathf.Pow(initial, 3) :
                        initial < WATER ? -WATER : Mathf.Pow(initial, 3);
                default:
                    return 0;
            }
        }
    }
}