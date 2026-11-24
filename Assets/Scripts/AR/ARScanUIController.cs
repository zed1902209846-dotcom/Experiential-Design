using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 扫描界面状态：识别前显示扫描框；识别后提示点击行星或捏合缩放。
    public class ARScanUIController : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject scanFrame;
        [SerializeField] private Text hintLabel;
        [SerializeField] private string foundHint = "Tap: details | Drag: rotate\nHold 0.5s + drag: move | Pinch on planet: resize";
        [SerializeField] private string sunHint = "The Sun card is the light!\nMove a planet around it to make day and night";
        [SerializeField] private string lostHint = "Point the camera at your planet cards";
        [SerializeField] private string lostStatus = "Scanning for planet cards...";
        public RectTransform HintRect => hintLabel != null && hintLabel.gameObject.activeInHierarchy
            ? hintLabel.rectTransform : null;

        private bool sunlight;
        private const float FeedbackDuration = 3f;
        private float feedbackUntil;

        private const float ScanAdviceDelay = 5f;
        private const float GlareAdviceDelay = 11f;
        private float firstScanStarted = -1f;
        private float secondCardHintAt = -1f;
        private bool inspected;
        private string afterFeedback = "";
        private string secondCardHint;
        private enum GestureStep { Resize, Move, Finished }
        private GestureStep gestureStep;
        private bool gestureGuideVisible;
        private string lastCardName;
        private Button skipGuide;
        private GestureTutorialPreview gesturePreview;

        public void BeginMuseumGuide()
        {
            inspected = false;
            gestureStep = GestureStep.Resize;
            SetGestureGuideVisible(false);
            secondCardHintAt = -1f;
            firstScanStarted = Time.unscaledTime;
            OnTrackingLost();
            if (statusText == null) return;

            statusText.raycastTarget = false;
            statusText.color = Color.white;
            statusText.fontSize = 38;
            statusText.rectTransform.sizeDelta = new Vector2(920f, 120f);
            var position = statusText.rectTransform.anchoredPosition;
            statusText.rectTransform.anchoredPosition = new Vector2(position.x, 687f);
            var outline = statusText.GetComponent<Outline>();
            if (outline == null) outline = statusText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.06f, 0.11f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            if (skipGuide == null) BuildGestureGuideUI();

            ShowFirstScanAdvice(0f);
        }

        public void OnMuseumCardFound(string name, string eligiblePlanetId, bool pairReady)
        {
            firstScanStarted = -1f;
            secondCardHintAt = -1f;
            OnTrackingFound();
            if (pairReady)
            {
                gestureStep = GestureStep.Finished;
                SetGestureGuideVisible(false);
                return;
            }

            lastCardName = name;
            secondCardHint = SecondCardAdvice(eligiblePlanetId);
            if (gestureStep != GestureStep.Finished)
            {
                ShowGestureGuide(name);
                return;
            }
            ShowExploreGuide(name);
        }

        private void ShowExploreGuide(string detectedName = null)
        {
            if (!inspected) secondCardHintAt = Time.unscaledTime + 11f;
            string next = inspected ? secondCardHint : $"Tap {lastCardName} to see what makes it special.";
            if (detectedName == null) ShowStatus(next);
            else ShowStatus($"<b>{detectedName} detected</b>\n{next}", FeedbackDuration, next);
        }

        public void OnMuseumGesture(PlanetGesture gesture)
        {
            if (!gestureGuideVisible) return;
            if (gestureStep == GestureStep.Resize && gesture == PlanetGesture.Resize)
            {
                gestureStep = GestureStep.Move;
                ShowGestureGuide();
            }
            else if (gestureStep == GestureStep.Move && gesture == PlanetGesture.Move)
                FinishGestureGuide();
        }

        private void ShowGestureGuide(string detectedName = null)
        {
            SetGestureGuideVisible(true);
            bool resize = gestureStep == GestureStep.Resize;
            if (gesturePreview != null) gesturePreview.Play(resize ? PlanetGesture.Resize : PlanetGesture.Move);
            string title = resize ? "Pinch to resize" : "Move with one finger";
            string instruction = resize ? "Pinch in or out with two fingers over the planet."
                : "Lift both fingers. Press and hold, then drag.";
            string guide = $"<b>{title}</b>\n{instruction}";
            if (detectedName == null) ShowStatus(guide);
            else ShowStatus($"<b>{detectedName} detected</b>\n{instruction}", 2f, guide);
        }

        private void FinishGestureGuide()
        {
            gestureStep = GestureStep.Finished;
            SetGestureGuideVisible(false);
            ShowExploreGuide();
        }

        private void SetGestureGuideVisible(bool visible)
        {
            gestureGuideVisible = visible;
            if (skipGuide != null) skipGuide.gameObject.SetActive(visible);
            if (gesturePreview != null) gesturePreview.transform.parent.gameObject.SetActive(visible);
        }

        private void BuildGestureGuideUI()
        {
            var previewFrame = new GameObject("GestureDemonstration", typeof(RectTransform), typeof(Canvas), typeof(Image));
            var previewRect = (RectTransform)previewFrame.transform;
            PlaceGuideControl(previewRect, new Vector2(-155f, -12f), new Vector2(278f, 136f));
            var background = previewFrame.GetComponent<Image>();
            background.sprite = LabSkin.Solid;
            background.type = Image.Type.Sliced;
            background.color = new Color(.03f, .075f, .12f, .92f);
            background.raycastTarget = false;
            gesturePreview = new GameObject("Preview", typeof(RectTransform), typeof(GestureTutorialPreview))
                .GetComponent<GestureTutorialPreview>();
            var art = gesturePreview.rectTransform;
            art.SetParent(previewRect, false);
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.offsetMin = art.offsetMax = Vector2.zero;
            gesturePreview.raycastTarget = false;

            var button = new GameObject("SkipGestureGuide", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)button.transform;
            PlaceGuideControl(rect, new Vector2(155f, -40f), new Vector2(290f, 80f));
            var image = button.GetComponent<Image>();
            image.sprite = LabSkin.Solid;
            image.type = Image.Type.Sliced;
            image.color = new Color(.03f, .075f, .12f, .92f);
            skipGuide = button.GetComponent<Button>();
            skipGuide.targetGraphic = image;
            skipGuide.onClick.AddListener(FinishGestureGuide);

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(rect, false);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            label.font = statusText.font;
            label.fontSize = 34;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            label.text = "Skip tutorial";
            SetGestureGuideVisible(false);
        }

        private void PlaceGuideControl(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.SetParent(statusText.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void OnMuseumProfileOpened(string eligiblePlanetId)
        {
            inspected = true;
            secondCardHintAt = -1f;
            secondCardHint = SecondCardAdvice(eligiblePlanetId);
            if (!gestureGuideVisible) ShowStatus(secondCardHint);
        }

        public void OnMuseumCardLost(string name, bool anyCardsVisible, bool hadPair, bool pairReady, bool interrupted)
        {
            if (pairReady)
            {
                if (interrupted) ShowStatus($"The {name} card left the view. Make a new prediction.", FeedbackDuration);
                return;
            }
            if (gestureGuideVisible && anyCardsVisible) return;
            SetGestureGuideVisible(false);
            secondCardHintAt = -1f;
            if (!anyCardsVisible) OnTrackingLost();
            if (interrupted)
                ShowStatus($"<b>Bring the {name} card back into view</b>\nOnce both cards are visible, try again.");
            else if (hadPair || anyCardsVisible)
                ShowStatus($"<b>Bring the {name} card back into view</b>\nKeep both cards in view.");
            else
                ShowStatus($"<b>Bring the {name} card back into the frame</b>\nKeep the whole card in view.");
        }

        private static string SecondCardAdvice(string eligiblePlanetId)
        {
            switch (eligiblePlanetId)
            {
                case "earth": return "Place the Mars card beside the Earth card.\nKeep both cards in view.";
                case "mars": return "Place the Earth card beside the Mars card.\nKeep both cards in view.";
                default: return "Add a different planet card.\nKeep both cards in view.";
            }
        }

        private void ShowFirstScanAdvice(float elapsed)
        {
            string advice = elapsed >= GlareAdviceDelay ? "Seeing glare? Tilt the tablet slightly."
                : elapsed >= ScanAdviceDelay ? "Raise the tablet so the whole card fits in the frame."
                : "Fit the whole card inside the frame.";
            string copy = "<b>Scan one planet card</b>\n" + advice;
            if (statusText != null && statusText.text != copy) ShowStatus(copy);
        }

        public void ShowScanGuide()
        {
            OnTrackingLost();
            if (statusText == null) return;

            statusText.raycastTarget = false;
            statusText.color = Color.white;
            statusText.rectTransform.sizeDelta = new Vector2(920f, 120f);
            var outline = statusText.GetComponent<Outline>();
            if (outline == null) outline = statusText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.06f, 0.11f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            ShowStatus("<b>Place a card inside the frame</b>\nKeep the whole card visible and avoid glare.");
        }

        public void OnCardFound(string cardName, bool isFirstCard)
        {
            OnTrackingFound();
            string nextStep = isFirstCard
                ? "\nTap the model or scan another card."
                : "";
            ShowStatus($"<b>{cardName} detected</b>{nextStep}", FeedbackDuration);
        }

        public void OnCardLost(string cardName, bool hasOtherCards)
        {
            if (hasOtherCards)
            {
                ShowStatus($"<b>{cardName} left view</b>\nBring it back into view.", FeedbackDuration);
                return;
            }

            OnTrackingLost();
            ShowStatus($"<b>{cardName} left view</b>\nBring the card back into the frame.");
        }

        private void ShowStatus(string message, float duration = 0f, string next = "")
        {
            afterFeedback = next;
            feedbackUntil = duration > 0f ? Time.unscaledTime + duration : 0f;
            if (statusText != null) statusText.text = message;
        }

        private void Update()
        {
            if (firstScanStarted >= 0f) ShowFirstScanAdvice(Time.unscaledTime - firstScanStarted);
            if (feedbackUntil > 0f && Time.unscaledTime >= feedbackUntil) ShowStatus(afterFeedback);
            if (secondCardHintAt >= 0f && Time.unscaledTime >= secondCardHintAt)
            {
                secondCardHintAt = -1f;
                ShowStatus(secondCardHint);
            }
        }

        public void OnTrackingFound()
        {
            if (scanFrame != null) scanFrame.SetActive(false);
            if (hintLabel != null) hintLabel.text = sunlight ? sunHint : foundHint;
            ShowStatus("");
        }

        /// 太阳卡光源生效时，把底部提示换成玩法说明；失效后恢复常规手势提示。
        public void SetSunlight(bool active)
        {
            if (sunlight == active) return;
            sunlight = active;
            bool scanning = scanFrame != null && scanFrame.activeSelf;
            if (hintLabel != null && !scanning) hintLabel.text = active ? sunHint : foundHint;
        }

        public void OnTrackingLost()
        {
            sunlight = false;
            if (scanFrame != null) scanFrame.SetActive(true);
            if (hintLabel != null) hintLabel.text = lostHint;
            ShowStatus(lostStatus);
        }

        public void GoBack()
        {
            SceneNavigator.GoTo(SceneNavigator.MainMenu);
        }
    }
}
