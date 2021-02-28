using Godot;
using System;
using System.Collections.Generic;

namespace ProceduralGeneration
{
    public class Pointer : MeshInstance
    {
        private const uint Step = 1u;
        private const float Phi = Mathf.Pi / 2;
        private const float Theta = Mathf.Pi / 12;

        private enum Direction { Vertical, Horizontal };

        private static readonly Dictionary<KeyList, Vector3> _actions =
            new Dictionary<KeyList, Vector3>()
            {
                [KeyList.W] = new Vector3(0, 0, -Step),
                [KeyList.A] = new Vector3(-Step, 0, 0),
                [KeyList.S] = new Vector3(0, 0, Step),
                [KeyList.D] = new Vector3(Step, 0, 0),
            };
        private static readonly Dictionary<KeyList, (Direction, int)> _rotation =
            new Dictionary<KeyList, (Direction, int)>()
            {
                [KeyList.Left] = (Direction.Horizontal, 1),
                [KeyList.Right] = (Direction.Horizontal, -1),
                [KeyList.Up] = (Direction.Vertical, 1),
                [KeyList.Down] = (Direction.Vertical, -1),
            };


        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey e
                && !e.IsPressed()
                && _actions.ContainsKey((KeyList)e.Scancode))
            {
                Translate(_actions[(KeyList)e.Scancode]);
                GetTree().SetInputAsHandled();
            }
            
            if (@event is InputEventKey e1
                && e1.IsPressed()
                && _rotation.ContainsKey((KeyList)e1.Scancode))
            {
                var pair = _rotation[(KeyList)e1.Scancode];
                
                switch (pair.Item1)
                {
                    case Direction.Horizontal:
                        Rotate(Vector3.Up, Phi * pair.Item2);
                        GetTree().SetInputAsHandled();
                        break;
                    case Direction.Vertical:
                        Rotate(new Vector3(0.5f, 0, 0.5f).Normalized(), Theta * pair.Item2);
                        GetTree().SetInputAsHandled();
                        break;
                }
            }
        }
    }
}
