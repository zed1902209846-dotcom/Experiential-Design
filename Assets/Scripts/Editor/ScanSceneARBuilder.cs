using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

namespace SolarSystemAR.EditorTools
{
    /// 第5步构建器（幂等）：
    /// 1. 切到 ScanScene（当前场景未保存改动时中止）
    /// 2. 清理 probe 遗留的未命名 ImageTarget
    /// 3. ARCamera（缺失则创建）+ PhysicsRaycaster
    /// 4. 建 8 大行星 ImageTarget（尺寸取自 SolarCards.xml），挂 VuforiaTargetController
    /// 5. 每个目标下挂 ScanPlanetContent（earth/jupiter 复用已有内容，其余新建）
    /// 6. PlanetScanController.planets 列表写入 8 条绑定（目标名/内容/科普卡片）
    /// 7. 保存场景
    public static class ScanSceneARBuilder
    {
        const string OutPath = @"D:\Trae生成文件\666\vuf_build.txt";
        const string ScenePath = "Assets/Scenes/App/ScanScene.unity";
        const string PrefabRoot = "Assets/Inguz Media Studio/The Amazing Solar System/Prefabs/";
        const string CardFolder = "Assets/UI/Cards/";

        private class PlanetDef
        {
            public string go, target, prefab, card, content, ds;
            public float w, h, dia;

            public PlanetDef(string go, string target, float w, float h, string prefab, string card, string content, float dia = CardW * 0.5f, string ds = "SolarCards")
            {
                this.go = go; this.target = target; this.w = w; this.h = h;
                this.prefab = prefab; this.card = card; this.content = content;
                this.dia = dia; this.ds = ds;
            }
        }

        /// 实体打印卡宽度（米）。所有卡片同为 6 cm，模型直径取卡面宽的 50%。
        const float CardW = 0.06f;

        static readonly PlanetDef[] Defs =
        {
            new PlanetDef("EarthTarget_V",   "earth",   CardW, CardW * 1.3361343f, "Earth.prefab",       "EarthCard.jpg",   "EarthContent"),
            new PlanetDef("VenusTarget_V",   "venus1",  CardW, CardW * 0.9375f,     "Venus.prefab",      "VenusCard.jpg",   "VenusContent", CardW * 0.5f, "Vuforia/VenusCard.xml"),
            new PlanetDef("JupiterTarget_V", "jupiter", CardW, CardW * 1.1146953f, "Jupiter.prefab",     "JupiterCard.jpg", "JupiterContent"),
            new PlanetDef("MarsTarget_V",    "mars",    CardW, CardW * 1.0070423f, "Mars.prefab",        "MarsCard.jpg",    "MarsContent"),
            new PlanetDef("MercuryTarget_V", "mercury", CardW, CardW * 1.0404414f, "Mercury.prefab",     "MercuryCard.jpg", "MercuryContent"),
            new PlanetDef("SaturnTarget_V",  "saturn",  CardW, CardW * 1.077966f,  "Assets/Prefabs/SaturnWithRings.prefab", "SaturnCard.jpg",  "SaturnContent"),
            new PlanetDef("UranusTarget_V",  "uranus",  CardW, CardW * 1.0915253f, "Uranus.prefab",      "UranusCard.jpg",  "UranusContent"),
            new PlanetDef("NeptuneTarget_V", "neptune", CardW, CardW * 1.089041f,  "Neptune.prefab",     "NeptuneCard.jpg", "NeptuneContent"),
            new PlanetDef("SunTarget_V",         "sun",         CardW, CardW * 1.1038064f, "Sun.prefab",         "SunCard.jpg",         "SunContent"),
            new PlanetDef("SolarSystemTarget_V", "solarsystem", CardW, CardW * 1.0920717f, "SolarSystem.prefab", "SolarSystemCard.jpg", "SolarSystemContent", CardW * 0.5f),
        };

        static readonly System.Text.StringBuilder Log = new System.Text.StringBuilder();

        [MenuItem("Solar System/Setup/Step5 Build AR Targets")]
        public static void Run()
        {
            try { Build(); }
            catch (System.Exception e) { Log.AppendLine("EXCEPTION: " + e); }
            File.WriteAllText(OutPath, Log.ToString());
            Debug.Log("[BuildAR]\n" + Log);
        }

        static void Build()
        {
            // 0. 场景切换（脏场景不自动丢弃）
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.Replace('\\', '/').EndsWith("ScanScene.unity"))
            {
                if (scene.isDirty) { Log.AppendLine("ABORT: current scene has unsaved changes: " + scene.path); return; }
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Log.AppendLine("opened ScanScene");
            }

