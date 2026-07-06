#nullable enable
using UnityEngine;

namespace RagsToRiches.View
{
    /// <summary>
    /// Изометрическая камера со сглаженным слежением за целью.
    /// Угол задаётся один раз смещением; в LateUpdate двигается только позиция.
    /// </summary>
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform? _target;
        [SerializeField] private Vector3 _offset = new(0f, 13f, -9f);
        [SerializeField] private float _smoothTime = 0.15f;

        private Vector3 _velocity;

        public void SetTarget(Transform target)
        {
            _target = target;
            transform.position = target.position + _offset;
            transform.rotation = Quaternion.LookRotation(-_offset, Vector3.up);
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            transform.position = Vector3.SmoothDamp(
                transform.position, _target.position + _offset, ref _velocity, _smoothTime);
        }
    }
}
