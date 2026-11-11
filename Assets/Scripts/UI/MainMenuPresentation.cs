using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// Keep the complete illustration and place a text-free entry in its lower quiet area.
    [ExecuteAlways]
    public class MainMenuPresentation : MonoBehaviour
    {
        [SerializeField] private Image artwork;
        [SerializeField] private Button entry;
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
            if (artwork == null || entry == null || artwork.canvas == null) return;
            var canvas = artwork.canvas.rootCanvas.transform as RectTransform;
            if (canvas == null) return;
            Rect safe = Screen.safeArea;
            if (!dirty && canvas.rect == previousCanvas && safe == previousSafe) return;
            previousCanvas = canvas.rect; previousSafe = safe; dirty = false;
            Vector2 bottom;
            var camera = artwork.canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : artwork.canvas.rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, safe.min, camera, out bottom);
            var rect = entry.GetComponent<RectTransform>();
            float diameter = Mathf.Clamp(canvas.rect.width * .16f, 144, 180);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.anchoredPosition = new Vector2(0, Mathf.Max(0,bottom.y-canvas.rect.yMin)+Mathf.Max(28,canvas.rect.height*.07f));
        }
    }
}
