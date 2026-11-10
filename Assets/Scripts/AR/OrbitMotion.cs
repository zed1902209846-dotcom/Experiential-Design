using UnityEngine;

namespace SolarSystemAR
{
    /// 行星公转：绕父物体（轨道中心）在 XZ 平面上做圆周运动。
    /// ExecuteAlways 使场景编辑模式下也能预览轨道布局（Simulate Found 前 Game 视图可见）。
    [ExecuteAlways]
    public class OrbitMotion : MonoBehaviour
    {
        [SerializeField] private float orbitRadius = 0.2f;   // 轨道半径（米，相对父物体）
        [SerializeField] private float orbitSpeed = 20f;     // 公转角速度（度/秒）
        [SerializeField] private float phaseAngle = 0f;      // 初始相位角（度）
        [SerializeField] private bool clockwise = true;      // 公转方向
        [SerializeField] private float orbitTilt = 0f;       // 轨道面倾斜角（度，X 轴）

        private float angle;

        private void OnEnable()
        {
            angle = phaseAngle;
            Apply();
        }

        private void Update()
        {
            if (Application.isPlaying)
                angle += (clockwise ? -1f : 1f) * orbitSpeed * Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
            if (Mathf.Abs(orbitTilt) > 0.01f) p = Quaternion.Euler(orbitTilt, 0f, 0f) * p;
            transform.localPosition = p;
        }
    }
}
