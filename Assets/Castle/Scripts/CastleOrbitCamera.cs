using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CastleGen
{
    /// <summary>
    /// Slow turntable orbit around the castle. Right-drag to look, scroll to zoom;
    /// releasing the button hands control back to the automatic orbit.
    /// </summary>
    public class CastleOrbitCamera : MonoBehaviour
    {
        public Vector3 target = new Vector3(0f, 5.5f, 1f);
        public float autoOrbitSpeed = 4f;
        public bool allowInput = true;
        public float minDistance = 18f;
        public float maxDistance = 110f;

        float _distance = 46f;
        float _yaw;
        float _pitch = 24f;

        void Start()
        {
            Vector3 offset = transform.position - target;
            _distance = offset.magnitude;
            _yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Asin(Mathf.Clamp(offset.y / Mathf.Max(_distance, 0.001f), -1f, 1f)) * Mathf.Rad2Deg;
        }

        void Update()
        {
            Vector2 look;
            float zoom;
            bool dragging = ReadInput(out look, out zoom);

            if (dragging)
            {
                _yaw += look.x * 0.18f;
                _pitch = Mathf.Clamp(_pitch - look.y * 0.12f, 6f, 78f);
            }
            else
            {
                _yaw += autoOrbitSpeed * Time.deltaTime;
            }

            if (!Mathf.Approximately(zoom, 0f))
                _distance = Mathf.Clamp(_distance - zoom, minDistance, maxDistance);

            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = target + rot * new Vector3(0f, 0f, -_distance);
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }

        bool ReadInput(out Vector2 look, out float zoom)
        {
            look = Vector2.zero;
            zoom = 0f;
            if (!allowInput) return false;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse == null) return false;
            zoom = mouse.scroll.ReadValue().y * 0.03f;
            if (!mouse.rightButton.isPressed) return false;
            look = mouse.delta.ReadValue();
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            zoom = Input.mouseScrollDelta.y * 3f;
            if (!Input.GetMouseButton(1)) return false;
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 20f;
            return true;
#else
            return false;
#endif
        }
    }
}
