using UnityEngine;

namespace SolarSystemAR
{
    /// Decorative, collider-free figure; sibling of the model so rotation cannot throw it off the top.
    public sealed class GravityStickFigure
    {
        private readonly GameObject root;
        private readonly Material material;
        private readonly ScanPlanetContent content;
        private readonly LineRenderer arms;
        private readonly System.Collections.Generic.List<LineRenderer> lines = new System.Collections.Generic.List<LineRenderer>();
        public Bounds SurfaceBounds => content != null ? content.BodyWorldBounds : new Bounds();
        public bool Ready => content != null && content.Model != null && content.Model.activeInHierarchy;
        public Transform Root => root.transform;

        public GravityStickFigure(ScanPlanetContent content, Transform parent, Color color, Material source)
        {
            this.content = content;
            root = new GameObject("Gravity Jumper " + content.name);
            root.transform.SetParent(parent, false);
            material = new Material(source);
            material.SetColor("_BaseColor", color);
            Segment("Body", new[] { new Vector3(0, .38f, 0), new Vector3(0, .77f, 0) });
            arms = Segment("Arms", new[] {new Vector3(-.24f,.46f,0),new Vector3(0,.68f,0),new Vector3(.24f,.46f,0)});
            Segment("Legs", new[] {new Vector3(-.2f,0,0),new Vector3(0,.38f,0),new Vector3(.2f,0,0)});
            var circle = new Vector3[25];
            for (int i=0;i<circle.Length;i++)
            {
                float angle = i * Mathf.PI * 2f / (circle.Length-1);
                circle[i] = new Vector3(Mathf.Cos(angle)*.12f,.89f+Mathf.Sin(angle)*.12f,0);
            }
            Segment("Head", circle);
        }

        private LineRenderer Segment(string name, Vector3[] points)
        {
            var host = new GameObject(name);
            host.transform.SetParent(root.transform, false);
            var line = host.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = .045f;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            lines.Add(line);
            return line;
        }

        public void Pose(Camera camera, float figureSize, float rise, bool airborne)
        {
            if (!Ready) { root.SetActive(false); return; }
            root.SetActive(true);
            var bounds = SurfaceBounds;
            float radius = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            root.transform.position = bounds.center + camera.transform.up * (radius + rise)
                - camera.transform.forward * radius * .04f;
            root.transform.rotation = camera.transform.rotation;
            var parentScale = root.transform.parent.lossyScale;
            root.transform.localScale = new Vector3(figureSize/Mathf.Max(.0001f,Mathf.Abs(parentScale.x)),
                figureSize/Mathf.Max(.0001f,Mathf.Abs(parentScale.y)),figureSize/Mathf.Max(.0001f,Mathf.Abs(parentScale.z)));
            // LineRenderer widths remain world-space even when local points follow a scaled root.
            foreach (var line in lines) line.widthMultiplier = figureSize * .045f;
            arms.SetPosition(0, new Vector3(-.24f, airborne ? .84f : .46f,0));
            arms.SetPosition(2, new Vector3(.24f, airborne ? .84f : .46f,0));
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root);
            if (material != null) Object.Destroy(material);
        }
    }
}
