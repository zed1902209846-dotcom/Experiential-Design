using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 科普卡片弹窗：整张设计图 + 半透明暗背景，CLOSE 关闭。
    public class InfoCardPopup : MonoBehaviour
    {
        [SerializeField] private Image cardImage;

        public void Show(Sprite card)
        {
            if (cardImage != null) cardImage.sprite = card;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
