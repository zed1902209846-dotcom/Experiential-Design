using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SolarSystemAR
{
    /// 共用一盏相机方向的主光，并立即填充环境光，避免刚识别时背光或等待环境探针更新。
    /// 太阳卡与行星卡同时出现时切换为「太阳卡光源」：主光淡出，每颗行星由太阳卡所在方向照亮。
    [DisallowMultipleComponent]
    public class ARSceneLighting : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float keyIntensity = 1.35f;
        [SerializeField] private Color ambientColor = new Color(0.8f, 0.8f, 0.8f);

        [Header("太阳卡光源")]
        [SerializeField, Min(0f)] private float sunIntensity = 1.7f;
        [SerializeField] private Color sunColor = new Color(1f, 0.96f, 0.88f);
        // 夜面保留一点点亮度，访客仍能看出背光半球的轮廓。
        [SerializeField] private Color nightAmbientColor = new Color(0.07f, 0.075f, 0.1f);
        [SerializeField, Min(0.01f)] private float blendSeconds = 0.4f;

        private ScanPlanetContent sun;
        private readonly Dictionary<ScanPlanetContent, bool> lit = new Dictionary<ScanPlanetContent, bool>(); // 值：是否正面补光
        private readonly Dictionary<ScanPlanetContent, Light> lamps = new Dictionary<ScanPlanetContent, Light>();
        private readonly List<ScanPlanetContent> stale = new List<ScanPlanetContent>();
        // 素材着色器 PlanetRotationAndMask 把一张「夜面」遮罩乘进了贴图，等于自带一块跟着模型转的假夜面。
        // 太阳卡光源下昼夜由真实受光决定，期间用白图顶掉这张遮罩。
        private static readonly int BakedNightTexture = Shader.PropertyToID("Texture2D_BF5AEFF2");
        private MaterialPropertyBlock block;
        // 每盏太阳灯占一个独立的渲染层，只照自己那颗星球：卡片挨得再近也不会照到邻居的背光面。
        // 第 0 层留给主光；URP 的灯光层只有 8 位，1..7 足够同时追踪的 5 张卡使用。
        private readonly Dictionary<ScanPlanetContent, int> layers = new Dictionary<ScanPlanetContent, int>();
        private float sunBlend;
        private float appliedBlend = -1f;

        /// 太阳卡光源是否生效：需要太阳卡，外加至少一颗可被照亮的行星。
        public bool SunActive { get; private set; }

        /// 追踪集合变化时调用（键为 PlanetData id）。
        public void Sync(IReadOnlyDictionary<string, ScanPlanetContent> tracked)
        {
            sun = null;
            lit.Clear();
            bool anyPlanet = false;
            foreach (var pair in tracked)
            {
                if (pair.Value == null) continue;
                // 太阳自身和整幅太阳系模型没有「被太阳卡照亮」的意义，保持朝向镜头的补光。
                bool frontLit = pair.Key == "sun" || pair.Key == "solarsystem";
                if (pair.Key == "sun") sun = pair.Value;
                anyPlanet |= !frontLit;
                lit[pair.Value] = frontLit;
            }
            SunActive = sun != null && anyPlanet;
        }

        private Light key;
        private bool createdKey;
        private Quaternion originalRotation;
        private float originalIntensity;
        private Color originalColor;
        private LightShadows originalShadows;
        private int originalMask;
        private Light originalSun;
        private AmbientMode originalAmbientMode;
        private Color originalAmbientColor;
        private float originalAmbientIntensity;
        private SphericalHarmonicsL2 originalProbe;

        private void OnEnable()
        {
            originalSun = RenderSettings.sun;
            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientColor = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;
            originalProbe = RenderSettings.ambientProbe;

            // 优先复用已有主光，所有卡片共用，避免每出现一颗星球就增加灯光。
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional || !light.isActiveAndEnabled ||
                    light.gameObject.scene != gameObject.scene) continue;
                key = light;
                break;
            }
            if (key == null)
            {
                var host = new GameObject("AR Planet Key Light");
                host.transform.SetParent(transform, false);
                key = host.AddComponent<Light>();
                key.type = LightType.Directional;
                createdKey = true;
            }

            originalRotation = key.transform.rotation;
            originalIntensity = key.intensity;
            originalColor = key.color;
            originalShadows = key.shadows;
            originalMask = key.cullingMask;
            key.intensity = keyIntensity;
            key.color = Color.white;
            key.shadows = LightShadows.None;
            key.cullingMask = ~(1 << 8); // 知识卡的离屏预览有自己的灯光。
            RenderSettings.sun = key;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
            RenderSettings.ambientIntensity = 1f;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(ambientColor.linear);
            RenderSettings.ambientProbe = probe;
            LateUpdate();
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (key != null && camera != null)
                key.transform.rotation = camera.transform.rotation * Quaternion.Euler(25f, -25f, 0f);
            UpdateSunlight(camera);
        }

        private void UpdateSunlight(Camera camera)
        {
            if (key == null) return;
            sunBlend = Mathf.MoveTowards(sunBlend, SunActive ? 1f : 0f, Time.deltaTime / blendSeconds);
            if (!Mathf.Approximately(sunBlend, appliedBlend))
            {
                appliedBlend = sunBlend;
                key.intensity = Mathf.Lerp(keyIntensity, 0f, sunBlend);
                var ambient = Color.Lerp(ambientColor, nightAmbientColor, sunBlend);
                RenderSettings.ambientLight = ambient;
                var probe = new SphericalHarmonicsL2();
                probe.AddAmbientLight(ambient.linear);
                RenderSettings.ambientProbe = probe;
            }

            stale.Clear();
            foreach (var pair in lamps)
                if (pair.Key == null || !lit.ContainsKey(pair.Key) || sunBlend <= 0f) stale.Add(pair.Key);
            foreach (var content in stale)
            {
                if (lamps[content] != null) Destroy(lamps[content].gameObject);
                lamps.Remove(content);
                ReleaseRenderers(content);
            }
            if (sunBlend <= 0f) return;

            foreach (var pair in lit)
            {
                var content = pair.Key;
                if (content == null) continue;
                var center = content.AnchorWorldPosition;
                float radius = content.VisualWorldBounds.extents.x;
                // 太阳和太阳系模型从镜头方向补光，朝向访客的一面始终是亮的；
                // 太阳卡刚丢失、行星灯正在淡出时也沿用这个方向，避免灯光跳变。
                var front = camera != null ? camera.transform.position : center - key.transform.forward;
                var source = pair.Value || sun == null ? front : sun.AnchorWorldPosition;
                bool visible = content.Model != null && content.Model.activeInHierarchy;
                SunLampMath.Pose pose = default;
                bool usable = visible && SunLampMath.TryCompute(source, center, radius, sunIntensity * sunBlend, out pose);
                if (!lamps.TryGetValue(content, out var lamp) || lamp == null)
                {
                    if (!usable) continue;
                    lamp = CreateLamp(content.name);
                    lamps[content] = lamp;
                    ClaimRenderers(content, lamp, !pair.Value);
                }
                lamp.enabled = usable;
                if (!usable) continue;
                lamp.transform.position = pose.position;
                lamp.range = pose.range;
                lamp.intensity = pose.intensity;
            }
        }

        private void ClaimRenderers(ScanPlanetContent content, Light lamp, bool hideBakedNight)
        {
            int layer = 0;
            for (int candidate = 1; candidate < 8 && layer == 0; candidate++)
                if (!layers.ContainsValue(candidate)) layer = candidate;
            // 层用完时退回默认层：灯仍然工作，只是可能照到相邻的星球。
            // URP 读的是它自己的灯光数据，直接改 Light.renderingLayerMask 不生效。
            lamp.GetUniversalAdditionalLightData().renderingLayers = (RenderingLayerMask)(1u << layer);
            if (layer != 0) layers[content] = layer;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var renderer in content.Model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.renderingLayerMask |= 1u << layer;
                var material = renderer.sharedMaterial;
                if (!hideBakedNight || material == null || !material.HasProperty(BakedNightTexture)) continue;
                renderer.GetPropertyBlock(block);
                block.SetTexture(BakedNightTexture, Texture2D.whiteTexture);
                renderer.SetPropertyBlock(block);
            }
        }

        private void ReleaseRenderers(ScanPlanetContent content)
        {
            bool claimed = layers.TryGetValue(content, out int layer);
            layers.Remove(content);
            if (content == null || content.Model == null) return;
            foreach (var renderer in content.Model.GetComponentsInChildren<Renderer>(true))
            {
                if (claimed) renderer.renderingLayerMask &= ~(1u << layer);
                var material = renderer.sharedMaterial;
                if (material != null && material.HasProperty(BakedNightTexture)) renderer.SetPropertyBlock(null);
            }
        }

        private Light CreateLamp(string contentName)
        {
            var host = new GameObject("Sunlight on " + contentName);
            host.transform.SetParent(transform, false);
            var lamp = host.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = sunColor;
            lamp.shadows = LightShadows.None;
            lamp.cullingMask = ~(1 << 8);
            return lamp;
        }

        private void OnDisable()
        {
            RenderSettings.sun = originalSun;
            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientColor;
            RenderSettings.ambientIntensity = originalAmbientIntensity;
            RenderSettings.ambientProbe = originalProbe;
            if (key != null)
            {
                if (createdKey) Destroy(key.gameObject);
                else
                {
                    key.transform.rotation = originalRotation;
                    key.intensity = originalIntensity;
                    key.color = originalColor;
                    key.shadows = originalShadows;
                    key.cullingMask = originalMask;
                }
            }
            foreach (var pair in lamps)
            {
                if (pair.Value != null) Destroy(pair.Value.gameObject);
                ReleaseRenderers(pair.Key);
            }
            lamps.Clear();
            layers.Clear();
            sunBlend = 0f;
            appliedBlend = -1f;
            key = null;
            createdKey = false;
        }
    }
}
