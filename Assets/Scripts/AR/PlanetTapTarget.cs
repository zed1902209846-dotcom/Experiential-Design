using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SolarSystemAR
{
    /// 模型上的单指旋转与点击入口；拖动、长按和捏合均不当作点击。
    public class PlanetTapTarget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Min(0f)] private float degreesPerScreen = 360f;
        private PlanetAnnotations annotations;
        private ScanPlanetContent content;
        private Vector2 pressPosition;
        private float pressTime;
        private int pointerId;
        private bool pressed;
        private bool dragged;
        private bool cancelled;
        private SelfRotation pausedSpin;

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
                eventData.pointerId != pointerId || eventData.dragging || Time.unscaledTime - pressTime > 0.6f) return;
            float threshold = Mathf.Max(12f, Screen.dpi > 0 ? Screen.dpi * 0.08f : 12f);
            if (Vector2.Distance(pressPosition, eventData.position) > threshold) return;
            if (annotations.CanOpenFromTap) annotations.Show(content);
        }
    }
}
