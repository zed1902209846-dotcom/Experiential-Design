using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 3D 动态科普知识卡面板控制（本组件挂在活跃物体上，panel 为弹层根节点，默认隐藏）：
    /// 左侧 RawImage 显示 PlanetPreviewRig 的 RenderTexture（3D 自转模型），
    /// 右侧四段结构化文本（What is it? / How Special? / Life / Fun Fact），
    /// 顶部 8 行星 Tabs 可切换，右上角 X 关闭。
    public class PlanetCardController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panel;

        [Header("Preview")]
        [SerializeField] private PlanetPreviewRig previewRig;
        [SerializeField] private RawImage previewImage;

        [Header("Texts")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text taglineText;
        [SerializeField] private Text whatText;
        [SerializeField] private Text specialText;
        [SerializeField] private Text lifeText;
        [SerializeField] private Text funText;

        [Header("Tabs")]
        [SerializeField] private Button[] tabButtons;   // 顺序对应 PlanetData.All

        [Header("Key Facts")]
        [SerializeField] private Text[] statLabels;     // 顺序：ORDER / DIAMETER / DISTANCE / ORBIT / ROTATION / MOONS
        [SerializeField] private Text[] statValues;

        private string currentId;

        private void Awake()
        {
            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length && i < PlanetData.All.Count; i++)
                {
                    if (tabButtons[i] == null)
                    {
                        Debug.LogError("PlanetCardController: tabButtons[" + i + "] 未绑定 Button（Inspector 数组第 " + (i + 1) + " 个元素为空），请在该场景的 PlanetCardPanel 上补全绑定。", this);
                        continue;
                    }
                    int idx = i;
                    string label = PlanetData.All[idx].display;
                    tabButtons[i].onClick.AddListener(() => ShowPlanet(PlanetData.All[idx].id));
                    var t = tabButtons[i].GetComponentInChildren<Text>();
                    if (t != null) t.text = label;
                }
            }
            if (previewImage != null && previewRig != null && previewRig.Texture != null)
                previewImage.texture = previewRig.Texture;
            if (panel != null) panel.SetActive(false);
        }

        /// 显示指定行星的知识卡（由识别事件、Tab 点击或悬浮按钮调用）。
        public void ShowPlanet(string planetId)
        {
            var info = PlanetData.Find(planetId);
            if (info == null)
            {
                Debug.LogWarning("PlanetCardController: 未知行星 " + planetId);
                return;
            }
            currentId = info.id;

            if (titleText != null) titleText.text = info.display;
            if (taglineText != null) taglineText.text = info.tagline;
            if (whatText != null) whatText.text = info.what;
            if (specialText != null) specialText.text = info.special;
            if (lifeText != null) lifeText.text = info.life;
            if (funText != null) funText.text = info.fun;

            ApplyStats(info);
            UpdateCardArt(info);

            if (previewRig != null) previewRig.Show(info.id);
            if (panel != null) panel.SetActive(true);
        }

        /// 成品卡片图：Resources/Cards/<Display>Card，存在则显示在 CardArt 上。
        private void UpdateCardArt(PlanetData.Info info)
        {
            if (panel == null) return;
            var art = panel.transform.Find("Card/CardArt");
            if (art == null) return;
            var img = art.GetComponent<Image>();
            if (img == null) return;

            var sprite = Resources.Load<Sprite>("Cards/" + info.display + "Card");
            if (sprite == null) return;

            img.sprite = sprite;
            img.enabled = true;
            art.gameObject.SetActive(true);

            // 卡片比例跟随图片，避免横版图被塞进竖版面板后出现大块空白。
            var cardRt = art.transform.parent as RectTransform;
            if (cardRt != null && sprite.rect.width > 1f)
            {
                float w = 1020f;
                float h = w * (sprite.rect.height / sprite.rect.width);
                cardRt.sizeDelta = new Vector2(w, h);

                var close = cardRt.Find("CloseButton") as RectTransform;
                if (close != null) close.anchoredPosition = new Vector2(w * 0.5f - 62f, h * 0.5f - 62f);
            }

            // 成品卡片图已包含全部文案：隐藏旧的手工文字/预览/Tab，仅保留右上角关闭按钮。
            var parent = art.transform.parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child == art.transform) continue;
                if (child.name == "CloseButton") continue;
                child.gameObject.SetActive(false);
            }
        }

        /// KEY FACTS 六行：顺序 / 直径 / 日距 / 轨道周期 / 自转周期 / 卫星数。
        /// 数据留空的行整行隐藏，避免出现空白占位。
        private void ApplyStats(PlanetData.Info info)
        {
            if (statLabels == null || statValues == null) return;

            // PlanetStats 为全目标的权威六项数据；命中时整组使用（空字符串代表该行不适用，会整行隐藏）。
            // 未命中时回退到 PlanetData.Info 里自带的字段。
            string[] extra = PlanetStats.Get(info.id);
            string[] baseValues = { info.order, info.diameter, info.distance, info.orbit, info.rotation, info.moons };

            for (int i = 0; i < statLabels.Length; i++)
            {
                string value = extra != null && i < extra.Length
                    ? extra[i]
                    : (i < baseValues.Length ? baseValues[i] : null);
                bool has = !string.IsNullOrEmpty(value);
                if (has && i < statValues.Length && statValues[i] != null) statValues[i].text = value;
                if (statLabels[i] != null) statLabels[i].gameObject.SetActive(has);
                if (i < statValues.Length && statValues[i] != null) statValues[i].gameObject.SetActive(has);
            }
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (previewRig != null) previewRig.Hide();
        }

        public bool IsOpen => panel != null && panel.activeSelf;
        public string CurrentId => currentId;
    }
}
