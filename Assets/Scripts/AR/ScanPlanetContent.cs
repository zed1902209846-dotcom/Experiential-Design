using UnityEngine;

namespace SolarSystemAR
{
    /// 识别成功后的行星内容：实例化 prefab、按目标直径缩放、附加自转。
    public class ScanPlanetContent : MonoBehaviour
    {
        [SerializeField] private GameObject planetPrefab;
        // 单位：米。模型最大边会被归一化到这个值。
        // 实体打印卡宽 0.06 m，取卡面宽 50% → 0.03。
        [SerializeField] private float modelDiameter = 0.03f;
        [SerializeField] private float rotationSpeed = 20f;
        // 让星球以赤道/侧面展示而不是把极点对淮镜头（单位：度，绕卡片 X 轴前倾）。
        [SerializeField] private float modelTilt = 60f;

        private GameObject instance;
        private float localVisualRadius;
        private float localBodyRadius;
        public GameObject Model => instance;
        public Vector3 AnchorWorldPosition => instance != null ? instance.transform.position : transform.position;
        // Use the non-rotating parent scale: lossyScale on a rotated child can vary under a
        // non-uniformly scaled target. These conservative spheres cover every visual orientation.
        private float WorldScale => instance == null ? 0 : MaxScale(transform.lossyScale) * MaxScale(instance.transform.localScale);
        public Bounds VisualWorldBounds => new Bounds(AnchorWorldPosition, Vector3.one * (2f * localVisualRadius * WorldScale));
        public Bounds BodyWorldBounds => new Bounds(AnchorWorldPosition, Vector3.one * (2f * localBodyRadius * WorldScale));

        private static float MaxScale(Vector3 scale) => Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        public bool TryGetScreenBounds(Camera camera, RectTransform canvas, out Rect rect)
        {
            rect = Rect.zero;
            if (instance == null || !instance.activeInHierarchy || camera == null || canvas == null) return false;
            var bounds = VisualWorldBounds;
            var center = camera.WorldToScreenPoint(bounds.center);
            if (center.z <= 0) return false;
            if (center.z - bounds.extents.x <= camera.nearClipPlane)
            {
                rect = canvas.rect; // No safe label space if the camera is inside the model envelope.
                return true;
            }
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var point = camera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, point, null, out local);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.Overlaps(canvas.rect);
        }

        public void Show()
        {
            EnsureInstance();
            if (instance != null) instance.SetActive(true);
        }

        public void Hide()
        {
            if (instance != null) instance.SetActive(false);
        }

        private void EnsureInstance()
        {
            if (instance != null || planetPrefab == null) return;
            // A centered interaction/spin root isolates imported off-center prefab pivots.
            instance = new GameObject(planetPrefab.name + " Presentation");
            instance.transform.SetParent(transform, false);
            var visual = Instantiate(planetPrefab, instance.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            instance.transform.localPosition = Vector3.zero;

            // 用所有渲染器的并集包围盒归一化：多部件模型（如带环土星）不能只看第一个 Renderer，
            // 否则会按环或按球体之一缩放，导致尺寸与占比错误。
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            float maxDim = 0f;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var lb = r.localBounds.size;
                var ls = r.transform.lossyScale;
                var s = new Vector3(lb.x * Mathf.Abs(ls.x), lb.y * Mathf.Abs(ls.y), lb.z * Mathf.Abs(ls.z));
                maxDim = Mathf.Max(maxDim, Mathf.Max(s.x, Mathf.Max(s.y, s.z)));
            }
            if (maxDim > 0.0001f)
                instance.transform.localScale = instance.transform.localScale * (modelDiameter / maxDim);

            CacheStableGeometry(visual.transform, renderers);
            instance.transform.localRotation = Quaternion.Euler(-modelTilt, 0f, 0f);

            // 在归一化完成后记录初始大小，扫描场景与独立行星场景共用双指缩放。
            if (instance.GetComponent<PlanetPinchScale>() == null)
                instance.AddComponent<PlanetPinchScale>();

            // Inguz 行星 prefab 自带 RotateArround 组件且 pivotObject 未赋值，运行时每帧报空引用。
            // 实例化后按类型名移除（不修改原素材 prefab），统一由 SelfRotation 驱动自转。
            foreach (var mb in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb != null && mb.GetType().Name == "RotateArround")
                    Destroy(mb);
            }

            var spin = instance.GetComponent<SelfRotation>();
            if (spin == null) spin = instance.AddComponent<SelfRotation>();
            spin.degreesPerSecond = rotationSpeed;
        }

        private void CacheStableGeometry(Transform visual, Renderer[] renderers)
        {
            var pivot = instance.transform;
            Vector3 center = Vector3.zero;
            float bestBody = 0;
            bool found = false;
            var combined = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                var local = renderer.localBounds;
                var position = pivot.InverseTransformPoint(renderer.transform.TransformPoint(local.center));
                float radius = RendererRadius(renderer, pivot);
                var size = local.size;
                float max = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                float min = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                if (min >= max * .8f && radius > bestBody)
                {
                    bestBody = radius;
                    center = position;
                }
                var envelope = new Bounds(position, Vector3.one * radius * 2);
                if (!found) { combined = envelope; found = true; }
                else combined.Encapsulate(envelope);
            }
            if (bestBody <= 0) center = combined.center;
            visual.localPosition -= center;
            localVisualRadius = 0;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer) continue;
                var position = pivot.InverseTransformPoint(renderer.transform.TransformPoint(renderer.localBounds.center));
                localVisualRadius = Mathf.Max(localVisualRadius, position.magnitude + RendererRadius(renderer, pivot));
            }
            localBodyRadius = bestBody > 0 ? bestBody : localVisualRadius;
        }

        private static float RendererRadius(Renderer renderer, Transform pivot)
        {
            var extents = renderer.localBounds.extents;
            var x = pivot.InverseTransformVector(renderer.transform.TransformVector(Vector3.right * extents.x));
            var y = pivot.InverseTransformVector(renderer.transform.TransformVector(Vector3.up * extents.y));
            var z = pivot.InverseTransformVector(renderer.transform.TransformVector(Vector3.forward * extents.z));
            float max = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
            float min = Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z));
            // Round bodies need their radius, not the diagonal of an enclosing cube.
            if (min >= max * .8f) return Mathf.Max(x.magnitude, Mathf.Max(y.magnitude, z.magnitude));
            float radius = 0;
            for (int i = 0; i < 8; i++)
                radius = Mathf.Max(radius, (x * ((i & 1) == 0 ? -1 : 1)
                    + y * ((i & 2) == 0 ? -1 : 1) + z * ((i & 4) == 0 ? -1 : 1)).magnitude);
            return radius;
        }
    }
}
