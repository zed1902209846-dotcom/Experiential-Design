using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 3D 行星预览相机：把行星渲染到 RenderTexture，UI 里用 RawImage 显示。
    /// 相机藏在场景角落，Culling Mask 只拍 PreviewLayer（第 8 层 "PlanetPreview"）。
    /// 行星 prefab 实例化后放到该层并自转。
    public class PlanetPreviewRig : MonoBehaviour
    {
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject[] planetPrefabs; // 按 prefab 名（小写）匹配 planetId
        [SerializeField] private float modelDiameter = 1.6f;
        [SerializeField] private float rotationSpeed = 25f;

        private RenderTexture rt;
        private GameObject instance;

        public RenderTexture Texture => rt;

        private void Awake()
        {
            rt = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            rt.Create();
            if (previewCamera != null) previewCamera.targetTexture = rt;
        }

        public void Show(string planetId)
        {
            if (instance != null) Destroy(instance);
            var prefab = LoadPrefab(planetId);
            if (prefab == null) return;

            instance = Instantiate(prefab, spawnPoint);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // 放入预览层（含所有子物体）
            SetLayerRecursively(instance, spawnPoint.gameObject.layer);

            // 缩放到固定直径
            var rend = instance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var s = rend.bounds.size;
                float maxDim = Mathf.Max(s.x, s.y, s.z);
                if (maxDim > 0.0001f)
                    instance.transform.localScale = instance.transform.localScale * (modelDiameter / maxDim);
            }

            // 移除素材包自带报错脚本
            foreach (var mb in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null && mb.GetType().Name == "RotateArround")
                    Destroy(mb);

            var spin = instance.GetComponent<SelfRotation>();
            if (spin == null) spin = instance.AddComponent<SelfRotation>();
            spin.degreesPerSecond = rotationSpeed;
        }

        public void Hide()
        {
            if (instance != null) Destroy(instance);
        }

        private GameObject LoadPrefab(string planetId)
        {
            if (planetPrefabs == null) return null;
            foreach (var p in planetPrefabs)
                if (p != null && (p.name.ToLowerInvariant() == planetId ||
                    (planetId == "saturn" && p.name == "SaturnWithRings")))
                    return p;
            Debug.LogWarning("PlanetPreviewRig: 未绑定 prefab " + planetId);
            return null;
        }

        private void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
