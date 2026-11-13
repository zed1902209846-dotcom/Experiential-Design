using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    /// 行星实验室：两张行星卡同时出现时，访客先猜、再看演示、最后揭晓。
    /// 四个实验共用同一套回合：跳高（重力）、自转（一天）、公转（一年）、真实大小。
    [DefaultExecutionOrder(-50)]
    public class GravityJumpComparison : MonoBehaviour
    {
        private class Choice
        {
            public PlanetData.Info info;
            public ScanPlanetContent content;
            public float gravity;
        }

        /// 一个可点的答案卡：底板、选中描边、文字，以及自转/公转实验用的进度条。
        private class Card
        {
            public Button button;
            public Image fill;
            public Image ring;
            public Text label;
            public RectTransform track;
            public RectTransform bar;
            public Color accent;
        }

        private readonly List<Choice> choices = new List<Choice>();
        private readonly List<Rect> obstacles = new List<Rect>();
        private readonly List<Rect> exclusions = new List<Rect>();
        public IReadOnlyList<Rect> ScreenExclusions => exclusions;
        private Choice first, second;
        private GravityStickFigure figureA, figureB;
        private Material figureMaterial;
        private RectTransform canvasRect, dock;
        private Text facts, prompt, note, actionLabel;
        private Image actionFill;
        private Button action, cycleA, cycleB;
        private Card cardA, cardSame, cardB;
        private readonly List<Card> tabs = new List<Card>();
        private readonly GravityGuessRound round = new GravityGuessRound();
        private LabExperiment experiment = LabExperiment.Jump;
        private Font font;

        // 累计时钟而非起始时间戳：切到后台或卡顿一帧不会让动画直接跳到结果。
        private float runClock;
        private const float MaxFrameStep = .1f;
        private const float PlaybackRate = .45f;
        // 自转/公转：较快的一方在这段时间里正好转完 WinnerTurns 圈（或跑满进度条）。
        private const float RaceSeconds = 4f;
        private const float WinnerTurns = 2f;
        private const float SizeSeconds = 1.6f;
        // 公转：行星先用这段时间从卡片滑到轨道起点，再开始赛跑。
        private const float GlideSeconds = .7f;
        private OrbitRace orbit;
        private ScanPlanetContent sunCard;

        // 演示期间临时改动的自转速度和模型大小，结束或换实验时还原。
        private SelfRotation spinA, spinB;
        private float spinARestore, spinBRestore;
        private Transform scaled, enlarged;
        private Vector3 scaledRestore, enlargedRestore;
        private float scaledRatio = 1f;
        // 较大的一颗同时放大一些，小的那颗按真实比例缩小后才不至于小得看不见。
        private const float LargerGrowth = 1.6f;

        private static readonly Color Cyan = new Color(.3f, .9f, 1f);
        private static readonly Color Gold = new Color(1f, .8f, .25f);
        private static readonly Color Green = new Color(.45f, 1f, .62f);
        private static readonly Color Amber = new Color(1f, .68f, .3f);
        private static readonly Color Ink = new Color(.03f, .07f, .11f);
        private static readonly Color PanelFill = new Color(.035f, .065f, .105f, .95f);
        private static readonly Color CardFill = new Color(.09f, .17f, .24f);
        private static readonly Color CardSelected = new Color(.12f, .29f, .37f);
        private static readonly Color Muted = new Color(.13f, .2f, .27f);
        private static readonly Color SoftText = new Color(.72f, .83f, .9f);

        public bool IsJumping => round.Phase == GravityGuessPhase.Jumping;
        public GravityGuessPhase Phase => round.Phase;
        public GravityGuess Prediction => round.Prediction;
        public bool GuessedCorrectly => round.IsCorrect;
        public LabExperiment Experiment => experiment;
        public string FirstPlanetId => first != null ? first.info.id : null;
        public string SecondPlanetId => second != null ? second.info.id : null;

        private void Awake()
        {
            figureMaterial = Resources.Load<Material>("UI/GravityStickFigure");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        public void Sync(IReadOnlyDictionary<string, ScanPlanetContent> tracked)
        {
            string a = FirstPlanetId, b = SecondPlanetId;
            tracked.TryGetValue("sun", out sunCard);
            choices.Clear();
            foreach (var info in PlanetData.All)
            {
                if (!PlanetGravity.TryGet(info.id, out float gravity) || !tracked.TryGetValue(info.id, out var content)
                    || content == null || content.Model == null || !content.Model.activeInHierarchy) continue;
                choices.Add(new Choice { info = info, content = content, gravity = gravity });
            }
            first = choices.Find(c => c.info.id == a);
            if (first == null) first = choices.Find(c => c.info.id == b);
            if (first == null && choices.Count > 0) first = choices[0];
            second = choices.Find(c => c.info.id == b && c != first);
            if (second == null) second = choices.Find(c => c != first);
            // Repeated tracking notifications do not restart an ongoing experiment.
            if (a != FirstPlanetId || b != SecondPlanetId || figureA == null || figureB == null) RebuildPair();
            dock.gameObject.SetActive(first != null && second != null && figureMaterial != null);
            if (second == null) ClearFigures();
            UpdateCopy();
        }

        private void RebuildPair()
        {
            ClearFigures();
            if (first == null || second == null || figureMaterial == null) return;
            figureA = new GravityStickFigure(first.content, transform, Cyan, figureMaterial);
            figureB = new GravityStickFigure(second.content, transform, Gold, figureMaterial);
        }

        private void Cycle(bool slotA)
        {
            if (IsJumping || choices.Count < 3) return;
            var current = slotA ? first : second;
            var other = slotA ? second : first;
            int index = choices.IndexOf(current);
            for (int i = 1; i <= choices.Count; i++)
            {
                var next = choices[(index + i) % choices.Count];
                if (next == other) continue;
                if (slotA) first = next; else second = next;
                RebuildPair();
                UpdateCopy();
                return;
            }
        }

        /// 切换实验：演示进行中不能切，切换后重新开始一轮预测。
        public void SetExperiment(LabExperiment next)
        {
            if (IsJumping || next == experiment) return;
            RestoreEffects();
            round.Reset();
            experiment = next;
            UpdateCopy();
        }

        public void Guess(GravityGuess guess)
        {
            if (!PairReady()) return;
            if (round.Choose(guess)) UpdateCopy();
        }

        public void GuessAgain()
        {
            if (round.Phase != GravityGuessPhase.Result) return;
            RestoreEffects();
            round.Reset();
            UpdateCopy();
        }

        private void PrimaryAction()
        {
            if (round.Phase == GravityGuessPhase.Result) GuessAgain();
            else Run();
        }

        /// 开始演示当前实验（需要先选好答案）。
        public void Run()
        {
            if (!PairReady() || !round.Begin()) return;
            runClock = 0;
            StartEffects();
            UpdateCopy();
        }

        private bool PairReady() => first != null && second != null && figureA != null && figureB != null
            && figureA.Ready && figureB.Ready;

        private float Value(Choice choice)
        {
            PlanetLab.TryGet(experiment, choice.info.id, out float value);
            return value;
        }

        private void StartEffects()
        {
            float a = Value(first), b = Value(second);
            if (experiment == LabExperiment.Day)
            {
                float fastest = Mathf.Min(a, b);
                float winnerSpeed = WinnerTurns * 360f / RaceSeconds;
                spinA = TakeSpin(first, winnerSpeed * fastest / a, out spinARestore);
                spinB = TakeSpin(second, winnerSpeed * fastest / b, out spinBRestore);
            }
            else if (experiment == LabExperiment.Year && !Mathf.Approximately(a, b))
            {
                if (orbit == null) orbit = new OrbitRace(transform, figureMaterial);
                bool firstInner = a < b;
                orbit.Begin(firstInner ? first.content : second.content, firstInner ? Cyan : Gold,
                    firstInner ? second.content : first.content, firstInner ? Gold : Cyan, sunCard);
            }
            else if (experiment == LabExperiment.Size && !Mathf.Approximately(a, b))
            {
                var smaller = a < b ? first : second;
                var larger = smaller == first ? second : first;
                scaled = smaller.content.Model.transform;
                scaledRestore = scaled.localScale;
                enlarged = larger.content.Model.transform;
                enlargedRestore = enlarged.localScale;
                // 访客可能已经用双指把其中一颗捏大或捏小：按当前实际大小换算，结果才是真实比例。
                float current = smaller.content.BodyWorldBounds.extents.x;
                float match = current > 0f ? larger.content.BodyWorldBounds.extents.x / current : 1f;
                scaledRatio = match * Mathf.Min(a, b) / Mathf.Max(a, b);
            }
        }

        private static SelfRotation TakeSpin(Choice choice, float degreesPerSecond, out float restore)
        {
            var spin = choice.content.Model.GetComponent<SelfRotation>();
            restore = spin != null ? spin.degreesPerSecond : 0f;
            if (spin != null)
                spin.degreesPerSecond = PlanetLab.SpinsBackwards(choice.info.id) ? -degreesPerSecond : degreesPerSecond;
            return spin;
        }

        private void RestoreSpin()
        {
            if (spinA != null) spinA.degreesPerSecond = spinARestore;
            if (spinB != null) spinB.degreesPerSecond = spinBRestore;
            spinA = spinB = null;
        }

        private void RestoreEffects()
        {
            RestoreSpin();
            if (scaled != null) scaled.localScale = scaledRestore;
            if (enlarged != null) enlarged.localScale = enlargedRestore;
            scaled = enlarged = null;
            if (orbit != null) orbit.End();
        }

        private void Complete()
        {
            round.Finish(PlanetLab.Answer(experiment, Value(first), Value(second)));
            RestoreSpin(); // 揭晓后恢复平常的慢速自转；缩小后的真实比例保留到下一轮。
            UpdateCopy();
        }

        private void LateUpdate()
        {
            exclusions.Clear();
            if (first == null || second == null || figureA == null || figureB == null || canvasRect == null) return;
            var camera = Camera.main;
            if (camera == null) return;
            if (!figureA.Ready || !figureB.Ready) { dock.gameObject.SetActive(false); return; }
            if (IsJumping) runClock += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);

            float progressA = 0, progressB = 0;
            if (experiment == LabExperiment.Jump) AnimateJump(camera);
            else
            {
                figureA.Hide();
                figureB.Hide();
                if (experiment == LabExperiment.Size) AnimateSize();
                else AnimateRace(out progressA, out progressB);
            }

            obstacles.Clear();
            foreach (var choice in choices)
                if (choice.content.TryGetScreenBounds(camera, canvasRect, out Rect rect)) obstacles.Add(rect);
            if (orbit != null && orbit.Active && Project(orbit.WorldBounds, camera, out Rect orbitRect))
            {
                obstacles.Add(orbitRect);
                exclusions.Add(orbitRect);
            }
            if (experiment == LabExperiment.Jump)
            {
                float size = FigureSize();
                float worldPerMeter = WorldPerMeter(size);
                ProtectJump(figureA, first.gravity, size, worldPerMeter, camera);
                ProtectJump(figureB, second.gravity, size, worldPerMeter, camera);
            }

            Rect safe = SafeRect();
            bool full = PlanetPanelLayout.PlaceOverlay(safe, obstacles, new Vector2(Mathf.Min(660, safe.width), 330),
                new Vector2(Mathf.Min(360, safe.width), 322), out Rect placement);
            if (!full && !PlanetPanelLayout.PlaceOverlay(safe, obstacles, new Vector2(Mathf.Min(420, safe.width), 190),
                new Vector2(Mathf.Min(280, safe.width), 190), out placement))
            {
                dock.gameObject.SetActive(false);
                return;
            }
            dock.gameObject.SetActive(true);
            dock.sizeDelta = placement.size;
            dock.anchoredPosition = new Vector2(placement.xMin, placement.yMax);
            Layout(full);
            bool racing = (experiment == LabExperiment.Day || experiment == LabExperiment.Year)
                && (IsJumping || Phase == GravityGuessPhase.Result);
            SetBar(cardA, racing, progressA);
            SetBar(cardB, racing, progressB);
            RefreshControls();
            exclusions.Add(placement);
        }

        private float FigureSize()
        {
            float radiusA = figureA.SurfaceBounds.extents.magnitude / Mathf.Sqrt(3);
            float radiusB = figureB.SurfaceBounds.extents.magnitude / Mathf.Sqrt(3);
            return Mathf.Min(radiusA, radiusB);
        }

        private float WorldPerMeter(float size) =>
            size * .85f / Mathf.Max(PlanetGravity.Peak(first.gravity), PlanetGravity.Peak(second.gravity));

        private void AnimateJump(Camera camera)
        {
            float size = FigureSize();
            float worldPerMeter = WorldPerMeter(size);
            float elapsed = IsJumping ? runClock * PlaybackRate : 0;
            float end = Mathf.Max(PlanetGravity.FlightTime(first.gravity), PlanetGravity.FlightTime(second.gravity));
            if (IsJumping && elapsed >= end)
            {
                Complete();
                elapsed = 0;
            }
            figureA.Pose(camera, size * .65f, PlanetGravity.Height(first.gravity, elapsed) * worldPerMeter,
                IsJumping && elapsed < PlanetGravity.FlightTime(first.gravity));
            figureB.Pose(camera, size * .65f, PlanetGravity.Height(second.gravity, elapsed) * worldPerMeter,
                IsJumping && elapsed < PlanetGravity.FlightTime(second.gravity));
        }

        /// 自转与公转：较快的一方跑满进度条，另一方按真实比例落后。
        private void AnimateRace(out float progressA, out float progressB)
        {
            float lead = experiment == LabExperiment.Year ? GlideSeconds : 0f;
            if (IsJumping && runClock >= lead + RaceSeconds) Complete();
            bool result = Phase == GravityGuessPhase.Result;
            float time = result ? 1f : IsJumping ? Mathf.Clamp01((runClock - lead) / RaceSeconds) : 0f;
            float a = Value(first), b = Value(second), fastest = Mathf.Min(a, b);
            progressA = time * fastest / a;
            progressB = time * fastest / b;
            if (orbit == null || !orbit.Active) return;
            // 揭晓后行星停在轨道上，直到访客点 AGAIN；期间仍然跟着卡片移动。
            float glide = result ? 1f : Mathf.Clamp01(runClock / GlideSeconds);
            orbit.Update(glide, Mathf.Max(progressA, progressB), Mathf.Min(progressA, progressB));
        }

        /// 真实大小：较大的一颗略微放大，较小的一颗缩到与它成真实的直径比例。
        private void AnimateSize()
        {
            if (IsJumping && runClock >= SizeSeconds + .4f) Complete();
            if (scaled == null || enlarged == null) return;
            float time = Phase == GravityGuessPhase.Result ? 1f : Mathf.Clamp01(runClock / SizeSeconds);
            float eased = Mathf.SmoothStep(0f, 1f, time);
            enlarged.localScale = enlargedRestore * Mathf.Lerp(1f, LargerGrowth, eased);
            scaled.localScale = scaledRestore * Mathf.Lerp(1f, LargerGrowth * scaledRatio, eased);
        }

        private void ProtectJump(GravityStickFigure figure, float gravity, float size, float scale, Camera camera)
        {
            var surface = figure.SurfaceBounds;
            float radius = Mathf.Max(surface.extents.x, Mathf.Max(surface.extents.y, surface.extents.z));
            var foot = surface.center + camera.transform.up * radius;
            var top = foot + camera.transform.up * (PlanetGravity.Peak(gravity) * scale + size * .7f);
            var bounds = new Bounds(foot, Vector3.one * size * .35f);
            bounds.Encapsulate(top);
            if (Project(bounds, camera, out Rect rect)) { obstacles.Add(rect); exclusions.Add(rect); }
        }

        private bool Project(Bounds bounds, Camera camera, out Rect rect)
        {
            rect = Rect.zero;
            if (camera.WorldToScreenPoint(bounds.center).z <= 0) return false;
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var point = camera.WorldToScreenPoint(bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                if (point.z <= 0) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, point, null, out Vector2 local);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return rect.Overlaps(canvasRect.rect);
        }

        private Rect SafeRect()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Screen.safeArea.min, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Screen.safeArea.max, null, out Vector2 max);
            return Rect.MinMaxRect(min.x + 16, min.y + Mathf.Min(200, (max.y - min.y) * .18f), max.x - 16, max.y - 110);
        }

        private void UpdateCopy()
        {
            if (first == null || second == null) return;
            bool result = Phase == GravityGuessPhase.Result;
            float a = Value(first), b = Value(second);
            cardA.label.text = first.info.display + (result ? "\n" + PlanetLab.Format(experiment, a) : "");
            cardB.label.text = second.info.display + (result ? "\n" + PlanetLab.Format(experiment, b) : "");
            prompt.text = result ? (round.IsCorrect ? "CORRECT!" : "NICE TRY!")
                : IsJumping ? "WATCH!" : PlanetLab.Question(experiment);
            prompt.color = !result ? Cyan : round.IsCorrect ? Green : Amber;
            facts.text = result ? PlanetLab.Fact(experiment, first.info.display, a, second.info.display, b) : "";
            note.text = PlanetLab.Note(experiment, first.info.id, second.info.id);
            Highlight(cardA, GravityGuess.First);
            Highlight(cardSame, GravityGuess.Same);
            Highlight(cardB, GravityGuess.Second);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool active = PlanetLab.All[i] == experiment;
                tabs[i].fill.color = active ? Cyan : Muted;
                tabs[i].label.color = active ? Ink : SoftText;
            }
            RefreshControls();
        }

        /// 选中的答案描上自己的颜色；揭晓后正确答案描绿，猜错的那张描橙。
        private void Highlight(Card card, GravityGuess guess)
        {
            bool selected = Prediction == guess;
            bool result = Phase == GravityGuessPhase.Result;
            bool correct = result && round.Answer == guess;
            card.fill.color = selected || correct ? CardSelected : CardFill;
            card.ring.enabled = selected || correct;
            card.ring.color = correct ? Green : result ? Amber : card.accent;
        }

        private void RefreshControls()
        {
            bool ready = Phase == GravityGuessPhase.Ready;
            bool result = Phase == GravityGuessPhase.Result;
            action.interactable = ready || result;
            cardA.button.interactable = cardB.button.interactable = cardSame.button.interactable =
                Phase == GravityGuessPhase.Guessing || ready;
            cycleA.gameObject.SetActive(choices.Count > 2);
            cycleB.gameObject.SetActive(choices.Count > 2);
            cycleA.interactable = cycleB.interactable = !IsJumping;
            foreach (var tab in tabs) tab.button.interactable = !IsJumping;
            actionLabel.text = result ? "AGAIN" : IsJumping ? "..." : ready ? PlanetLab.Action(experiment) : "PICK ONE";
            // 主按钮只有在能按的时候才是实心亮色，引导下一步该点哪里。
            actionFill.color = ready ? Cyan : result ? Gold : Muted;
            actionLabel.color = ready || result ? Ink : SoftText;
        }

        private static void SetBar(Card card, bool visible, float progress)
        {
            card.track.gameObject.SetActive(visible);
            // 进度条出现时把文字往上让出位置。
            card.label.rectTransform.offsetMin = new Vector2(6, visible ? 16 : 6);
            if (visible) card.bar.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void Layout(bool full)
        {
            float side = 14f;
            float tabTop = full ? 12 : 8, tabHeight = full ? 36 : 28;
            for (int i = 0; i < tabs.Count; i++)
            {
                var tab = (RectTransform)tabs[i].button.transform;
                Region(tab, 0, tabTop, 0, tabHeight);
                tab.anchorMin = new Vector2(i / (float)tabs.Count, 1);
                tab.anchorMax = new Vector2((i + 1) / (float)tabs.Count, 1);
                tab.offsetMin = new Vector2(i == 0 ? side : 3, tab.offsetMin.y);
                tab.offsetMax = new Vector2(i == tabs.Count - 1 ? -side : -3, tab.offsetMax.y);
            }

            float promptTop = full ? 56 : 40, promptHeight = full ? 34 : 26;
            float inset = choices.Count > 2 ? 56 : side;
            Region(prompt.rectTransform, inset, promptTop, inset, promptHeight);
            var previous = (RectTransform)cycleA.transform;
            Region(previous, side, promptTop - 2, 0, promptHeight + 4);
            previous.anchorMax = new Vector2(0, 1);
            previous.offsetMax = new Vector2(side + 38, previous.offsetMax.y);
            var next = (RectTransform)cycleB.transform;
            Region(next, 0, promptTop - 2, side, promptHeight + 4);
            next.anchorMin = new Vector2(1, 1);
            next.offsetMin = new Vector2(-side - 38, next.offsetMin.y);

            float answerTop = full ? 98 : 70, answerHeight = full ? 82 : 54;
            var a = (RectTransform)cardA.button.transform;
            Region(a, side, answerTop, 4, answerHeight);
            a.anchorMax = new Vector2(.4f, 1);
            var tie = (RectTransform)cardSame.button.transform;
            Region(tie, 4, answerTop, 4, answerHeight);
            tie.anchorMin = new Vector2(.4f, 1);
            tie.anchorMax = new Vector2(.6f, 1);
            var b = (RectTransform)cardB.button.transform;
            Region(b, 4, answerTop, side, answerHeight);
            b.anchorMin = new Vector2(.6f, 1);

            facts.gameObject.SetActive(full && Phase == GravityGuessPhase.Result);
            Region(facts.rectTransform, side, 186, side, 32);
            note.gameObject.SetActive(full);
            var actionRect = (RectTransform)action.transform;
            actionRect.anchorMin = new Vector2(0, 0);
            actionRect.anchorMax = new Vector2(1, 0);
            actionRect.offsetMin = new Vector2(side, full ? 38 : 8);
            actionRect.offsetMax = new Vector2(-side, full ? 98 : 58);
        }

        private RectTransform NewRect(string name, Transform parent)
        {
            var host = new GameObject(name, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            return host.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, float margin = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }

        private Image Rounded(RectTransform rect, Sprite sprite, Color color, float cornerScale, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = cornerScale; // 越大圆角越小
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private Text Label(string name, Transform parent, string value, int size, Color color)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.raycastTarget = false;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(14, size - 8);
            text.resizeTextMaxSize = size;
            return text;
        }

        private static void Region(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private Card NewCard(string name, Transform parent, string value, int size, Color accent, float cornerScale,
            UnityEngine.Events.UnityAction onClick)
        {
            var rect = NewRect(name, parent);
            var card = new Card { accent = accent };
            card.fill = Rounded(rect, LabSkin.Solid, CardFill, cornerScale, true);
            card.button = rect.gameObject.AddComponent<Button>();
            card.button.targetGraphic = card.fill;
            // 可用/不可用的外观由面板自己控制，按钮只保留按下时的反馈。
            var colors = card.button.colors;
            colors.disabledColor = Color.white;
            colors.pressedColor = new Color(.78f, .78f, .78f);
            card.button.colors = colors;
            card.button.onClick.AddListener(onClick);

            var ringRect = NewRect("Ring", rect);
            Stretch(ringRect);
            card.ring = Rounded(ringRect, LabSkin.Ring, accent, cornerScale, false);
            card.ring.enabled = false;

            card.label = Label("Label", rect, value, size, accent);
            Stretch(card.label.rectTransform, 6);

            card.track = NewRect("Track", rect);
            card.track.anchorMin = new Vector2(0, 0);
            card.track.anchorMax = new Vector2(1, 0);
            card.track.offsetMin = new Vector2(12, 7);
            card.track.offsetMax = new Vector2(-12, 13);
            Rounded(card.track, LabSkin.Solid, new Color(1, 1, 1, .12f), 4f, false);
            card.bar = NewRect("Bar", card.track);
            card.bar.anchorMin = Vector2.zero;
            card.bar.anchorMax = new Vector2(0, 1);
            card.bar.offsetMin = card.bar.offsetMax = Vector2.zero;
            Rounded(card.bar, LabSkin.Solid, accent, 4f, false);
            card.track.gameObject.SetActive(false);
            return card;
        }

        private void BuildUI()
        {
            // ScanController may live on another Canvas: use an independent root so coordinates
            // match PlanetAnnotations (720-wide screen space), not a 100x100 nested canvas.
            canvasRect = NewRect("GravityLabCanvas", null);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            scaler.matchWidthOrHeight = 0;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();

            dock = NewRect("GravityLab", canvasRect);
            dock.pivot = new Vector2(0, 1);
            Rounded(dock, LabSkin.Solid, PanelFill, 1f, true);
            var edge = NewRect("Edge", dock);
            Stretch(edge);
            Rounded(edge, LabSkin.Ring, new Color(Cyan.r, Cyan.g, Cyan.b, .45f), 1f, false);

            foreach (var item in PlanetLab.All)
            {
                var target = item;
                tabs.Add(NewCard("Tab" + PlanetLab.Tab(item), dock, PlanetLab.Tab(item), 20, SoftText, 1.25f,
                    () => SetExperiment(target)));
            }

            prompt = Label("Question", dock, PlanetLab.Question(experiment), 26, Cyan);
            cardA = NewCard("GuessFirst", dock, "", 24, Cyan, 1.5f, () => Guess(GravityGuess.First));
            cardSame = NewCard("GuessSame", dock, "=", 30, Color.white, 1.5f, () => Guess(GravityGuess.Same));
            cardB = NewCard("GuessSecond", dock, "", 24, Gold, 1.5f, () => Guess(GravityGuess.Second));
            cycleA = NewCard("CycleFirst", dock, "<", 24, Cyan, 1.6f, () => Cycle(true)).button;
            cycleB = NewCard("CycleSecond", dock, ">", 24, Gold, 1.6f, () => Cycle(false)).button;

            facts = Label("Result", dock, "", 24, Color.white);
            facts.gameObject.SetActive(false);
            note = Label("ScienceNote", dock, "", 16, SoftText);
            note.fontStyle = FontStyle.Normal;
            note.rectTransform.anchorMin = Vector2.zero;
            note.rectTransform.anchorMax = new Vector2(1, 0);
            note.rectTransform.offsetMin = new Vector2(14, 8);
            note.rectTransform.offsetMax = new Vector2(-14, 32);

            var primary = NewCard("RunExperiment", dock, "PICK ONE", 26, Color.white, 1.1f, PrimaryAction);
            action = primary.button;
            actionFill = primary.fill;
            actionLabel = primary.label;

            Layout(true);
            RefreshControls();
            dock.gameObject.SetActive(false);
        }

        private void ClearFigures()
        {
            RestoreEffects();
            if (figureA != null) figureA.Dispose();
            if (figureB != null) figureB.Dispose();
            figureA = figureB = null;
            round.Reset();
            exclusions.Clear();
        }

        private void OnEnable() { if (first != null && second != null) RebuildPair(); }
        private void OnDisable() { ClearFigures(); if (dock != null) dock.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            ClearFigures();
            if (orbit != null) orbit.Dispose();
            if (canvasRect != null) Destroy(canvasRect.gameObject);
        }
    }
}
