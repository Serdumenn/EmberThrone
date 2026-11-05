using System;
using EmberThrone.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EmberThrone.Game
{
    /// <summary>
    /// Turns pointer input (touch on a phone, mouse in the editor) into board gestures, using the new
    /// Input System: a drag of more than <see cref="DragCells"/> cells is a swipe, anything else is a tap.
    /// </summary>
    public sealed class BoardInput : MonoBehaviour
    {
        const float DragCells = 0.4f;

        public event Action<GridPos> Pressed;
        public event Action<GridPos> Released;
        public event Action<GridPos, GridPos> Swiped;
        public event Action<GridPos> Tapped;

        Camera _camera;
        BoardView _view;
        bool _enabled;
        bool _down;
        bool _swiped;
        Vector2 _start;
        GridPos _cell;

        public void Init(Camera camera, BoardView view)
        {
            _camera = camera;
            _view = view;
        }

        /// <summary>Turned off while an outcome is animating, so input can never overlap a resolution.</summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            if (!enabled) Cancel();
        }

        void Cancel()
        {
            if (_down) Released?.Invoke(_cell);
            _down = false;
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || !_enabled || _view == null) return;

            var press = pointer.press;
            var pos = pointer.position.ReadValue();

            if (!_down)
            {
                if (!press.wasPressedThisFrame) return;
                var cell = _view.CellAtWorld(_camera.ScreenToWorldPoint(pos));
                if (!cell.IsValid) return;
                _down = true;
                _swiped = false;
                _start = pos;
                _cell = cell;
                Pressed?.Invoke(cell);
                return;
            }

            if (!_swiped)
            {
                var delta = pos - _start;
                if (delta.magnitude >= DragCells * PixelsPerCell())
                {
                    _swiped = true;
                    Swiped?.Invoke(_cell, Neighbor(_cell, delta));
                    // the swipe may have disabled input and released the cell already
                    if (!_down) return;
                }
            }

            if (press.wasReleasedThisFrame || !press.isPressed)
            {
                _down = false;
                Released?.Invoke(_cell);
                if (!_swiped) Tapped?.Invoke(_cell);
            }
        }

        float PixelsPerCell() => Screen.height / (2f * _camera.orthographicSize);

        static GridPos Neighbor(GridPos cell, Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)) return delta.x > 0f ? cell.Right : cell.Left;
            return delta.y > 0f ? cell.Up : cell.Down;
        }
    }
}
