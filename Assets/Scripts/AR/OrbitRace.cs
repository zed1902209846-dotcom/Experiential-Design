using UnityEngine;

namespace SolarSystemAR
{
    /// 公转实验的 AR 动画：两颗行星离开各自的卡片，绕同一个太阳转圈。
    /// 年更短的行星走内圈（现实中也是离太阳更近的一颗），外圈按真实的周期比例落后。
    /// 桌上有太阳卡时绕太阳卡转；没有时在两颗行星中间放一个临时的小太阳。
    public sealed class OrbitRace
    {
        private const int RingPoints = 72;
        private readonly GameObject root;
        private readonly Transform marker;
        private readonly Material markerMaterial, innerMaterial, outerMaterial;
        private readonly LineRenderer innerRing, outerRing;
        private readonly Vector3[] ringBuffer = new Vector3[RingPoints];
        private ScanPlanetContent inner, outer, sun;
        private Vector3 innerRestore, outerRestore;

        public bool Active { get; private set; }
        /// 整个轨道占据的空间，面板排版时要避开它。
        public Bounds WorldBounds { get; private set; }

        public OrbitRace(Transform parent, Material source)
        {
            root = new GameObject("Orbit Race");
            root.transform.SetParent(parent, false);
            markerMaterial = new Material(source);
            markerMaterial.SetColor("_BaseColor", new Color(1f, .78f, .2f));
            innerMaterial = new Material(source);
            outerMaterial = new Material(source);
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Stand-in Sun";
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.GetComponent<Renderer>().sharedMaterial = markerMaterial;
            marker = sphere.transform;
            marker.SetParent(root.transform, false);
            innerRing = Ring("Inner Orbit", innerMaterial);
            outerRing = Ring("Outer Orbit", outerMaterial);
            root.SetActive(false);
        }

        private LineRenderer Ring(string name, Material material)
        {
            var host = new GameObject(name);
            host.transform.SetParent(root.transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = RingPoints;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        /// sunCard 为 null 表示桌上没有太阳卡。
        public void Begin(ScanPlanetContent innerPlanet, Color innerColor, ScanPlanetContent outerPlanet, Color outerColor,
            ScanPlanetContent sunCard)
        {
            End();
            inner = innerPlanet;
            outer = outerPlanet;
            sun = sunCard;
            innerRestore = inner.Model.transform.localPosition;
            outerRestore = outer.Model.transform.localPosition;
            innerMaterial.SetColor("_BaseColor", innerColor);
            outerMaterial.SetColor("_BaseColor", outerColor);
            Active = true;
            root.SetActive(true);
        }

        /// glide：0→1，行星从卡片上滑到起跑位置；laps：各自已经绕了多少圈。
        public void Update(float glide, float innerLaps, float outerLaps)
        {
            if (!Active) return;
            if (inner == null || outer == null || inner.Model == null || outer.Model == null) { End(); return; }
            var innerHome = inner.transform.TransformPoint(innerRestore);
            var outerHome = outer.transform.TransformPoint(outerRestore);
            bool realSun = sun != null && sun.Model != null && sun.Model.activeInHierarchy;
            var center = realSun ? sun.AnchorWorldPosition : (innerHome + outerHome) * .5f;

            // 轨道平面与卡片平行；卡片被拿歪时取两张卡的平均朝向。
            var normal = (inner.transform.up + outer.transform.up).normalized;
            if (normal.sqrMagnitude < .5f) normal = inner.transform.up;
            var axis = Vector3.ProjectOnPlane(innerHome - center, normal);
            if (axis.sqrMagnitude < 1e-8f) axis = Vector3.ProjectOnPlane(inner.transform.right, normal);
            axis.Normalize();
            var side = Vector3.Cross(normal, axis);

            float innerRadius = inner.VisualWorldBounds.extents.x, outerRadius = outer.VisualWorldBounds.extents.x;
            float centerRadius = realSun ? sun.VisualWorldBounds.extents.x : Mathf.Min(innerRadius, outerRadius) * .6f;
            float innerOrbit = centerRadius + innerRadius * 1.3f;
            float outerOrbit = innerOrbit + innerRadius + outerRadius * 1.2f;

            // 各自从靠近自己卡片的那一侧起跑，这样滑到起点的路最短，也不会穿过太阳。
            float outerStart = StartAngle(outerHome - center, axis, side);
            float eased = Mathf.SmoothStep(0f, 1f, glide);
            inner.Model.transform.position = Vector3.Lerp(innerHome,
                Point(center, axis, side, innerOrbit, innerLaps * Mathf.PI * 2f), eased);
            outer.Model.transform.position = Vector3.Lerp(outerHome,
                Point(center, axis, side, outerOrbit, outerStart + outerLaps * Mathf.PI * 2f), eased);

            marker.gameObject.SetActive(!realSun);
            marker.position = center;
            marker.localScale = Vector3.one * (centerRadius * 2f / Mathf.Max(1e-5f, root.transform.lossyScale.x));
            DrawRing(innerRing, center, axis, side, innerOrbit, innerRadius);
            DrawRing(outerRing, center, axis, side, outerOrbit, innerRadius);
            // 轨道是一张薄圆盘：只围住圆盘本身，不用整个立方体，给面板多留些位置。
            float reach = outerOrbit + outerRadius;
            var bounds = new Bounds(center + normal * outerRadius, Vector3.zero);
            bounds.Encapsulate(center - normal * outerRadius);
            for (int i = 0; i < 8; i++) bounds.Encapsulate(Point(center, axis, side, reach, i * Mathf.PI * .25f));
            WorldBounds = bounds;
        }

        private static float StartAngle(Vector3 offset, Vector3 axis, Vector3 side) =>
            Mathf.Atan2(Vector3.Dot(offset, side), Vector3.Dot(offset, axis));

        private static Vector3 Point(Vector3 center, Vector3 axis, Vector3 side, float radius, float angle) =>
            center + (axis * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * radius;

        private void DrawRing(LineRenderer line, Vector3 center, Vector3 axis, Vector3 side, float radius, float planetRadius)
        {
            for (int i = 0; i < RingPoints; i++)
                ringBuffer[i] = Point(center, axis, side, radius, i * Mathf.PI * 2f / RingPoints);
            line.SetPositions(ringBuffer);
            line.widthMultiplier = planetRadius * .07f;
        }

        /// 把两颗行星放回各自的卡片上。
        public void End()
        {
            if (!Active) return;
            Active = false;
            if (inner != null && inner.Model != null) inner.Model.transform.localPosition = innerRestore;
            if (outer != null && outer.Model != null) outer.Model.transform.localPosition = outerRestore;
            inner = outer = sun = null;
            if (root != null) root.SetActive(false);
        }

        public void Dispose()
        {
            End();
            if (root != null) Object.Destroy(root);
            Object.Destroy(markerMaterial);
            Object.Destroy(innerMaterial);
            Object.Destroy(outerMaterial);
        }
    }
}
