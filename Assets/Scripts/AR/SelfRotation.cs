using UnityEngine;

namespace SolarSystemAR
{
    /// AR 天体本地自转，替代原 RotateArround（其 pivotObject 可能为空，存在空引用风险）。
    public class SelfRotation : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 20f;

        private void Update()
        {
            transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
