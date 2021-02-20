using Godot;
using System;

namespace GameTest
{
    public class Camera : Godot.Camera
    {
        [Export]
        private float MouseSensitivity = 3f;
        private float InitialRadius = 3f;

        public override void _Ready()
        {
            LookAtFromPosition(
                new Vector3(InitialRadius, InitialRadius/2, InitialRadius),
                Vector3.Zero,
                Vector3.Up
                );
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseMotion e)
            {
                LookAtFromPosition(
                    new Vector3(InitialRadius * Mathf.Cos(e.Relative.x), InitialRadius/2, InitialRadius * Mathf.Sin(e.Relative.y)),
                    Vector3.Zero,
                    Vector3.Up
                    );
                GD.Print(Translation);
            }
        }
    }
}
