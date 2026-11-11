using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SolarSystemAR
{
    [DefaultExecutionOrder(-50)]
    public class GravityJumpComparison : MonoBehaviour
    {
        private class Choice
        {
            public PlanetData.Info info;
            public ScanPlanetContent content;
            public float gravity;
        }
        private readonly List<Choice> choices = new List<Choice>();
        private readonly List<Rect> obstacles = new List<Rect>();
        private readonly List<Rect> exclusions = new List<Rect>();
        public IReadOnlyList<Rect> ScreenExclusions => exclusions;
        private Choice first, second;
        private GravityStickFigure figureA, figureB;
        private Material figureMaterial;
        private RectTransform canvasRect, dock, details;
        private Text facts, prompt, note;
        private Text jumpLabel;
        private Button jump, selectA, selectB, same, cycleA, cycleB;
        private readonly GravityGuessRound round = new GravityGuessRound();
        private Font font;
        private float jumpStart;
        private const float PlaybackRate = .45f;
        private readonly Color cyan = new Color(.3f,.9f,1f);
        private readonly Color gold = new Color(1f,.8f,.25f);
        public bool IsJumping => round.Phase == GravityGuessPhase.Jumping;
        public GravityGuessPhase Phase => round.Phase;
        public GravityGuess Prediction => round.Prediction;
        public bool GuessedCorrectly => round.IsCorrect;
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
            choices.Clear();
            foreach (var info in PlanetData.All)
            {
                ScanPlanetContent content; float gravity;
                if (!PlanetGravity.TryGet(info.id, out gravity) || !tracked.TryGetValue(info.id,out content)
                    || content == null || content.Model == null || !content.Model.activeInHierarchy) continue;
                choices.Add(new Choice {info=info,content=content,gravity=gravity});
            }
            first = choices.Find(c=>c.info.id==a);
            if (first == null) first = choices.Find(c=>c.info.id==b);
            if (first == null && choices.Count>0) first=choices[0];
            second = choices.Find(c=>c.info.id==b && c!=first);
            if (second == null) second = choices.Find(c=>c!=first);
            // Repeated tracking notifications do not restart an ongoing experiment.
            if (a!=FirstPlanetId || b!=SecondPlanetId || figureA==null || figureB==null) RebuildPair();
            dock.gameObject.SetActive(first!=null && second!=null && figureMaterial!=null);
            if (second==null) ClearFigures();
            UpdateCopy();
        }

        private void RebuildPair()
        {
            ClearFigures();
            if (first==null || second==null || figureMaterial==null) return;
            figureA=new GravityStickFigure(first.content,transform,cyan,figureMaterial);
            figureB=new GravityStickFigure(second.content,transform,gold,figureMaterial);
        }

        private void Cycle(bool slotA)
        {
            if (IsJumping || choices.Count<3) return;
            var current=slotA?first:second; var other=slotA?second:first;
            int index=choices.IndexOf(current);
            for(int i=1;i<=choices.Count;i++)
            {
                var next=choices[(index+i)%choices.Count];
                if(next==other) continue;
                if(slotA)first=next;else second=next;
                RebuildPair();UpdateCopy();return;
            }
        }

        public void Guess(GravityGuess guess)
        {
            if (first == null || second == null || figureA == null || figureB == null ||
                !figureA.Ready || !figureB.Ready) return;
            if (round.Choose(guess)) UpdateCopy();
        }

        public void GuessAgain()
        {
            if (round.Phase != GravityGuessPhase.Result) return;
            round.Reset();
            UpdateCopy();
        }

        private void PrimaryAction()
        {
            if (round.Phase == GravityGuessPhase.Result) GuessAgain();
            else JumpTogether();
        }

        public void JumpTogether()
        {
            if (first == null || second == null || figureA == null || figureB == null ||
                !figureA.Ready || !figureB.Ready || !round.Begin()) return;
            jumpStart = Time.unscaledTime;
            UpdateCopy();
        }

        private void LateUpdate()
        {
            exclusions.Clear();
            if(first==null || second==null || figureA==null || figureB==null || canvasRect==null) return;
            var camera=Camera.main;if(camera==null)return;
            if(!figureA.Ready || !figureB.Ready) {dock.gameObject.SetActive(false);return;}
            float radiusA=figureA.SurfaceBounds.extents.magnitude/Mathf.Sqrt(3);
            float radiusB=figureB.SurfaceBounds.extents.magnitude/Mathf.Sqrt(3);
            float size=Mathf.Min(radiusA,radiusB);
            float worldPerMeter=size*.85f/Mathf.Max(PlanetGravity.Peak(first.gravity),PlanetGravity.Peak(second.gravity));
            float elapsed=IsJumping?(Time.unscaledTime-jumpStart)*PlaybackRate:0;
            float end=Mathf.Max(PlanetGravity.FlightTime(first.gravity),PlanetGravity.FlightTime(second.gravity));
            if (IsJumping && elapsed >= end)
            {
                round.Finish(first.gravity, second.gravity);
                elapsed = 0;
                UpdateCopy();
            }
            figureA.Pose(camera,size*.65f,PlanetGravity.Height(first.gravity,elapsed)*worldPerMeter,
                IsJumping&&elapsed<PlanetGravity.FlightTime(first.gravity));
            figureB.Pose(camera,size*.65f,PlanetGravity.Height(second.gravity,elapsed)*worldPerMeter,
                IsJumping&&elapsed<PlanetGravity.FlightTime(second.gravity));
            obstacles.Clear();
            foreach(var choice in choices)
            {
                Rect rect;
                if(choice.content.TryGetScreenBounds(camera,canvasRect,out rect)) obstacles.Add(rect);
            }
            ProtectJump(figureA,first.gravity,size,worldPerMeter,camera);
            ProtectJump(figureB,second.gravity,size,worldPerMeter,camera);
            Rect safe=SafeRect();Rect placement;
            bool full=PlanetPanelLayout.PlaceOverlay(safe,obstacles,new Vector2(Mathf.Min(660,safe.width),280),
                new Vector2(Mathf.Min(360,safe.width),272),out placement);
            if(!full && !PlanetPanelLayout.PlaceOverlay(safe,obstacles,new Vector2(Mathf.Min(420,safe.width),160),
                new Vector2(Mathf.Min(280,safe.width),160),out placement))
            {dock.gameObject.SetActive(false);return;}
            dock.gameObject.SetActive(true);dock.sizeDelta=placement.size;dock.anchoredPosition=new Vector2(placement.xMin,placement.yMax);
            note.gameObject.SetActive(full);
            facts.gameObject.SetActive(full && Phase == GravityGuessPhase.Result);
            Region(prompt.rectTransform, choices.Count > 2 ? 52 : 12, 8, choices.Count > 2 ? 52 : 12, full ? 34 : 28);
            LayoutAnswers(full ? 48 : 44, full ? 76 : 56);
            var actionRect = jump.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0, 0);
            actionRect.anchorMax = new Vector2(1, 0);
            actionRect.offsetMin = new Vector2(12, full ? 44 : 6);
            actionRect.offsetMax = new Vector2(-12, full ? 108 : 58);
            RefreshControls();
            exclusions.Add(placement);
        }

        private void ProtectJump(GravityStickFigure figure,float gravity,float size,float scale,Camera camera)
        {
            var surface=figure.SurfaceBounds;
            float radius=Mathf.Max(surface.extents.x,Mathf.Max(surface.extents.y,surface.extents.z));
            var foot=surface.center+camera.transform.up*radius;
            var top=foot+camera.transform.up*(PlanetGravity.Peak(gravity)*scale+size*.7f);
            var bounds=new Bounds(foot,Vector3.one*size*.35f);bounds.Encapsulate(top);
            Rect rect;if(Project(bounds,camera,out rect)){obstacles.Add(rect);exclusions.Add(rect);}
        }

        private bool Project(Bounds bounds,Camera camera,out Rect rect)
        {
            rect=Rect.zero;if(camera.WorldToScreenPoint(bounds.center).z<=0)return false;
            var min=Vector2.positiveInfinity;var max=Vector2.negativeInfinity;
            for(int i=0;i<8;i++)
            {
                var point=camera.WorldToScreenPoint(bounds.center+Vector3.Scale(bounds.extents,
                    new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
                if(point.z<=0)continue;
                Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,point,null,out local);
                min=Vector2.Min(min,local);max=Vector2.Max(max,local);
            }
            rect=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return rect.Overlaps(canvasRect.rect);
        }

        private Rect SafeRect()
        {
            Vector2 min,max;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Screen.safeArea.min,null,out min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Screen.safeArea.max,null,out max);
            return Rect.MinMaxRect(min.x+16,min.y+Mathf.Min(200,(max.y-min.y)*.18f),max.x-16,max.y-110);
        }

        private static string Number(float value) => value.ToString("0.00",CultureInfo.InvariantCulture);
        private void UpdateCopy()
        {
            if (first == null || second == null) return;
            bool result = Phase == GravityGuessPhase.Result;
            selectA.GetComponentInChildren<Text>().text = first.info.display +
                (result ? "\n" + Number(PlanetGravity.Peak(first.gravity)) + " m" : "");
            selectB.GetComponentInChildren<Text>().text = second.info.display +
                (result ? "\n" + Number(PlanetGravity.Peak(second.gravity)) + " m" : "");
            prompt.text = result ? (round.IsCorrect ? "CORRECT!" : "NICE TRY!")
                : IsJumping ? "WATCH!" : "WHO JUMPS HIGHER?";
            facts.text = "";
            if (result)
            {
                if (round.Answer == GravityGuess.Same) facts.text = "SAME HEIGHT";
                else
                {
                    var low = first.gravity < second.gravity ? first : second;
                    var high = low == first ? second : first;
                    facts.text = low.info.display + "  ↑ " + Number(high.gravity / low.gravity) + "x";
                }
            }
            note.text = PlanetGravity.IsGiant(first.info.id) || PlanetGravity.IsGiant(second.info.id)
                ? "No air resistance | 1 bar, no ground | 0.45x"
                : "Same launch | No air resistance | 0.45x";
            Highlight(selectA, GravityGuess.First);
            Highlight(same, GravityGuess.Same);
            Highlight(selectB, GravityGuess.Second);
            RefreshControls();
        }

        private void Highlight(Button button, GravityGuess guess)
        {
            bool selected = Prediction == guess;
            var border = button.GetComponent<Outline>();
            border.enabled = selected;
            button.GetComponent<Image>().color = selected ? new Color(.12f, .30f, .38f) : new Color(.08f, .18f, .24f);
        }

        private void RefreshControls()
        {
            jump.interactable = Phase == GravityGuessPhase.Ready || Phase == GravityGuessPhase.Result;
            selectA.interactable = selectB.interactable = same.interactable =
                Phase == GravityGuessPhase.Guessing || Phase == GravityGuessPhase.Ready;
            cycleA.gameObject.SetActive(choices.Count > 2);
            cycleB.gameObject.SetActive(choices.Count > 2);
            cycleA.interactable = cycleB.interactable = !IsJumping;
            jumpLabel.text = Phase == GravityGuessPhase.Result ? "AGAIN" : IsJumping ? "..."
                : Phase == GravityGuessPhase.Ready ? "JUMP" : "PICK ONE";
        }

        private void LayoutAnswers(float top, float height)
        {
            var a = selectA.GetComponent<RectTransform>();
            Region(a, 12, top, 4, height); a.anchorMax = new Vector2(.4f, 1);
            var tie = same.GetComponent<RectTransform>();
            Region(tie, 4, top, 4, height); tie.anchorMin = new Vector2(.4f, 1); tie.anchorMax = new Vector2(.6f, 1);
            var b = selectB.GetComponent<RectTransform>();
            Region(b, 4, top, 12, height); b.anchorMin = new Vector2(.6f, 1);
        }

        private RectTransform NewRect(string name,Transform parent)
        {
            var host=new GameObject(name,typeof(RectTransform));host.transform.SetParent(parent,false);return host.GetComponent<RectTransform>();
        }
        private Text Label(string name,Transform parent,string value,int size,Color color)
        {
            var text=NewRect(name,parent).gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=size;
            text.color=color;text.raycastTarget=false;text.alignment=TextAnchor.MiddleLeft;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.Max(16,size-4);text.resizeTextMaxSize=size;return text;
        }
        private static void Region(RectTransform rect,float left,float top,float right,float height)
        {
            rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(left,-top-height);rect.offsetMax=new Vector2(-right,-top);
        }
        private Button Button(string name,Transform parent,string value,Color color,UnityEngine.Events.UnityAction action)
        {
            var rect=NewRect(name,parent);rect.gameObject.AddComponent<Image>().color=new Color(.08f,.18f,.24f,1);
            var button=rect.gameObject.AddComponent<Button>();button.onClick.AddListener(action);
            var text=Label("Label",rect,value,24,color);text.alignment=TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;text.rectTransform.offsetMin=Vector2.zero;text.rectTransform.offsetMax=Vector2.zero;
            return button;
        }
        private void BuildUI()
        {
            // ScanController may live on another Canvas: use an independent root so coordinates
            // match PlanetAnnotations (720-wide screen space), not a 100x100 nested canvas.
            canvasRect=NewRect("GravityLabCanvas",null);var canvas=canvasRect.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=55;
            var scaler=canvasRect.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=0;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            dock=NewRect("GravityLab",canvasRect);dock.pivot=new Vector2(0,1);dock.gameObject.AddComponent<Image>().color=new Color(.025f,.055f,.09f,.98f);
            var border=dock.gameObject.AddComponent<Outline>();border.effectColor=cyan;border.effectDistance=new Vector2(1,-1);
            details=NewRect("Details",dock);details.anchorMin=Vector2.zero;details.anchorMax=Vector2.one;details.offsetMin=details.offsetMax=Vector2.zero;
            prompt = Label("Question", details, "WHO JUMPS HIGHER?", 26, cyan);
            prompt.alignment = TextAnchor.MiddleCenter;
            Region(prompt.rectTransform, 12, 8, 12, 34);
            selectA = Button("GuessFirst", details, "", cyan, () => Guess(GravityGuess.First));
            same = Button("GuessSame", details, "=", Color.white, () => Guess(GravityGuess.Same));
            selectB = Button("GuessSecond", details, "", gold, () => Guess(GravityGuess.Second));
            foreach (var option in new[] { selectA, same, selectB })
            {
                var outline = option.gameObject.AddComponent<Outline>();
                outline.effectColor = cyan;
                outline.effectDistance = new Vector2(2, -2);
                outline.enabled = false;
            }
            LayoutAnswers(48, 76);
            cycleA = Button("CycleFirst", details, "<", cyan, () => Cycle(true));
            cycleB = Button("CycleSecond", details, ">", gold, () => Cycle(false));
            var previous = cycleA.GetComponent<RectTransform>();
            Region(previous, 8, 6, 0, 36); previous.anchorMax = new Vector2(0, 1); previous.offsetMax = new Vector2(44, -6);
            var next = cycleB.GetComponent<RectTransform>();
            Region(next, 0, 6, 8, 36); next.anchorMin = new Vector2(1, 1); next.offsetMin = new Vector2(-44, -42);
            facts = Label("Result", details, "", 28, Color.white);
            facts.alignment = TextAnchor.MiddleCenter;
            Region(facts.rectTransform, 14, 130, 14, 34);
            facts.gameObject.SetActive(false);
            note = Label("ScienceNote", dock, "", 17, new Color(.75f,.85f,.9f));
            note.rectTransform.anchorMin = Vector2.zero; note.rectTransform.anchorMax = new Vector2(1, 0);
            note.rectTransform.offsetMin = new Vector2(14, 4); note.rectTransform.offsetMax = new Vector2(-14, 36);
            jump = Button("JumpTogether", dock, "PICK ONE", Color.white, PrimaryAction);
            jumpLabel = jump.GetComponentInChildren<Text>();
            RefreshControls();
            dock.gameObject.SetActive(false);
        }
        private void ClearFigures(){if(figureA!=null)figureA.Dispose();if(figureB!=null)figureB.Dispose();figureA=figureB=null;round.Reset();exclusions.Clear();}
        private void OnEnable(){if(first!=null && second!=null)RebuildPair();}
        private void OnDisable(){ClearFigures();if(dock!=null)dock.gameObject.SetActive(false);}
        private void OnDestroy(){ClearFigures();if(canvasRect!=null)Destroy(canvasRect.gameObject);}
    }
}
