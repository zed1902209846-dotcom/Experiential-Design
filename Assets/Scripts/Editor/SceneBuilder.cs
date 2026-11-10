using System.Collections.Generic;
using System.IO;
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
    /// v6：一键生成 LoginScene（太空登录）/ MainMenuScene（Start Exploration）/ ScanScene（HUD + 8 行星知识卡）。
    /// 菜单：Solar System > Setup > Build All UI Scenes
    /// ScanScene 为幂等更新：只重建 UI 层（Canvas/ScanRoot/EventSystem/PreviewRig），不动任何 Vuforia 物体（ARCamera、*Target_V）。
    /// 知识卡左侧 3D 模型走 RenderTexture（PlanetPreviewRig），右侧四段英文文案来自 PlanetData。
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
        const string SplashPath = "Assets/UI/Splash.jpg";
        const string StartButtonPath = "Assets/UI/StartButton.jpg";
        const string BrowseIconPath = "Assets/UI/BrowseIcon.png";  // 太阳系图标按钮
        const string PrefabDir = "Assets/Inguz Media Studio/The Amazing Solar System/Prefabs";

        [MenuItem("Solar System/Setup/Build All UI Scenes")]
        public static void BuildAll()
        {
            if (!Directory.Exists(Dir)) Directory.CreateDirectory(Dir);
            BuildLogin();
            BuildMainMenu();
            BuildScanUI();
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("SceneBuilder v6: Login / MainMenu / Scan 三场景完成，已加入 Build Settings。");
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

        // ============================================================ Login
        static void BuildLogin()
        {
            var canvas = SetupScene("LoginScene");
            var root = canvas.transform;
            Splash(root);

            var card = Box("Card", root, 0, -180, 880, 980);
            Img(card, CardBg);

            var h = Box("Header", card.transform, 0, 380, 880, 90);
            Txt(h, "Login", 56, TextMain);

            InputField email, pass;
            Input("EmailInput", card.transform, "Email", false, 0, 180, 720, 100, out email);
            Input("PasswordInput", card.transform, "Password", true, 0, 40, 720, 100, out pass);

            var msg = Box("Message", card.transform, 0, -70, 760, 44);
            Txt(msg, "", 26, TextSub);

            Button bLogin, bRegister;
            Btn("LoginButton", card.transform, "LOGIN", Cyan, CyanDark, 40, 0, -200, 720, 120, out bLogin);
            Btn("RegisterButton", card.transform, "REGISTER", Purple, PurpleDark, 40, 0, -360, 720, 120, out bRegister);

            // ---- 注册面板 ----
            var reg = NewUI("RegisterPanel", root);
            FullStretch(reg);
            Img(reg, Dim);
            reg.SetActive(false);

            var rCard = Box("Card", reg.transform, 0, 0, 880, 1050);
            Img(rCard, CardBg);
            var rH = Box("Header", rCard.transform, 0, 430, 880, 80);
            Txt(rH, "Register", 48, TextMain);

            InputField rEmail, rPass, rConfirm;
            Input("EmailInput", rCard.transform, "Email", false, 0, 250, 720, 100, out rEmail);
            Input("PasswordInput", rCard.transform, "Password (min 6 chars)", true, 0, 110, 720, 100, out rPass);
            Input("ConfirmInput", rCard.transform, "Confirm Password", true, 0, -30, 720, 100, out rConfirm);

            var rMsg = Box("Message", rCard.transform, 0, -160, 760, 44);
            Txt(rMsg, "", 26, TextSub);

            Button rSubmit, rBack;
            Btn("SubmitButton", rCard.transform, "SIGN UP", Purple, PurpleDark, 38, 0, -300, 720, 120, out rSubmit);
            Btn("BackButton", rCard.transform, "BACK", InputBg, Color.clear, 34, 0, -450, 720, 100, out rBack);

            var loginGo = NewUI("LoginRoot", root);
            var loginCtrl = loginGo.AddComponent<LoginController>();
            Wire(loginCtrl, f =>
            {
                f("usernameInput", email);
                f("passwordInput", pass);
                f("messageText", msg.GetComponent<Text>());
                f("registerPanel", reg);
            });
            var regCtrl = reg.AddComponent<RegisterController>();
            Wire(regCtrl, f =>
            {
                f("usernameInput", rEmail);
                f("passwordInput", rPass);
                f("confirmInput", rConfirm);
                f("messageText", rMsg.GetComponent<Text>());
            });
            AddClick(bLogin, loginCtrl.Login);
            AddClick(bRegister, loginCtrl.OpenRegister);
            AddClick(rSubmit, regCtrl.Submit);
            AddClick(rBack, regCtrl.Close);

            Save("LoginScene");
        }

        // ============================================================ MainMenu
        static void BuildMainMenu()
        {
            var canvas = SetupScene("MainMenuScene");
            var root = canvas.transform;
            Splash(root);

            // 胶囊按钮（用设计图切片）
            var startGo = Box("StartButton", root, 0, -560, 760, 230);
            var startImg = startGo.AddComponent<Image>();
            var sp = LoadSprite(StartButtonPath);
            if (sp != null) { startImg.sprite = sp; startImg.preserveAspect = true; }
            else startImg.color = Cyan;
            var startBtn = startGo.AddComponent<Button>();
            var c = startBtn.colors;
            c.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            c.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            c.fadeDuration = 0.06f;
            startBtn.colors = c;

            Button bLogout;
            Btn("LogoutButton", root, "LOG OUT", InputBg, Color.clear, 28, 400, 880, 240, 80, out bLogout);

            // 9 个星球直达入口（3x3）：地球走合一扫描场景，其余各自独立场景
            string[,] entries =
            {
                { "EARTH",        "ScanScene" },
                { "MERCURY",      "MercuryScene" },
                { "MARS",         "MarsScene" },
                { "JUPITER",      "JupiterScene" },
                { "SATURN",       "SaturnScene" },
                { "URANUS",       "UranusScene" },
                { "NEPTUNE",      "NeptuneScene" },
                { "SUN",          "SunScene" },
                { "SOLAR SYSTEM", "SolarSystemScene" },
            };
            for (int i = 0; i < entries.GetLength(0); i++)
            {
                float x = -340 + (i % 3) * 340;
                float y = 300 - (i / 3) * 140;
                Button bPlanet;
                Btn("PlanetButton_" + i, root, entries[i, 0], InputBg, CyanDark, 26, x, y, 300, 110, out bPlanet);
                var launcher = bPlanet.gameObject.AddComponent<PlanetSceneLauncher>();
                Wire(launcher, f => f("sceneName", entries[i, 1]));
                AddClick(bPlanet, launcher.Open);
            }

            var menuGo = NewUI("MainMenuRoot", root);
            var menuCtrl = menuGo.AddComponent<MainMenuController>();
            AddClick(startBtn, menuCtrl.StartExploration);
            AddClick(bLogout, menuCtrl.Logout);

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
            Txt(banner, "PLEASE SCAN THE PLANET CARD", 40, Cyan);

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
            Txt(hint, "TARGET: Planet Card", 30, TextSub);

            // ---- 右下角悬浮浏览按钮（太阳系图标，识别成功后才显示）----
            var browseGo = Box("BrowseButton", root, 420, -800, 180, 180);
            var browseImg = browseGo.AddComponent<Image>();
            var browseSp = LoadSprite(BrowseIconPath);
            if (browseSp != null) { browseImg.sprite = browseSp; browseImg.preserveAspect = true; }
            else browseImg.color = Purple;
            var bBrowse = browseGo.AddComponent<Button>();
            var bbc = bBrowse.colors;
            bbc.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            bbc.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            bbc.fadeDuration = 0.06f;
            bBrowse.colors = bbc;

            // ---- 3D 预览 Rig（藏在 y=-1000，RenderTexture 供卡片使用）----
            var rigGo = new GameObject("PreviewRig");
            rigGo.transform.position = new Vector3(0, -1000, 0);
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetParent(rigGo.transform, false);
            spawnGo.layer = 8; // 专用预览层（未命名层，主相机永远不会看到这里）
            var camGo2 = new GameObject("PreviewCamera");
            camGo2.transform.SetParent(rigGo.transform, false);
            camGo2.transform.localPosition = new Vector3(0, 0.3f, -4f);
            camGo2.transform.LookAt(spawnGo.transform);
            var pCam = camGo2.AddComponent<Camera>();
            pCam.clearFlags = CameraClearFlags.SolidColor;
            pCam.backgroundColor = new Color(0, 0, 0, 0);
            pCam.cullingMask = 1 << 8;
            pCam.fieldOfView = 30f;
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(rigGo.transform, false);
            lightGo.transform.localPosition = new Vector3(2, 2, -2);
            var pl = lightGo.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.intensity = 2f;
            pl.range = 20f;
            var rig = rigGo.AddComponent<PlanetPreviewRig>();

            // ---- 知识卡面板 ----
            var panelGo = NewUI("PlanetCardPanel", root);
            FullStretch(panelGo);
            Img(panelGo, Dim);

            var cardBox = Box("Card", panelGo.transform, 0, 0, 1020, 1760);
            Img(cardBox, CardBg);

            var title = Box("Title", cardBox.transform, -60, 800, 760, 80);
            Txt(title, "Earth", 60, TextMain);
            var tagline = Box("Tagline", cardBox.transform, -60, 726, 760, 50);
            Txt(tagline, "Our Only Home", 32, Cyan);

            Button bClose;
            Btn("CloseButton", cardBox.transform, "X", InputBg, Color.clear, 36, 442, 800, 90, 90, out bClose);

            // Tabs：2 行 x 4
            var tabs = new List<Button>();
            for (int i = 0; i < 8; i++)
            {
                float x = -375 + (i % 4) * 250;
                float y = (i < 4) ? 655 : 572;
                Button tb;
                Btn("Tab_" + i, cardBox.transform, "Tab", InputBg, Color.clear, 26, x, y, 230, 84, out tb);
                tabs.Add(tb);
            }

            // 左：3D 动态预览（RawImage + RenderTexture）
            var previewGo = Box("Preview", cardBox.transform, -240, 310, 470, 400);
            var raw = previewGo.AddComponent<RawImage>();
            raw.color = Color.white;

            // 右：KEY FACTS 统计表（标签左对齐 / 数值右对齐，与参考图一致）
            var factsHead = Box("FactsHead", cardBox.transform, 245, 485, 430, 40);
            Txt(factsHead, "KEY FACTS", 28, Cyan, TextAnchor.MiddleLeft);

            var statLabels = new List<Text>();
            var statValues = new List<Text>();
            string[] statNames = { "ORDER FROM SUN", "DIAMETER", "DISTANCE FROM SUN", "ORBITAL PERIOD", "ROTATION PERIOD", "MOONS" };
            for (int i = 0; i < statNames.Length; i++)
            {
                float y = 445 - i * 58;

                var lbGo = Box("StatLabel_" + i, cardBox.transform, 35, y, 250, 46);
                ((RectTransform)lbGo.transform).pivot = new Vector2(0, 0.5f);
                statLabels.Add(Txt(lbGo, statNames[i], 22, TextSub, TextAnchor.MiddleLeft));

                var vlGo = Box("StatValue_" + i, cardBox.transform, 470, y, 230, 46);
                ((RectTransform)vlGo.transform).pivot = new Vector2(1, 0.5f);
                var tv = Txt(vlGo, "-", 24, TextMain, TextAnchor.MiddleRight);
                tv.fontStyle = FontStyle.Bold;
                statValues.Add(tv);
            }

            // 下方四段说明文本
            Text tWhat = Section(cardBox.transform, "WhatSection", "1. WHAT IS IT?", -70);
            Text tSpecial = Section(cardBox.transform, "SpecialSection", "2. HOW SPECIAL?", -290);
            Text tLife = Section(cardBox.transform, "LifeSection", "3. LIFE", -510);
            Text tFun = Section(cardBox.transform, "FunSection", "4. FUN FACT", -730);

            // ---- 控制器与绑定 ----
            var scanRoot = NewUI("ScanRoot", root);
            var scanCtrl = scanRoot.AddComponent<ScanController>();
            var cardCtrl = scanRoot.AddComponent<PlanetCardController>();
            var scanUI = scanRoot.AddComponent<ARScanUIController>();

            // 绑 prefab 列表
            var prefabs = new List<Object>();
            foreach (var info in PlanetData.All)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + info.display + ".prefab");
                if (p != null) prefabs.Add(p);
            }
            Wire(rig, f =>
            {
                f("previewCamera", pCam);
                f("spawnPoint", spawnGo.transform);
                f("planetPrefabs", prefabs.ToArray());
            });
            Wire(cardCtrl, f =>
            {
                f("panel", panelGo);
                f("previewRig", rig);
                f("previewImage", raw);
                f("titleText", title.GetComponent<Text>());
                f("taglineText", tagline.GetComponent<Text>());
                f("whatText", tWhat);
                f("specialText", tSpecial);
                f("lifeText", tLife);
                f("funText", tFun);
                f("tabButtons", tabs.ToArray());
                f("statLabels", statLabels.ToArray());
                f("statValues", statValues.ToArray());
            });
            Wire(scanCtrl, f =>
            {
                f("card", cardCtrl);
                f("scanUI", scanUI);
                f("browseButton", browseGo);
            });
            Wire(scanUI, f =>
            {
                f("statusText", status.GetComponent<Text>());
                f("scanFrame", frame);
                f("hintLabel", hint.GetComponent<Text>());
            });
            AddClick(bClose, cardCtrl.Close);
            AddClick(bBrowse, scanCtrl.OpenBrowser);
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
            foreach (var n in new[] { "LoginScene", "MainMenuScene", "ScanScene" })
            {
                var p = Dir + "/" + n + ".unity";
                if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
