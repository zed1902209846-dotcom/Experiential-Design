using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 可读的屏幕空间标注：位置跟随 AR 模型，文字不随模型自转。
    public class PlanetAnnotations : MonoBehaviour
    {
        private class Annotation
        {
            public ScanPlanetContent content;
            public PlanetData.Info info;
            public RectTransform label;
            public bool tracked;
            public bool visible;
            public Rect screenBounds;
            public Vector2 screenAnchor;
            public Profile profile;
            public bool open;
            public bool panelVisible;
        }

        private class Profile
        {
            public RectTransform panel, leader, viewport, close, track;
            public Text title, tagline, body;
            public ScrollRect scroll;
            public float lastBodyWidth = -1f;
        }

        private readonly Dictionary<ScanPlanetContent, Annotation> annotations = new Dictionary<ScanPlanetContent, Annotation>();
        private readonly HashSet<int> pressedPointers = new HashSet<int>();
        private readonly List<Rect> labelRects = new List<Rect>();
        private readonly List<Annotation> openAnnotations = new List<Annotation>();
        private readonly List<Annotation> visibleProfiles = new List<Annotation>();
        private readonly List<Rect> profileTargets = new List<Rect>();
        private readonly List<Rect> profileRects = new List<Rect>();
        private readonly List<Rect> planetRects = new List<Rect>();
        private readonly List<Rect> layoutObstacles = new List<Rect>();
        private GravityJumpComparison gravityLab;
        private readonly Color accent = new Color(0.32f, 0.88f, 0.96f);
        private RectTransform canvasRect;
        private Font font;
        private Texture2D profileAtlas;
        private ARScanUIController scanUI;
        private readonly Vector3[] hintCorners = new Vector3[4];
        private readonly Dictionary<string, Sprite> profileSkins = new Dictionary<string, Sprite>();
        // Art 图集：从左到右、从上到下；不依赖 PlanetData.All 的顺序。
        private static readonly string[] skinIds =
        {
            "mercury", "venus", "earth", "mars", "jupiter",
            "saturn", "uranus", "neptune", "sun", "solarsystem"
        };
        private Annotation lastOpened;
        private float suppressTapUntil;

        public bool IsOpen => openAnnotations.Count > 0;
        public int OpenPanelCount => openAnnotations.Count;
        public string SelectedPlanetId => lastOpened != null ? lastOpened.info.id : null;
        public bool CanOpenFromTap
        {
            get
            {
                if (TouchCount() >= 2) suppressTapUntil = Time.unscaledTime + 0.25f;
                return Time.unscaledTime >= suppressTapUntil;
            }
        }

        private void Awake()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            profileAtlas = Resources.Load<Texture2D>("UI/PlanetInfoPanels");
            scanUI = FindFirstObjectByType<ARScanUIController>();
            var root = new GameObject("PlanetAnnotations", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRect = root.GetComponent<RectTransform>();
            var canvas = root.GetComponent<Canvas>();
            // 面板与世界中的模型分离：相机移动或星球旋转时，介绍仍正对屏幕且文字不倾斜。
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0f;
        }

        private void Update()
        {
            int touches = TouchCount();
            if (touches >= 2) suppressTapUntil = Time.unscaledTime + 0.25f;
            if (touches == 0) pressedPointers.Clear();
        }

        public void PointerDown(int pointerId)
        {
            pressedPointers.Add(pointerId);
            if (pressedPointers.Count > 1 || TouchCount() >= 2)
                suppressTapUntil = Time.unscaledTime + 0.25f;
        }

        public void PointerUp(int pointerId)
        {
            pressedPointers.Remove(pointerId);
        }

        private static int TouchCount()
        {
            int count = 0;
            var screen = Touchscreen.current;
            if (screen != null)
                foreach (var touch in screen.touches)
                    if (touch.press.isPressed) count++;
            return count;
        }

        public void Track(ScanPlanetContent content, PlanetData.Info info)
        {
            if (!annotations.TryGetValue(content, out var annotation))
            {
                annotation = new Annotation { content = content, info = info };
                var label = NewRect("Name_" + info.id, canvasRect);
                label.sizeDelta = new Vector2(220, 64);
                var background = label.gameObject.AddComponent<Image>();
                background.color = new Color(0.02f, 0.055f, 0.09f, 0.92f);
                var outline = label.gameObject.AddComponent<Outline>();
                outline.effectColor = accent;
                outline.effectDistance = new Vector2(1, -1);
                var text = NewText("Name", label, DisplayName(info), 32, Color.white);
                Stretch(text.rectTransform, 10, 4, 10, 4);
                text.alignment = TextAnchor.MiddleCenter;
                text.fontStyle = FontStyle.Bold;
                label.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    if (CanOpenFromTap) Show(content);
                });
                annotation.label = label;
                annotations.Add(content, annotation);
                AddTapTarget(content);
            }
            annotation.tracked = true;
            annotation.label.gameObject.SetActive(true);
        }

        public void Untrack(ScanPlanetContent content)
        {
            if (!annotations.TryGetValue(content, out var annotation)) return;
            annotation.tracked = false;
            annotation.label.gameObject.SetActive(false);
            Close(content);
        }

        public void Show(ScanPlanetContent content)
        {
            if (!annotations.TryGetValue(content, out var annotation) || !annotation.tracked) return;
            if (annotation.profile == null) annotation.profile = BuildPanel(annotation);
            var profile = annotation.profile;
            lastOpened = annotation;
            // 收起状态点击名称时也提高优先级，优先为用户刚选中的星球安排阅读空间。
            if (annotation.open) openAnnotations.Remove(annotation);
            if (!annotation.open)
            {
                annotation.open = true;
                openAnnotations.Add(annotation);
                profile.title.text = DisplayName(annotation.info).ToUpperInvariant();
                profile.tagline.text = annotation.info.tagline;
                profile.body.text = Description(annotation.info);
                profile.lastBodyWidth = -1f;
                profile.scroll.StopMovement();
                profile.scroll.verticalNormalizedPosition = 1f;
            }
            else openAnnotations.Add(annotation);
            profile.panel.gameObject.SetActive(true);
            profile.panel.SetAsLastSibling();
            LateUpdate();
            Canvas.ForceUpdateCanvases();
            ResizeBody(profile);
        }

        public void Close(ScanPlanetContent content)
        {
            if (!annotations.TryGetValue(content, out var annotation)) return;
            annotation.open = false;
            openAnnotations.Remove(annotation);
            if (annotation.profile != null)
            {
                annotation.profile.panel.gameObject.SetActive(false);
                annotation.profile.leader.gameObject.SetActive(false);
            }
            if (lastOpened == annotation)
                lastOpened = openAnnotations.Count > 0 ? openAnnotations[openAnnotations.Count - 1] : null;
        }

        public void Close()
        {
            while (openAnnotations.Count > 0) Close(openAnnotations[openAnnotations.Count - 1].content);
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null || canvasRect == null) return;
            if (camera.GetComponent<PhysicsRaycaster>() == null) camera.gameObject.AddComponent<PhysicsRaycaster>();
            var safe = SafeRect();
            visibleProfiles.Clear();
            profileTargets.Clear();
            planetRects.Clear();
            foreach (var annotation in annotations.Values)
            {
                annotation.visible = annotation.tracked && Project(annotation, camera, out annotation.screenBounds);
                annotation.panelVisible = false;
                if (annotation.visible) planetRects.Add(annotation.screenBounds);
                annotation.label.gameObject.SetActive(annotation.visible && !annotation.open);
                if (annotation.profile == null) continue;
                annotation.profile.panel.gameObject.SetActive(annotation.visible && annotation.open);
                annotation.profile.leader.gameObject.SetActive(false);
            }
            // 打开顺序稳定；各介绍框拥有独立的内容、滚动状态和关闭按钮。
            for (int i = openAnnotations.Count - 1; i >= 0; i--)
            {
                var annotation = openAnnotations[i];
                if (!annotation.visible) continue;
                visibleProfiles.Add(annotation);
                profileTargets.Add(annotation.screenBounds);
            }
            layoutObstacles.Clear();
            layoutObstacles.AddRange(planetRects);
            if (gravityLab == null) gravityLab = GetComponent<GravityJumpComparison>();
            if (gravityLab != null) layoutObstacles.AddRange(gravityLab.ScreenExclusions);
            bool besidePlanets = PlanetPanelLayout.Arrange(safe, profileTargets, layoutObstacles, profileRects);
            for (int i = 0; i < visibleProfiles.Count; i++)
            {
                var annotation = visibleProfiles[i];
                var profile = annotation.profile;
                var rect = profileRects[i];
                annotation.panelVisible = rect.width > 0 && rect.height > 0;
                profile.panel.gameObject.SetActive(annotation.panelVisible);
                if (!annotation.panelVisible) continue;
                profile.panel.sizeDelta = rect.size;
                profile.panel.anchoredPosition = new Vector2(rect.xMin, rect.yMax);
                profile.panel.localRotation = Quaternion.identity;
                ApplyProfileLayout(profile, rect.height);
                ResizeBody(profile);
                // 密集排列时不画穿过其他卡片的连接线，卡片标题仍标明所属星球。
                if (besidePlanets) PositionLeader(profile, annotation.screenAnchor, rect);
            }

            labelRects.Clear();
            labelRects.AddRange(profileRects);
            foreach (var annotation in annotations.Values)
            {
                if (!annotation.visible || annotation.panelVisible) continue;
                Rect labelRect;
                bool placed = PlanetPanelLayout.PlaceLabel(safe, annotation.screenBounds, layoutObstacles, labelRects, out labelRect);
                annotation.label.gameObject.SetActive(placed);
                if (!placed) continue;
                annotation.label.sizeDelta = labelRect.size;
                annotation.label.anchoredPosition = labelRect.center;
                labelRects.Add(labelRect);
            }
        }

        private void ApplyProfileLayout(Profile profile, float height)
        {
            float header = height < 260 ? Mathf.Min(84, height * 0.5f) : 132;
            bool compact = header <= 100;
            var background = profile.panel.GetComponent<Image>();
            background.pixelsPerUnitMultiplier = compact ? 1.7f : 1f;
            float closeSize = Mathf.Clamp(header - 12, 40, 48);
            profile.close.sizeDelta = new Vector2(closeSize, closeSize);
            TopRect(profile.title.rectTransform, 18, 12, compact ? 88 : 124, compact ? 38 : 52);
            int bodySize = compact ? 24 : 28;
            if (profile.body.fontSize != bodySize)
            {
                profile.body.fontSize = bodySize;
                profile.lastBodyWidth = -1f;
            }
            profile.tagline.gameObject.SetActive(header > 100);
            TopRect(profile.tagline.rectTransform, 18, 72, 28, 48);
            Stretch(profile.viewport, 18, header, 28, 14);
            profile.track.offsetMin = new Vector2(-14, 14);
            profile.track.offsetMax = new Vector2(-8, -header);
        }

        private void PositionLeader(Profile profile, Vector2 anchor, Rect card)
        {
            var start = anchor;
            var end = new Vector2(Mathf.Clamp(start.x, card.xMin, card.xMax), Mathf.Clamp(start.y, card.yMin, card.yMax));
            var delta = end - start;
            profile.leader.gameObject.SetActive(delta.sqrMagnitude > 1f);
            profile.leader.anchoredPosition = (start + end) * 0.5f;
            profile.leader.sizeDelta = new Vector2(delta.magnitude, 2);
            profile.leader.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private void ResizeBody(Profile profile)
        {
            float width = Mathf.Max(1, profile.panel.rect.width - 46);
            if (Mathf.Approximately(profile.lastBodyWidth, width)) return;
            profile.lastBodyWidth = width;
            profile.body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            profile.body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, profile.body.preferredHeight + 16);
        }

        private Rect SafeRect()
        {
            var area = Screen.safeArea;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, area.min, null, out var min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, area.max, null, out var max);
            float bottom = min.y + 24;
            if (scanUI != null && scanUI.HintRect != null)
            {
                var hint = scanUI.HintRect;
                hint.GetWorldCorners(hintCorners);
                var hintCanvas = hint.GetComponentInParent<Canvas>();
                var hintCamera = hintCanvas != null && hintCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? hintCanvas.worldCamera : null;
                var screenTop = RectTransformUtility.WorldToScreenPoint(hintCamera, hintCorners[2]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenTop, null, out var hintTop);
                bottom = Mathf.Max(bottom, hintTop.y + 16);
            }
            return Rect.MinMaxRect(min.x + 16, bottom, max.x - 16, max.y - 84);
        }

        private bool Project(Annotation annotation, Camera camera, out Rect rect)
        {
            rect = Rect.zero;
            if (annotation.content == null || !annotation.content.TryGetScreenBounds(camera, canvasRect, out rect)) return false;
            var point = camera.WorldToScreenPoint(annotation.content.AnchorWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, point, null, out annotation.screenAnchor);
            return true;
        }

        private void AddTapTarget(ScanPlanetContent content)
        {
            var model = content.Model;
            var tap = model.GetComponent<PlanetTapTarget>();
            if (tap == null) tap = model.AddComponent<PlanetTapTarget>();
            tap.Initialize(this, content);
            // 给整个可视模型添加一个本地包围盒，土星环、太阳系组合模型也可点击。
            var bounds = new Bounds();
            bool found = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;
                var local = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var point = model.transform.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) return;
            var collider = model.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
        }

        private Profile BuildPanel(Annotation annotation)
        {
            var profile = new Profile();
            profile.leader = NewRect("PlanetConnector_" + annotation.info.id, canvasRect);
            var line = profile.leader.gameObject.AddComponent<Image>();
            line.color = accent;
            line.raycastTarget = false;
            profile.panel = NewRect("PlanetInfo_" + annotation.info.id, canvasRect);
            profile.panel.pivot = new Vector2(0, 1);
            var background = profile.panel.gameObject.AddComponent<Image>();
            var skin = ProfileSkin(annotation.info.id);
            if (skin != null)
            {
                background.sprite = skin;
                background.type = Image.Type.Sliced;
                background.color = Color.white;
            }
            else
            {
                background.color = new Color(0.025f, 0.055f, 0.09f, 0.97f);
                var border = profile.panel.gameObject.AddComponent<Outline>();
                border.effectColor = accent;
                border.effectDistance = new Vector2(1, -1);
            }

            profile.title = NewText("Title", profile.panel, "EARTH", 38, Color.white);
            TopRect(profile.title.rectTransform, 22, 20, 130, 52);
            profile.title.fontStyle = FontStyle.Bold;
            profile.title.verticalOverflow = VerticalWrapMode.Truncate;
            profile.title.resizeTextForBestFit = true; profile.title.resizeTextMinSize = 18; profile.title.resizeTextMaxSize = 38;
            profile.tagline = NewText("Tagline", profile.panel, "Our Only Home", 24, accent);
            profile.tagline.resizeTextForBestFit = true;
            profile.tagline.resizeTextMinSize = 18;
            profile.tagline.resizeTextMaxSize = 24;
            profile.tagline.verticalOverflow = VerticalWrapMode.Truncate;
            TopRect(profile.tagline.rectTransform, 22, 78, 140, 44);
            var close = NewRect("CloseButton", profile.panel);
            profile.close = close;
            close.anchorMin = close.anchorMax = Vector2.one;
            close.pivot = Vector2.one; close.anchoredPosition = new Vector2(-8, -8); close.sizeDelta = new Vector2(100, 100);
            close.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.08f);
            close.gameObject.AddComponent<Button>().onClick.AddListener(() => Close(annotation.content));
            var cross = NewText("Close", close, "×", 46, Color.white);
            Stretch(cross.rectTransform, 0, 0, 0, 0); cross.alignment = TextAnchor.MiddleCenter;

            profile.viewport = NewRect("Viewport", profile.panel);
            Stretch(profile.viewport, 22, 132, 32, 20);
            profile.viewport.gameObject.AddComponent<RectMask2D>();
            profile.viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            profile.scroll = profile.viewport.gameObject.AddComponent<ScrollRect>();
            profile.scroll.horizontal = false; profile.scroll.vertical = true; profile.scroll.viewport = profile.viewport;
            profile.scroll.movementType = ScrollRect.MovementType.Clamped;
            profile.scroll.scrollSensitivity = 28;
            profile.body = NewText("Description", profile.viewport, "", 32, new Color(0.91f, 0.95f, 0.98f));
            profile.body.supportRichText = true; profile.body.lineSpacing = 1.15f;
            profile.body.rectTransform.anchorMin = new Vector2(0, 1); profile.body.rectTransform.anchorMax = Vector2.one;
            profile.body.rectTransform.pivot = new Vector2(0.5f, 1); profile.body.rectTransform.anchoredPosition = Vector2.zero;
            profile.scroll.content = profile.body.rectTransform;
            var track = NewRect("ScrollTrack", profile.panel);
            profile.track = track;
            track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
            track.offsetMin = new Vector2(-14, 20); track.offsetMax = new Vector2(-8, -132);
            track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, 0.08f);
            var handle = NewRect("Handle", track);
            Stretch(handle, 0, 0, 0, 0);
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = accent;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            profile.scroll.verticalScrollbar = scrollbar;
            profile.scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            profile.panel.gameObject.SetActive(false);
            profile.leader.gameObject.SetActive(false);
            return profile;
        }

        private Sprite ProfileSkin(string planetId)
        {
            if (profileSkins.TryGetValue(planetId, out var skin)) return skin;
            int index = System.Array.IndexOf(skinIds, planetId);
            if (profileAtlas == null || index < 0) return null;
            int column = index % 5;
            int row = 1 - index / 5; // Unity 的纹理坐标原点在左下。
            int left = Mathf.RoundToInt(column * profileAtlas.width / 5f);
            int right = Mathf.RoundToInt((column + 1) * profileAtlas.width / 5f);
            int bottom = Mathf.RoundToInt(row * profileAtlas.height / 2f);
            int top = Mathf.RoundToInt((row + 1) * profileAtlas.height / 2f);
            // 内缩一像素避免相邻卡片串色；九宫格保留边框和右上角天体插图。
            skin = Sprite.Create(profileAtlas, new Rect(left + 1, bottom + 1, right - left - 2, top - bottom - 2),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 144, 112));
            skin.name = "PlanetInfo_" + planetId;
            profileSkins.Add(planetId, skin);
            return skin;
        }

        private string Description(PlanetData.Info info)
        {
            var text = new StringBuilder();
            text.Append("<b><color=#52E0F5>ABOUT</color></b>\n").Append(info.what);
            text.Append("\n\n<b><color=#52E0F5>WHAT MAKES IT SPECIAL</color></b>\n").Append(info.special);
            var values = PlanetStats.Get(info.id);
            if (values != null)
            {
                string[] labels = { "Order", "Diameter", "Distance from Sun", "Orbit", "Rotation", "Moons" };
                text.Append("\n\n<b><color=#52E0F5>KEY FACTS</color></b>");
                for (int i = 0; i < values.Length && i < labels.Length; i++)
                    if (!string.IsNullOrEmpty(values[i])) text.Append("\n<b>").Append(labels[i]).Append(":</b> ").Append(values[i]);
            }
            text.Append("\n\n<b><color=#52E0F5>LIFE</color></b>\n").Append(info.life);
            text.Append("\n\n<b><color=#52E0F5>DID YOU KNOW?</color></b>\n").Append(info.fun);
            return text.ToString();
        }

        private static string DisplayName(PlanetData.Info info) => info.id == "solarsystem" ? "Solar System" : info.display;

        private RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Text NewText(string name, Transform parent, string value, int size, Color color)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static void TopRect(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1); rect.offsetMin = new Vector2(left, -top - height); rect.offsetMax = new Vector2(-right, -top);
        }

        private void OnDestroy()
        {
            if (canvasRect != null) Destroy(canvasRect.gameObject);
            foreach (var skin in profileSkins.Values)
                if (skin != null) Destroy(skin);
            profileSkins.Clear();
        }
    }
}
