using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 双指缩放识别后的模型，只改变视觉内容，不改变 Vuforia 目标的物理尺寸。
    [DisallowMultipleComponent]
    public class PlanetPinchScale : MonoBehaviour
    {
        private static readonly List<PlanetPinchScale> visiblePlanets = new List<PlanetPinchScale>();
        private static PlanetPinchScale gestureOwner;
        [SerializeField, Min(0.01f)] private float minScaleMultiplier = 0.5f;
        [SerializeField, Min(0.01f)] private float maxScaleMultiplier = 3f;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private Renderer[] modelRenderers;
        private Vector3 initialScale;
        private Vector3 gestureScale;
        private float gestureDistance;
        private int firstTouchId;
        private int secondTouchId;
        private bool pinching;
        private bool blockedUntilRelease;
        private PointerEventData pointer;
        private EventSystem pointerOwner;

        private void Awake()
        {
            initialScale = transform.localScale;
            modelRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnDisable()
        {
            visiblePlanets.Remove(this);
            if (gestureOwner == this) gestureOwner = null;
            pinching = false;
            blockedUntilRelease = false;
        }

        private void OnEnable()
        {
            if (!visiblePlanets.Contains(this)) visiblePlanets.Add(this);
        }

        private void Update()
        {
            var screen = Touchscreen.current;
            TouchControl first = null;
            TouchControl second = null;
            int count = 0;
            if (screen != null)
            {
                foreach (var touch in screen.touches)
                {
                    if (!touch.press.isPressed) continue;
                    if (count == 0) first = touch;
                    else if (count == 1) second = touch;
                    count++;
                }
            }

            // 一指松开后重新建立基准；三指操作不参与缩放。
            if (count != 2)
            {
                if (gestureOwner == this) gestureOwner = null;
                pinching = false;
                blockedUntilRelease = count > 2;
                return;
            }

            if (blockedUntilRelease) return;
            var firstPosition = first.position.ReadValue();
            var secondPosition = second.position.ReadValue();
            if (OverUI(firstPosition) || OverUI(secondPosition) || OverUI((firstPosition + secondPosition) * 0.5f))
            {
                if (gestureOwner == this) gestureOwner = null;
                pinching = false;
                blockedUntilRelease = true;
                return;
            }

            float distance = Vector2.Distance(firstPosition, secondPosition);
            if (distance < 1f)
            {
                pinching = false;
                return;
            }

            int firstId = first.touchId.ReadValue();
            int secondId = second.touchId.ReadValue();
            if (!pinching || firstId != firstTouchId || secondId != secondTouchId)
            {
                // 仅在手势中点命中模型时选定前方的一颗，整个手势中不切换。
                if ((gestureOwner != null && gestureOwner != this) ||
                    (gestureOwner == null && PlanetAtPosition(Camera.main, (firstPosition + secondPosition) * 0.5f) != this))
                {
                    pinching = false;
                    blockedUntilRelease = true;
                    return;
                }
                gestureOwner = this;
                firstTouchId = firstId;
                secondTouchId = secondId;
                gestureDistance = distance;
                gestureScale = transform.localScale;
                pinching = true;
                return;
            }

            float min = Mathf.Max(0.01f, minScaleMultiplier);
            float max = Mathf.Max(min, maxScaleMultiplier);
            float multiplier = Mathf.Clamp(
                gestureScale.magnitude / initialScale.magnitude * distance / gestureDistance, min, max);
            transform.localScale = initialScale * multiplier;
        }

        private static PlanetPinchScale PlanetAtPosition(Camera camera, Vector2 position)
        {
            if (camera == null || !camera.pixelRect.Contains(position)) return null;
            var ray = camera.ScreenPointToRay(position);
            PlanetPinchScale nearest = null;
            float closest = float.PositiveInfinity;
            foreach (var planet in visiblePlanets)
            {
                if (planet == null || !planet.isActiveAndEnabled) continue;
                foreach (var renderer in planet.modelRenderers)
                {
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        renderer is ParticleSystemRenderer || renderer.forceRenderingOff ||
                        (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                    // 使用各渲染器的包围盒，兼容土星环和不带 Collider 的模型。
                    if (!renderer.bounds.IntersectRay(ray, out float distance) || distance >= closest) continue;
                    float depth = camera.WorldToViewportPoint(ray.GetPoint(distance)).z;
                    if (depth < camera.nearClipPlane || depth > camera.farClipPlane) continue;
                    closest = distance;
                    nearest = planet;
                }
            }
            return nearest;
        }

        private bool OverUI(Vector2 position)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            if (pointer == null || pointerOwner != events)
            {
                pointer = new PointerEventData(events);
                pointerOwner = events;
            }
            pointer.Reset();
            pointer.position = position;
            uiHits.Clear();
            events.RaycastAll(pointer, uiHits);
            foreach (var hit in uiHits)
            {
                // 忽略 PhysicsRaycaster：手指放在 3D 星球上仍然可以缩放。
                if (hit.module is GraphicRaycaster) return true;
            }
            return false;
        }
    }
}
