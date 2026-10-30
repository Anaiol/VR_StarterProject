#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Configura automàticament els canvis dels scripts nous:
//  1. Prefab CubPistola  -> afegeix CubeExplode amb el model trencat i el material
//  2. GameScene          -> crea la MissZone darrere del jugador
//  3. LobbyScene         -> crea Text_BestScore, afegeix LobbyUI i posa sceneName al botó
// Menú: Tools > VR Project > Configurar escenes i prefabs
public static class ProjectSetup
{
    const string CubPistolaPath   = "Assets/Prefabs/CubPistola.prefab";
    const string ShatterFbxPath   = "Assets/VR-Game-Assets/Prefabs/Gun Cube/GunCubeShatterAnimation.FBX";
    const string GunCubeMatPath   = "Assets/VR-Game-Assets/Materials/Gun Cube/GunCube.mat";

    [MenuItem("Tools/VR Project/Configurar escenes i prefabs")]
    public static void RunAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previousScene = SceneManager.GetActiveScene().path;

        string log = "";
        log += SetupCubPistola() + "\n";
        log += SetupGameScene() + "\n";
        log += SetupLobbyScene() + "\n";

        if (!string.IsNullOrEmpty(previousScene))
            EditorSceneManager.OpenScene(previousScene);

        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup]\n" + log);
        EditorUtility.DisplayDialog("Configuració feta", log, "D'acord");
    }

    // ---------- 1. Prefab CubPistola ----------
    static string SetupCubPistola()
    {
        GameObject shatter = AssetDatabase.LoadAssetAtPath<GameObject>(ShatterFbxPath);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(GunCubeMatPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(CubPistolaPath) == null)
            return "✗ No trobo " + CubPistolaPath;

        GameObject root = PrefabUtility.LoadPrefabContents(CubPistolaPath);
        try
        {
            CubeExplode explode = root.GetComponent<CubeExplode>();
            if (explode == null) explode = root.AddComponent<CubeExplode>();
            if (explode.shatterPrefab == null) explode.shatterPrefab = shatter;
            if (explode.shatterMaterial == null) explode.shatterMaterial = mat;
            PrefabUtility.SaveAsPrefabAsset(root, CubPistolaPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return "✓ CubPistola: CubeExplode afegit"
            + (shatter == null ? " (ATENCIÓ: no trobo el model trencat, assigna'l a mà)" : "")
            + (mat == null ? " (sense material)" : "");
    }

    // ---------- 2. GameScene ----------
    static string SetupGameScene()
    {
        Scene scene;
        if (!OpenSceneByName("GameScene", out scene)) return "✗ No trobo la GameScene";

        GameObject zone = FindInScene(scene, "MissZone");
        if (zone == null)
        {
            zone = new GameObject("MissZone");
            SceneManager.MoveGameObjectToScene(zone, scene);

            // Posició: 1,5 m darrere del jugador, en la direcció contrària al generador de cubs
            Vector3 player = FindPlayerPosition(scene);
            Vector3 toPlayer = Vector3.back; // els cubs vénen en -Z
            blockGeneratorScript gen = FindComponent<blockGeneratorScript>(scene);
            if (gen != null)
            {
                Vector3 d = player - gen.transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.01f) toPlayer = d.normalized;
            }

            zone.transform.position = new Vector3(player.x, 0f, player.z) + toPlayer * 1.5f;
            zone.transform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);

            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 1.5f, 0f);
            box.size = new Vector3(6f, 4f, 0.5f);
        }
        else if (zone.GetComponent<BoxCollider>() == null)
        {
            BoxCollider box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }

        if (zone.GetComponent<CubeMissZone>() == null) zone.AddComponent<CubeMissZone>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "✓ GameScene: MissZone creada a " + zone.transform.position.ToString("F1");
    }

    // ---------- 3. LobbyScene ----------
    static string SetupLobbyScene()
    {
        Scene scene;
        if (!OpenSceneByName("LobbyScene", out scene)) return "✗ No trobo la LobbyScene";
        string result = "✓ LobbyScene:";

        // Text del best score: còpia de "Text_GameStartsIn", una mica per sobre
        GameObject best = FindInScene(scene, "Text_BestScore");
        if (best == null)
        {
            GameObject template = FindInScene(scene, "Text_GameStartsIn");
            if (template == null) return "✗ LobbyScene: no trobo Text_GameStartsIn per copiar-lo";

            best = Object.Instantiate(template, template.transform.parent);
            best.name = "Text_BestScore";
            best.SetActive(true);

            RectTransform rt = best.GetComponent<RectTransform>();
            RectTransform trt = template.GetComponent<RectTransform>();
            if (rt != null && trt != null)
            {
                rt.anchoredPosition = trt.anchoredPosition + Vector2.up * (trt.rect.height * 1.6f);
            }

            TMP_Text t = best.GetComponent<TMP_Text>();
            if (t != null) t.text = "Best score: 0";
            result += " Text_BestScore creat,";
        }

        // LobbyUI a [MANAGERS] (o a un GameObject nou si no existeix)
        GameObject managers = FindInScene(scene, "[MANAGERS]");
        if (managers == null)
        {
            managers = new GameObject("[MANAGERS]");
            SceneManager.MoveGameObjectToScene(managers, scene);
        }
        LobbyUI lobby = managers.GetComponent<LobbyUI>();
        if (lobby == null) lobby = managers.AddComponent<LobbyUI>();
        lobby.bestScoreText = best.GetComponent<TMP_Text>();
        result += " LobbyUI connectat,";

        // Botó: nom de l'escena a carregar
        Autohand.buttonScript button = FindComponent<Autohand.buttonScript>(scene);
        if (button != null)
        {
            Undo.RecordObject(button, "sceneName");
            button.sceneName = "GameScene";
            EditorUtility.SetDirty(button);
            result += " sceneName = GameScene";
        }
        else result += " (no he trobat el botó)";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return result;
    }

    // =====================================================================
    // WAVES: cubs especials, WaveManager, WeaponSwapper i gallet dret de la pistola
    // =====================================================================
    const string PistolaPath = "Assets/Prefabs/Pistola.prefab";
    const string InputActionsPath = "Assets/AutoHand/Examples/Scenes/MetaXR/Auto Hand.inputactions";

    [MenuItem("Tools/VR Project/Configurar Waves")]
    public static void RunWaves()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previousScene = SceneManager.GetActiveScene().path;

        string log = "";
        GameObject[] specials =
        {
            CreateSpecialCube("CubLent",  SpecialCube.Effect.SlowMotion,  0.5f, new Color(0.2f, 0.6f, 1f), ref log),
            CreateSpecialCube("CubRapid", SpecialCube.Effect.FastForward, 1.6f, new Color(1f, 0.5f, 0f), ref log),
            CreateSpecialCube("CubCanvi", SpecialCube.Effect.SwapWeapons, 1f,   new Color(0.8f, 0.2f, 1f), ref log),
        };
        log += SetupPistolRightTrigger() + "\n";
        log += SetupGameSceneWaves(specials) + "\n";

        if (!string.IsNullOrEmpty(previousScene))
            EditorSceneManager.OpenScene(previousScene);

        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectSetup Waves]\n" + log);
        EditorUtility.DisplayDialog("Waves configurades", log, "D'acord");
    }

    static GameObject CreateSpecialCube(string name, SpecialCube.Effect effect, float mult, Color color, ref string log)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            if (!AssetDatabase.CopyAsset(CubPistolaPath, path))
            {
                log += "✗ No he pogut crear " + name + "\n";
                return null;
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = name;
            SpecialCube sc = root.GetComponent<SpecialCube>();
            if (sc == null) sc = root.AddComponent<SpecialCube>();
            sc.effect = effect;
            sc.speedMultiplier = mult;
            sc.tint = color;
            sc.duration = 5f;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        log += "✓ Prefab " + name + " (" + effect + ")\n";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    static string SetupPistolRightTrigger()
    {
        UnityEngine.InputSystem.InputActionReference right = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
            .OfType<UnityEngine.InputSystem.InputActionReference>()
            .FirstOrDefault(r => r.name.EndsWith("Trigger Pressed (R)"));
        if (right == null) return "✗ No trobo l'acció 'Trigger Pressed (R)' a " + InputActionsPath;

        GameObject root = PrefabUtility.LoadPrefabContents(PistolaPath);
        try
        {
            pistolaScript p = root.GetComponentInChildren<pistolaScript>(true);
            if (p == null) return "✗ El prefab Pistola no té pistolaScript";
            p.rightTriggerAction = right;
            PrefabUtility.SaveAsPrefabAsset(root, PistolaPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return "✓ Pistola: gallet dret assignat";
    }

    static string SetupGameSceneWaves(GameObject[] specials)
    {
        Scene scene;
        if (!OpenSceneByName("GameScene", out scene)) return "✗ No trobo la GameScene";
        string result = "✓ GameScene:";

        GameObject managers = FindInScene(scene, "[MANAGERS]");
        if (managers == null)
        {
            managers = new GameObject("[MANAGERS]");
            SceneManager.MoveGameObjectToScene(managers, scene);
        }

        blockGeneratorScript gen = FindComponent<blockGeneratorScript>(scene);
        if (gen != null)
        {
            gen.specialPrefabs = specials.Where(s => s != null).ToArray();
            EditorUtility.SetDirty(gen);
            result += " cubs especials al generador,";
        }
        else result += " (no trobo el generador),";

        WaveManager wm = managers.GetComponent<WaveManager>();
        if (wm == null) wm = managers.AddComponent<WaveManager>();
        wm.generator = gen;
        EditorUtility.SetDirty(wm);
        result += " WaveManager,";

        if (managers.GetComponent<WeaponSwapper>() == null) managers.AddComponent<WeaponSwapper>();
        result += " WeaponSwapper,";

        ScoreManager sm = FindComponent<ScoreManager>(scene);
        if (sm != null)
        {
            sm.waveManager = wm;
            if (sm.cubeGenerator == null) sm.cubeGenerator = gen;

            // Text per al nom de la wave i el compte enrere entre waves (còpia de Text_Time)
            if (sm.textNextRound == null)
            {
                GameObject template = FindInScene(scene, "Text_Time");
                if (template != null)
                {
                    GameObject waveText = Object.Instantiate(template, template.transform.parent);
                    waveText.name = "Text_Wave";
                    RectTransform rt = waveText.GetComponent<RectTransform>();
                    RectTransform trt = template.GetComponent<RectTransform>();
                    if (rt != null && trt != null)
                    {
                        rt.sizeDelta = new Vector2(trt.sizeDelta.x * 3f, trt.sizeDelta.y);
                        rt.anchoredPosition = trt.anchoredPosition + Vector2.up * (trt.rect.height * 1.3f);
                    }
                    TMP_Text t = waveText.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.enableAutoSizing = true;
                        t.fontSizeMax = t.fontSize * 0.6f;
                        t.fontSizeMin = 1f;
                        t.text = "Wave 1";
                        sm.textNextRound = t;
                    }
                    result += " Text_Wave creat,";
                }
            }
            EditorUtility.SetDirty(sm);
            result += " ScoreManager connectat";
        }
        else result += " (no trobo el ScoreManager)";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return result;
    }

    // ---------- Utilitats ----------
    static bool OpenSceneByName(string name, out Scene scene)
    {
        string guid = AssetDatabase.FindAssets(name + " t:Scene")
            .FirstOrDefault(g => System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)) == name);
        if (guid == null) { scene = default; return false; }
        scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
        return scene.IsValid();
    }

    static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        return null;
    }

    static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T c = root.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    static Vector3 FindPlayerPosition(Scene scene)
    {
        // Preferim la càmera amb tag MainCamera (la del cap del jugador VR)
        Camera cam = null;
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Camera c in root.GetComponentsInChildren<Camera>(true))
                if (cam == null || c.CompareTag("MainCamera")) cam = c;
        if (cam != null) return cam.transform.position;
        GameObject player = FindInScene(scene, "Auto Hand Player Container");
        return player != null ? player.transform.position : Vector3.zero;
    }
}
#endif
