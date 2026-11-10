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
            instance = Instantiate(planetPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(-modelTilt, 0f, 0f);

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
    }
}
