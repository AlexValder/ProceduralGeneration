using Godot;
using System;
using ProceduralGeneration.Utils;

namespace ProceduralGeneration
{
    public class Camera : Godot.Camera
    {
        [Export]
        private float MouseSensitivity = 3f;
        private float InitialRadius = 3f;

        public override void _Ready()
        {
            LookAtFromPosition(
                new Vector3(InitialRadius, 0, 0 ),
                Vector3.Zero,
                Vector3.Up
                );
        }

        public override void _Input(InputEvent @event)
        {
            //if (@event is InputEventMouseMotion e)
            //{
            //    LookAtFromPosition(
            //        GetNewCoordinates(e),
            //        Vector3.Zero,
            //        Vector3.Up
            //        );
            //    GD.Print(Translation);
            //}
            if (@event is InputEventKey e)
            {
                var tmp = new SVector3(Translation);
                tmp.r = 3f;
                if (e.Scancode == (uint)KeyList.Up && !e.IsPressed())
                {
                    tmp.phi += .1f;
                }
                else if (e.Scancode == (uint)KeyList.Down && !e.IsPressed())
                {
                    tmp.phi -= .1f;
                }
                else if (e.Scancode == (uint)KeyList.Left && !e.IsPressed())
                {
                    tmp.theta -= .1f;
                }
                else if (e.Scancode == (uint)KeyList.Right && !e.IsPressed())
                {
                    tmp.theta += .1f;
                }

                tmp.phi = Mathf.Clamp(tmp.phi, 1f / Mathf.Pi, Mathf.Pi / 2 - 0.2f);
                //tmp.theta = Mathf.Clamp(tmp.theta, 0, Mathf.Pi * 2);

                GD.Print("NEW EVENT");
                GD.Print(tmp);

                Translation = tmp;

                //LookAtFromPosition(
                //    tmp,
                //    Vector3.Zero,
                //    Vector3.Up
                //    );
                GD.Print(Translation);
            }
        }

        private Vector3 GetNewCoordinates(InputEventMouseMotion e)
        {
            GD.Print(e.Relative);
            var viewport = GetViewport().Size;
            var tmp = new SVector3(InitialRadius, Translation.y + e.Relative.y/viewport.y, Translation.x + e.Relative.x/viewport.x);
            GD.Print(tmp);
            return tmp;
        }
    }
}