            // 1. 清理 probe 遗留
            var leftover = GameObject.Find("ImageTarget");
            if (leftover != null && leftover.GetComponent<ImageTargetBehaviour>() != null)
            {
                Undo.DestroyObjectImmediate(leftover);
                Log.AppendLine("removed leftover ImageTarget");
            }

            // 1b. 清理孤儿目标（旧版空壳，无 ImageTargetBehaviour）
            foreach (var name in new[] { "EarthTarget", "JupiterTarget" })
            {
                var orphan = GameObject.Find(name);
                if (orphan != null)
                {
                    Undo.DestroyObjectImmediate(orphan);
                    Log.AppendLine("removed orphan " + name);
                }
            }

            // 2. ARCamera（缺失则创建）+ PhysicsRaycaster
            var arcam = GameObject.Find("ARCamera");
            if (arcam == null)
            {
                bool ok = EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera");
                arcam = Selection.activeGameObject;
                if (!ok || arcam == null) { Log.AppendLine("FATAL: cannot create ARCamera"); return; }
                Log.AppendLine("ARCamera created");
            }
            if (arcam.GetComponent<PhysicsRaycaster>() == null)
                Undo.AddComponent<PhysicsRaycaster>(arcam);
            Log.AppendLine("PhysicsRaycaster on ARCamera: OK");

            // 3. 卡片 Sprite 导入设置（幂等）
            foreach (var d in Defs) EnsureSpriteImport(CardFolder + d.card);

            // 3b. 太阳系组合预制体（缺则按轨道环拼一个）
            EnsureSolarSystemPrefab();

            // 4. 全部 Image Target + 控制器 + 内容
            int built = 0;
            foreach (var d in Defs)
            {
                var t = BuildTarget(d);
                if (t == null) continue;
                EnsureController(t, d.target);
                var content = EnsureContent(d, t.transform);
                bool prefabOk = content != null && CheckPrefabAssigned(content);
                bool cardOk = AssetDatabase.LoadAssetAtPath<Sprite>(CardFolder + d.card) != null;
                Log.AppendLine(d.target + " | content=" + (content != null ? content.name : "NULL")
                             + " | PREFAB=" + (prefabOk ? "OK" : "FAIL")
                             + " | CARD=" + (cardOk ? "OK" : "FAIL"));
                if (content != null) built++;
            }

            // 5. 无需绑定列表：ScanController.Awake() 会自行 FindObjectsByType<ScanPlanetContent> 发现内容

            // 6. 保存
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log.AppendLine("SCENE SAVED: " + scene.path + " | content ready: " + built + "/" + Defs.Length);
        }

        /// 太阳系卡需要一个「整体」模型，Inguz 素材包里没有，这里用现有 prefab 现拼：
        /// 太阳居中，八大行星按轨道环排列（静态）。轨道半径/行星直径均以「太阳直径」为单位，
        /// 最终整体大小由 ScanPlanetContent.modelDiameter 决定（首个 Renderer 就是太阳）。
        static void EnsureSolarSystemPrefab()
        {
            var path = PrefabRoot + "SolarSystem.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Log.AppendLine("SolarSystem.prefab: exists");
                return;
            }

