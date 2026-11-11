using UnityEngine;
using UnityEngine.Rendering;

namespace SolarSystemAR
{
    /// 共用一盏相机方向的主光，并立即填充环境光，避免刚识别时背光或等待环境探针更新。
    [DisallowMultipleComponent]
    public class ARSceneLighting : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float keyIntensity = 1.35f;
        [SerializeField] private Color ambientColor = new Color(0.8f, 0.8f, 0.8f);

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
            key = null;
            createdKey = false;
        }
    }
}
