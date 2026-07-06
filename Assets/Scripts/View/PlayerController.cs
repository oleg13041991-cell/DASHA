#nullable enable
using UnityEngine;

namespace RagsToRiches.View
{
    /// <summary>
    /// Управление героем: floating-джойстик на тач-экране (палец задаёт
    /// направление от точки первого касания) + WASD/стрелки для теста в редакторе.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;
        [SerializeField] private float _joystickRadiusPixels = 140f;

        private CharacterController _controller = null!;
        private Vector2 _touchOrigin;
        private bool _touchActive;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = ReadInput();
            var move = new Vector3(input.x, 0f, input.y);
            if (move.sqrMagnitude > 1f)
                move.Normalize();

            _controller.SimpleMove(move * _moveSpeed);

            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, _rotationSpeedDegPerSec * Time.deltaTime);
            }
        }

        private Vector2 ReadInput()
        {
            var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (keyboard.sqrMagnitude > 0.01f)
                return keyboard;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _touchOrigin = touch.position;
                        _touchActive = true;
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        _touchActive = false;
                        break;
                }

                if (_touchActive)
                {
                    Vector2 delta = touch.position - _touchOrigin;
                    return Vector2.ClampMagnitude(delta / _joystickRadiusPixels, 1f);
                }
            }
            else
            {
                _touchActive = false;
            }

            return Vector2.zero;
        }
    }
}