            var sunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "Sun.prefab");
            if (sunPrefab == null) { Log.AppendLine("SolarSystem.prefab: Sun.prefab missing -> skipped"); return; }

            var root = new GameObject("SolarSystem");
            try
            {
                var sun = (GameObject)PrefabUtility.InstantiatePrefab(sunPrefab);
                sun.transform.SetParent(root.transform, false);
                sun.transform.localPosition = Vector3.zero;

                float sunSize = MaxDim(sun);
                if (sunSize <= 0.0001f) sunSize = 1f;

                string[] names = { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
                float[] orbit  = { 0.70f, 0.95f, 1.20f, 1.45f, 1.75f, 2.00f, 2.15f, 2.25f };
                float[] size   = { 0.14f, 0.18f, 0.20f, 0.16f, 0.26f, 0.22f, 0.18f, 0.17f };

                for (int i = 0; i < names.Length; i++)
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(names[i] == "Saturn"
                        ? "Assets/Prefabs/SaturnWithRings.prefab" : PrefabRoot + names[i] + ".prefab");
                    if (pf == null) { Log.AppendLine("  solar system: missing " + names[i] + ".prefab"); continue; }

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf);
                    inst.transform.SetParent(root.transform, false);

                    float a = i * Mathf.PI * 2f / names.Length;
                    inst.transform.localPosition = new Vector3(Mathf.Cos(a) * orbit[i] * sunSize, 0f, Mathf.Sin(a) * orbit[i] * sunSize);
                    inst.transform.localRotation = Quaternion.identity;

                    float m = MaxDim(inst);
                    if (m > 0.0001f) inst.transform.localScale *= (size[i] * sunSize / m);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Log.AppendLine("SolarSystem.prefab CREATED (" + names.Length + " orbits)");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// 取首个 Renderer 的最大尺寸（用 localBounds，避免未渲染时 bounds 为 0）
        static float MaxDim(GameObject go)
        {
            var r = go.GetComponentInChildren<Renderer>(true);
            if (r == null) return 0f;
            var s = r.localBounds.size;
            var ls = r.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x * ls.x), Mathf.Abs(s.y * ls.y), Mathf.Abs(s.z * ls.z));
        }

        /// 知识卡左侧 3D 预览按 prefab 名匹配 planetId，所以只需把缺的补齐即可（顺序无关）
        static GameObject BuildTarget(PlanetDef d)
        {
            var existing = GameObject.Find(d.go);
            if (existing != null && existing.GetComponent<ImageTargetBehaviour>() != null)
            {
                Configure(existing.GetComponent<ImageTargetBehaviour>(), d.target, d.w, d.h, d.ds);
                return existing;
            }

            bool ok = EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/Image Target");
            var go = Selection.activeGameObject;
            if (!ok || go == null || go.GetComponent<ImageTargetBehaviour>() == null)
            {
                Log.AppendLine("FAIL: cannot create " + d.go);
                return null;
            }
            go.name = d.go;
            Configure(go.GetComponent<ImageTargetBehaviour>(), d.target, d.w, d.h, d.ds);
            return go;
        }

        static void Configure(ImageTargetBehaviour itb, string targetName, float w, float h, string ds)
        {
            var so = new SerializedObject(itb);
            SetString(so, "mTrackableName", targetName);
            SetString(so, "mDataSetPath", ds);
            SetFloat(so, "mWidth", w);
            SetFloat(so, "mHeight", h);
            SetFloat(so, "mAspectRatio", w / h);
            SetBool(so, "mInitializedInEditor", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(itb);
        }

        static void EnsureController(GameObject go, string simulateName)
        {
            var c = go.GetComponent<SolarSystemAR.VuforiaTargetController>();
            if (c == null) c = Undo.AddComponent<SolarSystemAR.VuforiaTargetController>(go);
            var so = new SerializedObject(c);
            var p = so.FindProperty("simulateTargetName");
            if (p != null && p.propertyType == SerializedPropertyType.String)
            {
                p.stringValue = simulateName;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static ScanPlanetContent EnsureContent(PlanetDef d, Transform parent)
        {
            // 已有内容（含挂在旧壳下的 earth/jupiter）：找到并迁移
            ScanPlanetContent content = null;
            var existingGo = GameObject.Find(d.content);
            if (existingGo != null) content = existingGo.GetComponent<ScanPlanetContent>();

            bool created = content == null;
            if (created)
            {
                // 新建：挂 ScanPlanetContent 并指向行星 prefab
                var newGo = new GameObject(d.content);
                Undo.RegisterCreatedObjectUndo(newGo, "CreateContent " + d.content);
                content = newGo.AddComponent<ScanPlanetContent>();
            }

            // 同时修复已存在内容的引用，避免旧的无环土星被保留下来。
            string prefabPath = d.prefab.StartsWith("Assets/") ? d.prefab : PrefabRoot + d.prefab;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Log.AppendLine("  prefab missing: " + prefabPath); return content; }
            var cso = new SerializedObject(content);
            var pp = cso.FindProperty("planetPrefab");
            if (pp != null && pp.propertyType == SerializedPropertyType.ObjectReference)
                pp.objectReferenceValue = prefab;
            if (created)
            {
                SetFloat(cso, "modelDiameter", d.dia);
                SetFloat(cso, "rotationSpeed", 20f);
            }
            cso.ApplyModifiedPropertiesWithoutUndo();

            if (content.transform.parent != parent)
                Undo.SetTransformParent(content.transform, parent, "MoveContent " + d.content);
            content.transform.localPosition = Vector3.zero;
            return content;
        }

        static bool CheckPrefabAssigned(ScanPlanetContent content)
        {
            var so = new SerializedObject(content);
            var pp = so.FindProperty("planetPrefab");
            return pp != null && pp.objectReferenceValue != null;
        }

        static void EnsureSpriteImport(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Log.AppendLine("  card missing: " + path); return; }
            if (imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.SaveAndReimport();
            }
        }

        static void SetString(SerializedObject so, string name, string val)
        {
            var p = so.FindProperty(name);
            if (p == null) { Log.AppendLine("  prop missing: " + name); return; }
            if (p.propertyType != SerializedPropertyType.String) { Log.AppendLine("  prop " + name + " type=" + p.propertyType); return; }
            p.stringValue = val;
        }

        static void SetFloat(SerializedObject so, string name, float val)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = val;
        }

        static void SetBool(SerializedObject so, string name, bool val)
        {
            var p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = val;
        }
    }
}
