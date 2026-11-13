using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SolarSystemAR
{
    /// 普通拖动旋转，长按拖动在扫描锚点附近移动；拖动、长按和捏合不当作点击。
    public class PlanetTapTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Min(0f)] private float degreesPerScreen = 360f;
        [SerializeField, Min(0.1f)] private float moveHoldSeconds = 0.5f;
        [SerializeField, Min(0f)] private float moveRadius = 0.1f;
        private Vector3 anchorLocalPosition;
        private Vector3 planeLocalPoint;
        private Vector3 planeLocalNormal;
        private Vector3 grabLocalOffset;
        private bool moving;
        private bool movePlaneReady;
        private PlanetAnnotations annotations;
        private ScanPlanetContent content;
        private Vector2 pressPosition;
        private float pressTime;
        private int pointerId;
        private bool pressed;
        private bool dragged;
        private bool cancelled;
        private SelfRotation pausedSpin;

        private void Awake()
        {
            anchorLocalPosition = transform.localPosition;
        }

        private Vector3 ToWorldPoint(Vector3 point) => transform.parent != null
            ? transform.parent.TransformPoint(point) : point;

        private Vector3 ToLocalPoint(Vector3 point) => transform.parent != null
            ? transform.parent.InverseTransformPoint(point) : point;

        private Camera PointerCamera(PointerEventData data) => data.pressEventCamera != null
            ? data.pressEventCamera : Camera.main;

        private bool PointerOnMovePlane(PointerEventData data, out Vector3 point)
        {
            point = Vector3.zero;
            var camera = PointerCamera(data);
            if (camera == null) return false;
            var normal = transform.parent != null
                ? transform.parent.TransformDirection(planeLocalNormal) : planeLocalNormal;
            var plane = new Plane(normal, ToWorldPoint(planeLocalPoint));
            var ray = camera.ScreenPointToRay(data.position);
            if (!plane.Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        private static int TouchCount()
        {
            int count = 0;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed) count++;
            return count;
        }

        private void Update()
        {
            // 加入第二根手指后，本次按压不再恢复单指旋转，避免缩放结束时跳动。
            if (pressed && TouchCount() > 1) CancelRotation();
        }

        public void Initialize(PlanetAnnotations owner, ScanPlanetContent planet)
        {
            annotations = owner;
            content = planet;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (annotations != null) annotations.PointerDown(eventData.pointerId);
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (pressed)
            {
                CancelRotation();
                return;
            }
            pressPosition = eventData.position;
            pressTime = Time.unscaledTime;
            pointerId = eventData.pointerId;
            pressed = true;
            dragged = false;
            moving = false;
            movePlaneReady = false;
            var camera = PointerCamera(eventData);
            if (camera != null)
            {
                planeLocalPoint = transform.localPosition;
                planeLocalNormal = transform.parent != null
                    ? transform.parent.InverseTransformDirection(camera.transform.forward)
                    : camera.transform.forward;
                if (PointerOnMovePlane(eventData, out var hit))
                {
                    grabLocalOffset = transform.localPosition - ToLocalPoint(hit);
                    movePlaneReady = true;
                }
            }
            cancelled = TouchCount() > 1;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (annotations != null) annotations.PointerUp(eventData.pointerId);
            if (eventData.pointerId != pointerId) return;
            pressed = false;
            ResumeSpin();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!pressed || eventData.pointerId != pointerId) return;
            dragged = true;
            eventData.eligibleForClick = false;
            if (TouchCount() > 1) CancelRotation();
            if (cancelled) return;
            // 手势开始时固定模式，旋转过程中停顿不会突然切换为移动。
            moving = movePlaneReady && Time.unscaledTime - pressTime >= moveHoldSeconds;
            var spin = GetComponent<SelfRotation>();
            if (spin != null && spin.enabled)
            {
                pausedSpin = spin;
                spin.enabled = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!pressed || !dragged || eventData.pointerId != pointerId || cancelled) return;
            if (TouchCount() > 1) { CancelRotation(); return; }
            if (moving)
            {
                if (PointerOnMovePlane(eventData, out var hit))
                {
                    var desired = ToWorldPoint(ToLocalPoint(hit) + grabLocalOffset);
                    var anchor = ToWorldPoint(anchorLocalPosition);
                    transform.position = anchor + Vector3.ClampMagnitude(desired - anchor, moveRadius);
                }
                return;
            }
            var camera = eventData.pressEventCamera;
            if (camera == null) camera = Camera.main;
            if (camera == null) return;
            float sensitivity = degreesPerScreen / Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height));
            transform.Rotate(camera.transform.up, -eventData.delta.x * sensitivity, Space.World);
            transform.Rotate(camera.transform.right, eventData.delta.y * sensitivity, Space.World);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) ResumeSpin();
        }

        private void CancelRotation()
        {
            cancelled = true;
            ResumeSpin();
        }

        private void ResumeSpin()
        {
            if (pausedSpin != null) pausedSpin.enabled = true;
            pausedSpin = null;
        }

        private void OnDisable()
        {
            if (annotations != null) annotations.PointerUp(pointerId);
            pressed = false;
            cancelled = true;
            ResumeSpin();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (annotations == null || dragged || cancelled || TouchCount() > 1 ||
                eventData.button != PointerEventData.InputButton.Left ||
                eventData.pointerId != pointerId || eventData.dragging || Time.unscaledTime - pressTime >= Mathf.Min(0.6f, moveHoldSeconds)) return;
            float threshold = Mathf.Max(12f, Screen.dpi > 0 ? Screen.dpi * 0.08f : 12f);
            if (Vector2.Distance(pressPosition, eventData.position) > threshold) return;
            if (annotations.CanOpenFromTap) annotations.Show(content);
        }
    }
}
