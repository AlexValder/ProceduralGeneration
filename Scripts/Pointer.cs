using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Serilog;

namespace ProceduralGeneration.Scripts {
    public class Pointer : MeshInstance {
        private const uint STEP = 1u;
        private const float PHI = Mathf.Pi / 2;
        private const float THETA = Mathf.Pi / 12;

        private const float ZOOM_MIN = 1f;
        private const float ZOOM_MAX = 10f;
        private const float ZOOM_STEP = .5f;
        private static readonly Vector3 CameraPos = new Vector3(2.2f, 2.2f, 2.2f);

        private static readonly ImmutableDictionary<KeyList, Vector3> Actions =
            new Dictionary<KeyList, Vector3> {
                [KeyList.W] = new Vector3(0, 0, -STEP),
                [KeyList.A] = new Vector3(-STEP, 0, 0),
                [KeyList.S] = new Vector3(0, 0, STEP),
                [KeyList.D] = new Vector3(STEP, 0, 0)
            }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<KeyList, (Direction, int)> CameraRotation =
            new Dictionary<KeyList, (Direction, int)> {
                [KeyList.Left]  = (Direction.Horizontal, 1),
                [KeyList.Right] = (Direction.Horizontal, -1),
                [KeyList.Up]    = (Direction.Vertical, 1),
                [KeyList.Down]  = (Direction.Vertical, -1)
            }.ToImmutableDictionary();

        private static readonly ImmutableDictionary<ButtonList, Scroll> Scrolling =
            new Dictionary<ButtonList, Scroll> {
                [ButtonList.WheelUp]   = Scroll.Further,
                [ButtonList.WheelDown] = Scroll.Closer
            }.ToImmutableDictionary();

        private Camera _camera;
        private float _zoom = ZOOM_MIN;

        /// <summary>
        ///     Enum for camera movements.
        /// </summary>
        private enum Direction {
            Vertical,
            Horizontal
        }

        /// <summary>
        ///     Enum for zooming in and out.
        /// </summary>
        private enum Scroll {
            Closer,
            Further
        }

        #region Godot Overrides

        public override void _Ready() {
            try {
                _camera             = GetChild<Camera>(0);
                _camera.Translation = CameraPos;
            }
            catch (Exception ex) {
                Log.Logger.Error(ex, "Failed to initialize Pointer");
            }
        }

        public override void _Input(InputEvent @event) {
            if (@event is InputEventKey e
                && !e.IsPressed()
                && Actions.ContainsKey((KeyList)e.Scancode)) {
                Translate(Actions[(KeyList)e.Scancode]);
                GetTree().SetInputAsHandled();
            }

            if (@event is InputEventKey e1
                && e1.IsPressed()
                && CameraRotation.ContainsKey((KeyList)e1.Scancode)) {
                var (direction, item2) = CameraRotation[(KeyList)e1.Scancode];

                switch (direction) {
                    case Direction.Horizontal:
                        RotateY(PHI * item2);
                        GetTree().SetInputAsHandled();
                        break;
                    case Direction.Vertical:
                        RotateX(THETA * item2);
                        GetTree().SetInputAsHandled();
                        break;
                    default:
                        Log.Logger.Error("Unknown value of {Direction}", nameof(Direction));
                        break;
                }
            }

            if (@event is InputEventMouseButton e2
                && e2.IsPressed()
                && Scrolling.ContainsKey((ButtonList)e2.ButtonIndex))
                switch (Scrolling[(ButtonList)e2.ButtonIndex]) {
                    case Scroll.Closer:
                        if (_zoom < ZOOM_MAX) {
                            _zoom               += ZOOM_STEP;
                            _camera.Translation =  CameraPos * _zoom;
                        }

                        break;
                    case Scroll.Further:
                        if (_zoom > ZOOM_MIN) {
                            _zoom               -= ZOOM_STEP;
                            _camera.Translation =  CameraPos * _zoom;
                        }

                        break;
                    default:
                        Log.Logger.Error("Unknown value of {Scroll}", nameof(Scroll));
                        break;
                }
        }

        #endregion
    }
}