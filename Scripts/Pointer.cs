using Godot;
using System;
using System.Collections.Generic;

public class Pointer : MeshInstance
{
    private const uint Step = 1u;
    private static readonly Dictionary<KeyList, Vector3> _actions =
        new Dictionary<KeyList, Vector3>()
        {
            [KeyList.W] = new Vector3(0, 0, -Step),
            [KeyList.A] = new Vector3(-Step, 0, 0),
            [KeyList.S] = new Vector3(0, 0, Step),
            [KeyList.D] = new Vector3(Step, 0, 0),
        };

    public override void _Ready()
    {
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey e
            && !e.IsPressed()
            && _actions.ContainsKey((KeyList)e.Scancode))
        {
            Translate(_actions[(KeyList)e.Scancode]);
            GD.Print("MOVED");
        }
    }
}
