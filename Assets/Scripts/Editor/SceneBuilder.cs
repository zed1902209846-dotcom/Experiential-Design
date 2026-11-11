using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolarSystemAR;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SolarSystemAR.EditorBuildScripts
{
    /// 一键生成免登录 MainMenuScene 和 ScanScene（HUD + 行星知识卡）。
    /// 菜单：Solar System > Setup > Build All UI Scenes
    /// ScanScene 为幂等更新：只重建 UI 层（Canvas/ScanRoot/EventSystem/PreviewRig），不动任何 Vuforia 物体（ARCamera、*Target_V）。
    /// 英文名称与侧边介绍由运行时 PlanetAnnotations 跟随模型生成。
    public static class SceneBuilder
    {
        // ---- 色板 ----
        static readonly Color Cyan = Hex("22B8E6");
        static readonly Color CyanDark = Hex("1787AC");
        static readonly Color Purple = Hex("7A5AF8");
        static readonly Color PurpleDark = Hex("5B3FD4");
        static readonly Color TextMain = Hex("F2F7F4");
        static readonly Color TextSub = Hex("A8B8B0");
        static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        static readonly Color CardBg = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        static readonly Color InputBg = new Color(1f, 1f, 1f, 0.08f);

        const string Dir = "Assets/Scenes/App";
        const string SplashPath = "Assets/UI/HomeArtwork.png";
        const string BrowseIconPath = "Assets/UI/BrowseIcon.png";  // 太阳系图标按钮
        const string PrefabDir = "Assets/Inguz Media Studio/The Amazing Solar System/Prefabs";

        [MenuItem("Solar System/Setup/Build All UI Scenes")]
        public static void BuildAll()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            BuildMainMenu();
            BuildScanUI();
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("SceneBuilder: MainMenu / Scan 场景完成，已加入 Build Settings。");
        }

        [MenuItem("Solar System/Setup/Build Scan Scene UI Only")]
        public static void BuildScanSceneUIOnly()
        {
            BuildScanUI();
            AssetDatabase.SaveAssets();
            Debug.Log("SceneBuilder: ScanScene UI rebuilt.");
        }

        // ============================================================ Vuforia 重绑
        /// 新数据库目标名为中文（地球/火星…），把场景里 8 个 ImageTargetBehaviour 的
        /// mTrackableName 从英文名改为对应中文名。导入新数据库后运行一次。
        [MenuItem("Solar System/Setup/Rebind Vuforia Targets (Chinese Names)")]
        public static void RebindVuforiaTargets()
        {
            var map = new Dictionary<string, string>
            {
                { "mercury", "水星" }, { "venus", "金星" }, { "earth", "地球" }, { "mars", "火星" },
                { "jupiter", "木星" }, { "saturn", "土星" }, { "uranus", "天王星" }, { "neptune", "海王星" },
            };
            string path = Dir + "/ScanScene.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int rebound = 0;
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb == null || mb.GetType().Name != "ImageTargetBehaviour") continue;
                var so = new SerializedObject(mb);
                var tn = so.FindProperty("mTrackableName");
                if (tn == null) continue;
                string cur = (tn.stringValue ?? "").Trim();
                // 去掉尾部数字（earth1 → earth）再查映射
                string key = System.Text.RegularExpressions.Regex.Replace(cur.ToLower(), @"\d+$", "");
                if (!map.TryGetValue(key, out string zh)) continue;
                if (cur == zh) continue;
                tn.stringValue = zh;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mb);
                rebound++;
                Debug.Log($"Rebind: {mb.gameObject.name} : {cur} → {zh}");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"RebindVuforiaTargets 完成，重绑 {rebound} 个目标。");
        }

        // ============================================================ MainMenu
        static void BuildMainMenu()
        {
            var canvas = SetupScene("MainMenuScene");
            var root = canvas.transform;
            Splash(root);

            var startGo = Box("StartButton", root, 0, 0, 168, 168);
            var icon = startGo.AddComponent<ScanEntryIcon>();
            var startBtn = startGo.AddComponent<Button>();
            startBtn.targetGraphic = icon;
            var colors = startBtn.colors;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(.65f,.8f,.9f);
            colors.fadeDuration = .1f;
            startBtn.colors = colors;
            var menuGo = NewUI("MainMenuRoot", root);
            var menuCtrl = menuGo.AddComponent<MainMenuController>();
            AddClick(startBtn, menuCtrl.StartExploration);
            menuGo.AddComponent<MainMenuPresentation>().Configure(root.Find("Background").GetComponent<Image>(),startBtn);
            Camera.main.backgroundColor = new Color(0,.025f,.065f);

            Save("MainMenuScene");
        }

        // ============================================================ Scan（幂等：只重建 UI，不动 Vuforia）
        static void BuildScanUI()
        {
            string path = Dir + "/ScanScene.unity";
            UnityEngine.SceneManagement.Scene scene;
            if (File.Exists(path))
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // 清掉旧 UI（保留 ARCamera / *Target_V / Directional Light 等）
            foreach (var n in new[] { "Canvas", "ScanRoot", "ARScanUIRoot", "EventSystem", "PreviewRig" })
                DeleteRoot(n);
            // 无相机则补一台编辑器预览相机（真机由 ARCamera 取代）
            if (GameObject.Find("ARCamera") == null && Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }

            // ---- Canvas ----
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cv = canvasGo.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            // 匹配宽度：任意手机比例下可视宽度恒为 1080 参考单位，卡片（1020 宽）永不左右溢出。
            scaler.matchWidthOrHeight = 0f;
            var root = canvasGo.transform;

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // ---- 扫描 HUD ----
            var banner = Box("Banner", root, 0, 860, 1080, 80);
            Txt(banner, "SCAN PLANET CARDS", 40, Cyan);

            Button bBack;
            Btn("BackButton", root, "< BACK", InputBg, Color.clear, 32, -420, 880, 240, 96, out bBack);

            var frame = NewUI("ScanFrame", root);
            MakeCorner(frame.transform, -185, 255, 150, 10, Cyan);
            MakeCorner(frame.transform, -255, 185, 10, 150, Cyan);
            MakeCorner(frame.transform, 185, 255, 150, 10, Cyan);
            MakeCorner(frame.transform, 255, 185, 10, 150, Cyan);
            MakeCorner(frame.transform, -185, -255, 150, 10, Cyan);
            MakeCorner(frame.transform, -255, -185, 10, 150, Cyan);
            MakeCorner(frame.transform, 185, -255, 150, 10, Cyan);
            MakeCorner(frame.transform, 255, -185, 10, 150, Cyan);

            var status = Box("Status", root, 0, 420, 1080, 60);
            Txt(status, "SCANNING...", 40, Cyan);

            var hint = Box("Hint", root, 0, -700, 1080, 50);
            Txt(hint, "Tap a planet to learn more", 30, TextSub);

            var scanRoot = NewUI("ScanRoot", root);
            var scanCtrl = scanRoot.AddComponent<ScanController>();
            var scanUI = scanRoot.AddComponent<ARScanUIController>();
            Wire(scanCtrl, f => f("scanUI", scanUI));
            Wire(scanUI, f =>
            {
                f("statusText", status.GetComponent<Text>());
                f("scanFrame", frame);
                f("hintLabel", hint.GetComponent<Text>());
            });
            AddClick(bBack, scanCtrl.GoBack);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        // 四段文案块：编号标题 + 正文
        static Text Section(Transform parent, string name, string heading, float cy)
        {
            var head = Box(name + "_Head", parent, -420, cy + 95, 940, 50);
            var rtH = (RectTransform)head.transform;
            rtH.pivot = new Vector2(0, 0.5f);
            Txt(head, heading, 32, Cyan, TextAnchor.UpperLeft);

            var body = Box(name + "_Body", parent, -420, cy, 940, 130);
            var rtB = (RectTransform)body.transform;
            rtB.pivot = new Vector2(0, 0.5f);
            var t = Txt(body, "", 27, TextMain, TextAnchor.UpperLeft);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        // ============================================================ Helpers
        static void DeleteRoot(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }

        static GameObject SetupScene(string sceneName)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            return canvasGo;
        }

        static void Splash(Transform root)
        {
            var go = NewUI("Background", root);
            FullStretch(go);
            var img = go.AddComponent<Image>();
            var sp = LoadSprite(SplashPath);
            if (sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = false;
                // 填满竖屏：轻微放大裁边
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(200, 0);
            }
            else img.color = Hex("0B1210");
        }

        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject Box(string name, Transform parent, float cx, float cy, float w, float h)
        {
            var go = NewUI(name, parent);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, cy);
            return go;
        }

        static void FullStretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Image Img(GameObject go, Color color)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static Text Txt(GameObject parent, string content, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var t = parent.GetComponent<Text>();
            if (t == null) t = parent.AddComponent<Text>();
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void Input(string name, Transform parent, string placeholder, bool password, float cx, float cy, float w, float h, out InputField field)
        {
            var go = Box(name, parent, cx, cy, w, h);
            Img(go, InputBg);
            field = go.AddComponent<InputField>();

            var phGo = NewUI("Placeholder", go.transform);
            FullStretch(phGo);
            var rtP = (RectTransform)phGo.transform;
            rtP.offsetMin = new Vector2(32, 0);
            var ph = Txt(phGo, placeholder, 30, TextSub, TextAnchor.MiddleLeft);

            var txtGo = NewUI("Text", go.transform);
            FullStretch(txtGo);
            var rtT = (RectTransform)txtGo.transform;
            rtT.offsetMin = new Vector2(32, 0);
            var txt = Txt(txtGo, "", 32, TextMain, TextAnchor.MiddleLeft);
            txt.supportRichText = false;

            field.placeholder = ph;
            field.textComponent = txt;
            if (password) field.contentType = InputField.ContentType.Password;
        }

        static void Btn(string name, Transform parent, string label, Color normal, Color pressed, int fontSize, float cx, float cy, float w, float h, out Button btn)
        {
            var go = Box(name, parent, cx, cy, w, h);
            Img(go, normal);
            btn = go.AddComponent<Button>();
            var c = btn.colors;
            if (pressed != Color.clear) c.pressedColor = pressed;
            c.highlightedColor = normal * 1.12f;
            c.fadeDuration = 0.06f;
            btn.colors = c;

            var txtGo = NewUI("Label", go.transform);
            FullStretch(txtGo);
            Txt(txtGo, label, fontSize, TextMain);
        }

        static void MakeCorner(Transform parent, float x, float y, float w, float h, Color color)
        {
            var go = Box("Corner", parent, x, y, w, h);
            Img(go, color);
        }

        static void Wire(Component c, System.Action<System.Action<string, object>> bind)
        {
            var so = new SerializedObject(c);
            bind((field, value) =>
            {
                var p = so.FindProperty(field);
                if (p == null) { Debug.LogError("SceneBuilder: 字段不存在 " + c.GetType().Name + "." + field); return; }
                switch (value)
                {
                    case UnityEngine.Object[] objs:
                        p.arraySize = objs.Length;
                        for (int i = 0; i < objs.Length; i++)
                            p.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
                        break;
                    case UnityEngine.Object obj: p.objectReferenceValue = obj; break;
                    case float fv: p.floatValue = fv; break;
                    case bool bv: p.boolValue = bv; break;
                    case string sv: p.stringValue = sv; break;
                }
            });
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddClick(Button b, UnityEngine.Events.UnityAction call)
        {
            UnityEventTools.AddPersistentListener(b.onClick, call);
        }

        static Sprite LoadSprite(string path)
        {
            // 新导入的图片默认不是 Sprite 类型，这里自动修正导入设置。
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
            }
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp == null) Debug.LogWarning("SceneBuilder: 找不到 Sprite " + path);
            return sp;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        static void Save(string name)
        {
            var path = Dir + "/" + name + ".unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        }

        static void AddToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>();
            foreach (var n in new[] { "MainMenuScene", "ScanScene" })
            {
                var p = Dir + "/" + n + ".unity";
                if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            }
            // 保留各行星场景；重建 UI 不能丢掉已配置的构建入口。
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == Dir + "/LoginScene.unity") continue;
                if (list.All(s => s.path != scene.path)) list.Add(scene);
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
