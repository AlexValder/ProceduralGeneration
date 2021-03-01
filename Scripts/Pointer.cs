using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProceduralGeneration
{
    public class Pointer : MeshInstance
    {
        private const uint Step = 1u;
        private const float Phi = Mathf.Pi / 2;
        private const float Theta = Mathf.Pi / 12;

        private const float _zoomMin = 1f;
        private const float _zoomMax = 10f;
        private const float _zoomStep = .5f;
        private float _zoom = _zoomMin;
        private Camera _camera;
        private static readonly Vector3 _cameraPos = new Vector3(2.2f, 2.2f, 2.2f);

        private enum Direction { Vertical, Horizontal };
        private enum Scroll { Closer, Further };

        private static readonly ImmutableDictionary<KeyList, Vector3> _actions =
            new Dictionary<KeyList, Vector3>()
            {
                [KeyList.W] = new Vector3(0, 0, -Step),
                [KeyList.A] = new Vector3(-Step, 0, 0),
                [KeyList.S] = new Vector3(0, 0, Step),
                [KeyList.D] = new Vector3(Step, 0, 0),
            }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<KeyList, (Direction, int)> _rotation =
            new Dictionary<KeyList, (Direction, int)>()
            {
                [KeyList.Left] = (Direction.Horizontal, 1),
                [KeyList.Right] = (Direction.Horizontal, -1),
                [KeyList.Up] = (Direction.Vertical, 1),
                [KeyList.Down] = (Direction.Vertical, -1),
            }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<ButtonList, Scroll> _scrolling =
            new Dictionary<ButtonList, Scroll>()
            {
                [ButtonList.WheelUp] = Scroll.Further,
                [ButtonList.WheelDown] = Scroll.Closer,
            }.ToImmutableDictionary();

        public override void _Ready()
        {
            try
            {
                _camera = GetChild<Camera>(0);
                _camera.Translation = _cameraPos;
            }
            catch (Exception ex)
            {
                GD.Print(ex.Message);
            }
        }

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
                        RotateY(Phi * pair.Item2);
                        GetTree().SetInputAsHandled();
                        break;
                    case Direction.Vertical:
                        RotateX(Theta * pair.Item2);
                        GetTree().SetInputAsHandled();
                        break;
                }
            }

            if (@event is InputEventMouseButton e2
                && e2.IsPressed()
                && _scrolling.ContainsKey((ButtonList)e2.ButtonIndex))
            {
                switch (_scrolling[(ButtonList)e2.ButtonIndex])
                {
                    case Scroll.Closer:
                        if (_zoom < _zoomMax)
                        {
                            _zoom += _zoomStep;
                            _camera.Translation = _cameraPos * _zoom;
                        }
                        break;
                    case Scroll.Further:
                        if (_zoom > _zoomMin)
                        {
                            _zoom -= _zoomStep;
                            _camera.Translation = _cameraPos * _zoom;
                        }
                        break;
                }
            }
        }
    }
}
