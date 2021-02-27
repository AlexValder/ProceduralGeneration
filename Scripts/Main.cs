using Godot;
using System;

namespace ProceduralGeneration
{
    public class Main : Spatial
    {
        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey)
            {
                if (Input.IsKeyPressed((int)KeyList.Escape))
                {
                    GetTree().Quit();
                }
            }
        }
    }
}
