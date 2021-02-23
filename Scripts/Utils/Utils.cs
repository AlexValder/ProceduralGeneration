using System;
using Godot;

namespace ProceduralGeneration.Utils
{
    public static class Utils
    {
        public static Vector3 SphericalToCarthesian(SVector3 coord) => new Vector3(
            coord.r * Mathf.Sin(coord.theta) * Mathf.Cos(coord.phi),
            coord.r * Mathf.Sin(coord.theta) * Mathf.Sin(coord.phi),
            coord.r * Mathf.Cos(coord.theta)
        );

        public static SVector3 CarthesianToSpherical(Vector3 coord) => new SVector3(
                Mathf.Sqrt(coord.x * coord.x + coord.y * coord.y + coord.z * coord.z),
                //Mathf.Acos(coord.z/Mathf.Sqrt(coord.x * coord.x + coord.y * coord.y + coord.z * coord.z)),
                Mathf.Atan(Mathf.Sqrt(coord.x * coord.x + coord.y * coord.y) / coord.z),
                Mathf.Atan(coord.y / coord.x)
            );

        public static SVector3 ToSpherical(this Vector3 coord) => CarthesianToSpherical(coord);
    }

    /// <summary>
    /// Simple struct to hold spherical coordinates.
    /// </summary>
    public struct SVector3
    {
        public float r;
        public float theta;
        public float phi;

        public SVector3(float r, float theta, float phi)
        {
            this.r = r;
            this.theta = theta;
            this.phi = phi;
        }

        public SVector3(Vector3 vec)
        {
            var tmp = vec.ToSpherical();
            r = tmp.r;
            theta = tmp.theta;
            phi = tmp.phi;
        }

        public static implicit operator Vector3(SVector3 coord)
        {
            return Utils.SphericalToCarthesian(coord);
        }

        public override string ToString() => $"SVector3({r}, {theta}, {phi})";
    }
}
