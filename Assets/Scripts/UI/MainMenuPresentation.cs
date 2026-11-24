using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// Places the museum introduction and scan entry inside the available screen area.
    [ExecuteAlways]
    public class MainMenuPresentation : MonoBehaviour
    {
        [SerializeField] private Image artwork;
        [SerializeField] private Button entry;
        private RectTransform artworkArea;
        private Text title;
        private Text instruction;
        private Rect previousCanvas, previousSafe;
        private bool dirty = true;

        public void Configure(Image background, Button start)
        {
            artwork = background;
            entry = start;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            var fitter = artwork.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (artwork.sprite != null) fitter.aspectRatio = artwork.sprite.rect.width / artwork.sprite.rect.height;
            dirty = true;
            ApplyLayout();
        }

        private void OnEnable() { dirty = true; }
        private void LateUpdate() { ApplyLayout(); }

        private void ApplyLayout()
        {
            if (!Application.isPlaying || artwork == null || entry == null || artwork.canvas == null) return;
            var root = artwork.canvas.rootCanvas;
            var canvas = (RectTransform)root.transform;
            var safe = Screen.safeArea;
            if (!dirty && canvas.rect == previousCanvas && safe == previousSafe) return;
            var camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, safe.min, camera, out var min)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, safe.max, camera, out var max)) return;

            if (title == null) CreateMuseumContent(canvas);
            previousCanvas = canvas.rect;
            previousSafe = safe;
            dirty = false;

            float width = max.x - min.x;
            float height = max.y - min.y;
            bool landscape = width > height * 1.12f;
            Rect canvasRect = canvas.rect;
            Rect artRect = landscape
                ? Rect.MinMaxRect(min.x, canvasRect.yMin, min.x + width * .51f, canvasRect.yMax)
                : canvasRect;
            Position(artworkArea, canvasRect, artRect.center.x, artRect.yMin, artRect.width, artRect.height);

            const float copyHeight = 339f;
            float copyCenter = landscape ? max.x - width * .24f : (min.x + max.x) * .5f;
            float copyWidth = Mathf.Min(width * (landscape ? .44f : .87f), 880f);
            float copyBottom = landscape
                ? min.y + Mathf.Max(42f, (height - copyHeight) * .5f)
                : min.y + Mathf.Max(40f, height * .026f);
            copyBottom += Mathf.Min(72f, height * .045f);
            Position((RectTransform)entry.transform, canvasRect, copyCenter, copyBottom, Mathf.Min(420f, copyWidth), 108f);
            Position(instruction.rectTransform, canvasRect, copyCenter, copyBottom + 128f, copyWidth, 105f);
            Position(title.rectTransform, canvasRect, copyCenter, copyBottom + 234f, copyWidth, 105f);
        }

        private void CreateMuseumContent(RectTransform canvas)
        {
            artworkArea = Child("Artwork Area", canvas);
            artworkArea.SetAsFirstSibling();
            artwork.transform.SetParent(artworkArea, false);

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title = Label(canvas, "MuseumTitle", Application.productName, font, 78, Color.white);
            title.fontStyle = FontStyle.Bold;
            instruction = Label(canvas, "MuseumStartInstruction",
                "Scan a planet card to bring it to life.", font, 39,
                new Color(.77f, .88f, .97f));

            var icon = entry.GetComponent<ScanEntryIcon>();
            if (icon != null) icon.enabled = false;
            var face = Child("MuseumStartFace", entry.transform).gameObject.AddComponent<Image>();
            Stretch(face.rectTransform);
            face.sprite = LabSkin.Solid;
            face.type = Image.Type.Sliced;
            face.color = new Color(.38f, .89f, 1f);
            entry.targetGraphic = face;
            var label = Label(face.transform, "MuseumStartLabel", "Start scanning", font, 48,
                new Color(.025f, .075f, .12f));
            label.fontStyle = FontStyle.Bold;
            Stretch(label.rectTransform);
            entry.transform.SetAsLastSibling();
        }

        private static Text Label(Transform parent, string name, string value, Font font, int size, Color color)
        {
            var text = Child(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = size * 3 / 4;
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Child(string name, Transform parent)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            return host.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Position(RectTransform rect, Rect canvas, float center, float bottom, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(center - canvas.xMin, bottom - canvas.yMin);
        }
    }
}
