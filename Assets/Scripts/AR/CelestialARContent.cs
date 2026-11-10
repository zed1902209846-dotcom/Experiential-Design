using UnityEngine;
using UnityEngine.EventSystems;

namespace SolarSystemAR
{
    /// 太阳系仪上某颗行星的 AR 内容（挂在每个锚点物体上）。
    /// 整体 Model Target 识别成功后由 AROrreryController 统一初始化；
    /// 点击 3D 行星（PhysicsRaycaster + 冒泡）选中并弹出信息。
    public class CelestialARContent : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CelestialBodyData data;
        [SerializeField] private GameObject surfacePrefab;
        [SerializeField] private GameObject nucleusPrefab;
        [Tooltip("AR 模型直径（米），应与实体塑料球等大以便覆盖")]
        [SerializeField] private float modelDiameter = 0.04f;
        [SerializeField] private float rotationSpeed = 15f;

        private GameObject surfaceInstance;
        private GameObject nucleusInstance;
        private bool showingNucleus;
        private bool initialized;
        private float baseScale = 1f;

        public CelestialBodyData Data => data;
        public bool HasNucleus => nucleusPrefab != null;
        public bool IsShowingNucleus => showingNucleus;

        /// 整体目标识别成功：实例化表面模型与剖切模型（剖切默认隐藏）。
        public void OnTrackingFound()
        {
            if (!initialized)
            {
                if (surfacePrefab != null)
                {
                    surfaceInstance = Instantiate(surfacePrefab, transform);
                    baseScale = modelDiameter;
                    surfaceInstance.transform.localScale = Vector3.one * modelDiameter;
                    EnsureCollider(surfaceInstance);
                    var rot = surfaceInstance.GetComponent<SelfRotation>();
                    if (rot == null) rot = surfaceInstance.AddComponent<SelfRotation>();
                    rot.degreesPerSecond = rotationSpeed;
                }
                if (nucleusPrefab != null)
                {
                    nucleusInstance = Instantiate(nucleusPrefab, transform);
                    nucleusInstance.transform.localScale = Vector3.one * modelDiameter;
                    EnsureCollider(nucleusInstance);
                    nucleusInstance.SetActive(false);
                }
                initialized = true;
            }
            SetInstancesVisible(true);
        }

        public void OnTrackingLost()
        {
            SetInstancesVisible(false);
        }

        /// 表面 / 内部剖切结构互换（视频参考：地壳-地幔-地核分层）。
        public void ToggleStructure()
        {
            if (!HasNucleus) return;
            showingNucleus = !showingNucleus;
            if (surfaceInstance != null) surfaceInstance.SetActive(!showingNucleus);
            if (nucleusInstance != null) nucleusInstance.SetActive(showingNucleus);
        }

        public void SetSelected(bool selected)
        {
            var target = initialized ? baseScale * (selected ? 1.15f : 1f) : 1f;
            if (surfaceInstance != null) surfaceInstance.transform.localScale = Vector3.one * target;
            if (nucleusInstance != null) nucleusInstance.transform.localScale = Vector3.one * target;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var orrery = GetComponentInParent<AROrreryController>();
            if (orrery != null) orrery.Select(this);
        }

        private void SetInstancesVisible(bool visible)
        {
            if (surfaceInstance != null) surfaceInstance.SetActive(visible && !showingNucleus);
            if (nucleusInstance != null) nucleusInstance.SetActive(visible && showingNucleus);
        }

        private void EnsureCollider(GameObject go)
        {
            if (go.GetComponentInChildren<Collider>() == null)
            {
                var mesh = go.GetComponentInChildren<MeshFilter>();
                var host = mesh != null ? mesh.gameObject : go;
                var col = host.AddComponent<SphereCollider>();
                col.isTrigger = true;
            }
        }
    }
}
