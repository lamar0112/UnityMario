using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// CHAOS QUEST 3D - COMPLETE MASTER SETUP
/// Builds the entire game from scratch in English.
/// Place in: Assets/Editor/MasterSetup.cs
/// Run: ChaosQuest → ★ BUILD COMPLETE GAME (Run This!)
/// </summary>
public class MasterSetup : Editor
{
    // ============================================================
    // ASSET PATHS - matching your actual project structure
    // ============================================================
    const string YBOT_PATH       = "Assets/Player/yBot.fbx";
    const string ANIM_PATH       = "Assets/Animations/";
    const string KENNEY_NATURE   = "Assets/_Project/Art/Kenney/NatureKit/";
    const string KENNEY_PLATFORM = "Assets/_Project/Art/Kenney/PlatformerKit/";
    const string SIMPLE_NATURE   = "Assets/SimpleNaturePack/";
    const string ALLSKY          = "Assets/AllSkyFree/";
    const string LIT             = "Universal Render Pipeline/Lit";

    // ============================================================
    [MenuItem("ChaosQuest/★ BUILD COMPLETE GAME (Run This!)")]
    public static void BuildCompleteGame()
    {
        bool ok = EditorUtility.DisplayDialog("Build Complete Game",
            "This will rebuild everything in English:\n\n" +
            "✅ Import TMP Essentials\n" +
            "✅ Create folder structure\n" +
            "✅ Add Tags & Layers\n" +
            "✅ Create Animator Controllers\n" +
            "✅ Build Level 1 - Forest Dash\n" +
            "✅ Build Main Menu\n" +
            "✅ Update Build Settings\n\n" +
            "Continue?", "BUILD!", "Cancel");

        if (!ok) return;

        // Import TMP first
        TMPro.TMP_PackageUtilities.ImportProjectResourcesMenu();

        CreateFolders();
        AddTags();
        var animController = CreatePlayerAnimator();
        BuildLevel1(animController);
        BuildMainMenu();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete! 🎮",
            "✅ Level 1 - Forest Dash built\n" +
            "✅ Main Menu built\n" +
            "✅ Animator Controllers ready\n\n" +
            "Open Level_ForestDash scene\n" +
            "and press PLAY!\n\n" +
            "Controls:\n" +
            "WASD = Move\n" +
            "Mouse = Look\n" +
            "Space = Jump\n" +
            "Escape = Pause",
            "Let's go!");
    }

    // ============================================================
    // FOLDERS
    // ============================================================
    [MenuItem("ChaosQuest/1. Create Folders")]
    public static void CreateFolders()
    {
        string[] folders = {
            "Assets/_Project/Scenes",
            "Assets/_Project/Animators",
            "Assets/_Project/Materials",
            "Assets/_Project/Prefabs/Player",
            "Assets/_Project/Prefabs/Enemies",
            "Assets/_Project/Prefabs/Collectibles",
            "Assets/_Project/Prefabs/Level",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Terrain",
            "Assets/_Project/Documentation",
        };

        foreach (string path in folders)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // Asset credits file
        string creditsPath = "Assets/_Project/Documentation/AssetCredits.md";
        if (!File.Exists(creditsPath))
            File.WriteAllText(creditsPath,
                "# Asset Credits\n\n" +
                "## YBot Character\nSource: Mixamo (Adobe)\nLicense: Free for use\n\n" +
                "## Animations (Walk, Run, Jump, Fall, Death)\nSource: Mixamo (Adobe)\nLicense: Free for use\n\n" +
                "## Kenney Platformer Kit\nSource: kenney.nl\nLicense: CC0\n\n" +
                "## Kenney Nature Kit\nSource: kenney.nl\nLicense: CC0\n\n" +
                "## Simple Nature Pack\nSource: Unity Asset Store\nLicense: Unity Store EULA\n\n" +
                "## AllSky Free\nSource: Unity Asset Store\nLicense: Unity Store EULA\n");

        AssetDatabase.Refresh();
        Debug.Log("ChaosQuest: ✅ Folders created");
    }

    // ============================================================
    // TAGS
    // ============================================================
    [MenuItem("ChaosQuest/2. Add Tags")]
    public static void AddTags()
    {
        string[] tags = { "Player", "Enemy", "Collectible", "Checkpoint" };
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");

        foreach (string tag in tags)
        {
            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                { found = true; break; }
            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();
        Debug.Log("ChaosQuest: ✅ Tags added");
    }

    // ============================================================
    // ANIMATOR CONTROLLERS
    // PENSUM: Animator system (Lecture 9) - REQUIRED for exam!
    // ============================================================
    [MenuItem("ChaosQuest/3. Create Animator Controllers")]
    public static AnimatorController CreatePlayerAnimator()
    {
        // Find Mixamo clips from Assets/Animations/
        var clips = FindMixamoClips();

        // Player Animator
        string path = "Assets/_Project/Animators/PlayerAnimator.controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Parameters - PENSUM: Animator parameters (Lecture 9)
        controller.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded",  AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump",        AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die",         AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // States - PENSUM: FSM states via Animator (Lecture 5+9)
        var idle    = AddState(sm, "Idle",    GetClip(clips, "idle"));
        var walk    = AddState(sm, "Walk",    GetClip(clips, "walk"));
        var run     = AddState(sm, "Run",     GetClip(clips, "run"));
        var jump    = AddState(sm, "Jump",    GetClip(clips, "jump"));
        var fall    = AddState(sm, "Fall",    GetClip(clips, "fall"));
        var die     = AddState(sm, "Die",     GetClip(clips, "die"));

        sm.defaultState = idle;

        // Transitions - PENSUM: State transitions (Lecture 9)
        AddFloatTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.1f);
        AddFloatTransition(walk, run,  "Speed", AnimatorConditionMode.Greater, 4f);
        AddFloatTransition(run,  walk, "Speed", AnimatorConditionMode.Less,    4f);
        AddFloatTransition(walk, idle, "Speed", AnimatorConditionMode.Less,    0.1f);
        AddFloatTransition(run,  idle, "Speed", AnimatorConditionMode.Less,    0.1f);
        AddTriggerTransition(idle, jump, "Jump");
        AddTriggerTransition(walk, jump, "Jump");
        AddTriggerTransition(run,  jump, "Jump");

        var t1 = jump.AddTransition(fall);
        t1.hasExitTime = true; t1.exitTime = 0.6f; t1.duration = 0.1f;

        var t2 = fall.AddTransition(idle);
        t2.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        t2.hasExitTime = false; t2.duration = 0.1f;

        AddTriggerTransition(idle, die, "Die");
        AddTriggerTransition(walk, die, "Die");
        AddTriggerTransition(run,  die, "Die");

        // Enemy Animator
        CreateEnemyAnimator(clips);

        AssetDatabase.SaveAssets();
        Debug.Log($"ChaosQuest: ✅ Animators created with {clips.Count} clips");
        return controller;
    }

    static void CreateEnemyAnimator(Dictionary<string, AnimationClip> clips)
    {
        string path = "Assets/_Project/Animators/EnemyAnimator.controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);

        ctrl.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Hit",       AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Die",       AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle   = AddState(sm, "Idle",    GetClip(clips, "idle"));
        var patrol = AddState(sm, "Patrol",  GetClip(clips, "walk"));
        var chase  = AddState(sm, "Chase",   GetClip(clips, "run"));
        var hit    = AddState(sm, "Hit",     null);
        var dead   = AddState(sm, "Dead",    GetClip(clips, "die"));

        sm.defaultState = idle;

        AddBoolTransition(idle,   patrol, "IsWalking", true);
        AddBoolTransition(patrol, idle,   "IsWalking", false);
        AddBoolTransition(idle,   chase,  "IsChasing", true);
        AddBoolTransition(patrol, chase,  "IsChasing", true);
        AddBoolTransition(chase,  patrol, "IsChasing", false);
        AddTriggerTransition(idle,   hit,  "Hit");
        AddTriggerTransition(patrol, hit,  "Hit");
        AddTriggerTransition(chase,  hit,  "Hit");

        var t = hit.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;

        AddTriggerTransition(idle,   dead, "Die");
        AddTriggerTransition(patrol, dead, "Die");
        AddTriggerTransition(chase,  dead, "Die");
    }

    // ============================================================
    // BUILD LEVEL 1 - FOREST DASH
    // ============================================================
    [MenuItem("ChaosQuest/4. Build Level 1 - Forest Dash")]
    public static void BuildLevel1() => BuildLevel1(null);

    static void BuildLevel1(AnimatorController playerAnimator)
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupLighting();
        SetupSkybox();
        BuildTerrain();
        BuildWater();
        PlaceNatureDecoration();
        PlacePlatforms();
        PlacePlayer(playerAnimator);
        PlaceManagers();
        PlaceCollectibles();
        PlaceEnemies();
        PlaceCheckpoints();
        PlaceFinishPortal();
        BuildHUD();
        BuildPauseMenu();
        BuildLevelCompleteScreen();
        SetupCamera();

        string scenePath = "Assets/_Project/Scenes/Level_ForestDash.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"ChaosQuest: ✅ Level_ForestDash built");
    }

    // ============================================================
    // LIGHTING - PENSUM: Lights (Chapter 5)
    // ============================================================
    static void SetupLighting()
    {
        var sunGO = new GameObject("Sun");
        var sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.3f;
        sun.color = new Color(1f, 0.95f, 0.82f);
        sun.shadows = LightShadows.Soft;
        sunGO.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

        var fillGO = new GameObject("FillLight");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.25f;
        fill.color = new Color(0.5f, 0.6f, 1f);
        fillGO.transform.rotation = Quaternion.Euler(25f, 155f, 0f);

        // PENSUM: RenderSettings (Chapter 4)
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.65f, 0.82f, 0.92f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 130f;
        RenderSettings.ambientSkyColor    = new Color(0.45f, 0.65f, 0.95f);
        RenderSettings.ambientEquatorColor = new Color(0.35f, 0.55f, 0.35f);
        RenderSettings.ambientGroundColor  = new Color(0.2f, 0.28f, 0.2f);
    }

    // ============================================================
    // SKYBOX - uses AllSky Free if available
    // ============================================================
    static void SetupSkybox()
    {
        // Try AllSky skybox materials
        string[] skyboxPaths = {
            "Assets/AllSkyFree/Materials/AllSky_Generic_05.mat",
            "Assets/AllSkyFree/Materials/AllSky_Space_AnotherPlanet.mat",
            "Assets/AllSkyFree/Materials/AllSky_Sunny01_TopDown.mat",
            "Assets/AllSkyFree/Materials/AllSky_Overcast4_Low.mat",
        };

        foreach (var p in skyboxPaths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat != null)
            {
                RenderSettings.skybox = mat;
                Debug.Log($"ChaosQuest: ✅ Skybox set: {p}");
                return;
            }
        }
        Debug.LogWarning("ChaosQuest: No AllSky skybox found — using default");
    }

    // ============================================================
    // TERRAIN - PENSUM: Terrain (Chapter 4)
    // ============================================================
    static void BuildTerrain()
    {
        var data = new TerrainData();
        data.heightmapResolution = 257;
        data.size = new Vector3(200, 28, 200);

        int res = data.heightmapResolution;
        float[,] heights = new float[res, res];
        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                float nx = (float)x / res;
                float nz = (float)z / res;

                float h = Mathf.PerlinNoise(nx * 3f + 7f, nz * 3f + 7f) * 0.38f
                        + Mathf.PerlinNoise(nx * 7f + 2f, nz * 7f + 2f) * 0.08f
                        + Mathf.PerlinNoise(nx * 1.5f, nz * 1.5f) * 0.25f;

                // Flat path down the middle
                float distFromCenter = Mathf.Abs(nx - 0.5f);
                float pathInfluence = Mathf.Clamp01(1f - distFromCenter * 3.5f);
                h = Mathf.Lerp(h, 0.04f, pathInfluence * 0.75f);

                // Fade edges
                float edgeFade = Mathf.Min(
                    Mathf.Clamp01(nx * 6f), Mathf.Clamp01((1f - nx) * 6f),
                    Mathf.Clamp01(nz * 6f), Mathf.Clamp01((1f - nz) * 6f));
                heights[z, x] = h * edgeFade;
            }
        }
        data.SetHeights(0, 0, heights);

        // Terrain layers - PENSUM: Terrain textures (Chapter 4)
        var grass = MakeTerrainLayer("Grass", new Color(0.28f, 0.58f, 0.22f));
        var dirt  = MakeTerrainLayer("Dirt",  new Color(0.52f, 0.36f, 0.2f));
        var rock  = MakeTerrainLayer("Rock",  new Color(0.48f, 0.48f, 0.48f));
        data.terrainLayers = new TerrainLayer[] { grass, dirt, rock };

        // Paint textures by slope
        float[,,] alpha = new float[data.alphamapWidth, data.alphamapHeight, 3];
        for (int x = 0; x < data.alphamapWidth; x++)
        {
            for (int z = 0; z < data.alphamapHeight; z++)
            {
                float nx = (float)x / data.alphamapWidth;
                float nz = (float)z / data.alphamapHeight;
                Vector3 normal = data.GetInterpolatedNormal(nx, nz);
                float slope = 1f - normal.y;
                float height = data.GetInterpolatedHeight(nx, nz) / data.size.y;

                alpha[x, z, 0] = Mathf.Clamp01(1f - slope * 1.8f);  // grass
                alpha[x, z, 1] = Mathf.Clamp01(slope * 1.5f);        // dirt
                alpha[x, z, 2] = Mathf.Clamp01(height - 0.55f) * 4f; // rock on peaks
            }
        }
        data.SetAlphamaps(0, 0, alpha);

        Directory.CreateDirectory("Assets/_Project/Terrain");
        AssetDatabase.CreateAsset(data, "Assets/_Project/Terrain/ForestDash_Data.asset");

        var terrGO = Terrain.CreateTerrainGameObject(data);
        terrGO.name = "Terrain";
        terrGO.transform.position = new Vector3(-100, -2, -100);
        terrGO.GetComponent<Terrain>().drawInstanced = true;
    }

    static TerrainLayer MakeTerrainLayer(string name, Color color)
    {
        var layer = new TerrainLayer();
        var tex = new Texture2D(64, 64);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++)
        {
            float v = Random.Range(-0.04f, 0.04f);
            pixels[i] = new Color(color.r + v, color.g + v, color.b + v);
        }
        tex.SetPixels(pixels);
        tex.Apply();

        string texPath = $"Assets/_Project/Terrain/{name}Tex.png";
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);
        layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        layer.tileSize = new Vector2(8, 8);

        string layerPath = $"Assets/_Project/Terrain/{name}Layer.terrainlayer";
        AssetDatabase.CreateAsset(layer, layerPath);
        return AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
    }

    // ============================================================
    // WATER
    // ============================================================
    static void BuildWater()
    {
        // Try Simple Water Shader first
        string[] waterMatPaths = {
            "Assets/IgniteCoders/SimpleWaterShaderURP/Materials/Water.mat",
            "Assets/IgniteCoders/SimpleWaterShaderURP/Materials/SimpleWater.mat",
        };

        Material waterMat = null;
        foreach (var p in waterMatPaths)
        {
            waterMat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (waterMat != null) break;
        }

        if (waterMat == null)
        {
            waterMat = MakeMaterial("WaterMat", new Color(0.08f, 0.38f, 0.78f, 0.75f));
            waterMat.SetFloat("_Metallic", 0.8f);
            waterMat.SetFloat("_Smoothness", 0.95f);
        }

        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.position = new Vector3(0, -1.8f, 25);
        water.transform.localScale = new Vector3(12, 1, 12);
        water.GetComponent<Renderer>().sharedMaterial = waterMat;
        // Water damages player on contact - PENSUM: Trigger (Chapter 6)
        var col = water.GetComponent<Collider>();
        col.isTrigger = false; // Use box collider instead
        var bc = water.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1, 2, 1);
        water.AddComponent<Hazard>();
    }

    // ============================================================
    // NATURE DECORATION
    // ============================================================
    static void PlaceNatureDecoration()
    {
        var treeParent = new GameObject("Trees");
        var rockParent = new GameObject("Rocks");
        var bushParent = new GameObject("Bushes");

        // Try to find Kenney or SimpleNaturePack trees
        string[] treePaths = FindAssetPaths("t:GameObject", new[]{ "tree", "Tree", "pine", "Pine", "oak" },
            new[]{ KENNEY_NATURE, SIMPLE_NATURE });

        string[] rockPaths = FindAssetPaths("t:GameObject", new[]{ "rock", "Rock", "stone", "Stone" },
            new[]{ KENNEY_NATURE, SIMPLE_NATURE });

        // Place 35 trees around the level
        for (int i = 0; i < 35; i++)
        {
            float x = Random.Range(-45f, 45f);
            float z = Random.Range(-15f, 80f);
            if (Mathf.Abs(x) < 7f) x = x > 0 ? 9f : -9f;

            var pos = new Vector3(x, GetTerrainY(x, z), z);
            GameObject tree = treePaths.Length > 0
                ? InstantiateAsset(treePaths[Random.Range(0, treePaths.Length)])
                : MakePrimTree(pos);

            if (tree != null)
            {
                tree.name = $"Tree_{i}";
                tree.transform.position = pos;
                tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                tree.transform.localScale = Vector3.one * Random.Range(0.7f, 1.4f);
                tree.transform.SetParent(treeParent.transform);
            }
        }

        // Place 20 rocks
        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(-40f, 40f);
            float z = Random.Range(-12f, 75f);
            if (Mathf.Abs(x) < 5f) x = x > 0 ? 7f : -7f;

            var pos = new Vector3(x, GetTerrainY(x, z), z);
            GameObject rock = rockPaths.Length > 0
                ? InstantiateAsset(rockPaths[Random.Range(0, rockPaths.Length)])
                : MakePrimRock(pos);

            if (rock != null)
            {
                rock.name = $"Rock_{i}";
                rock.transform.position = pos;
                rock.transform.rotation = Quaternion.Euler(
                    Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                rock.transform.localScale = Vector3.one * Random.Range(0.3f, 1.1f);
                rock.transform.SetParent(rockParent.transform);
            }
        }

        // Place bushes as primitives
        for (int i = 0; i < 28; i++)
        {
            float x = Random.Range(-35f, 35f);
            float z = Random.Range(-10f, 72f);
            if (Mathf.Abs(x) < 4f) x = x > 0 ? 6f : -6f;
            var pos = new Vector3(x, GetTerrainY(x, z) + 0.3f, z);
            var bush = MakePrimBush(pos);
            bush.name = $"Bush_{i}";
            bush.transform.SetParent(bushParent.transform);
        }
    }

    // ============================================================
    // PLATFORMS - PENSUM: Colliders (Chapter 6)
    // ============================================================
    static void PlacePlatforms()
    {
        var parent = new GameObject("Platforms");
        var platMat = MakeMaterial("PlatformMat", new Color(0.52f, 0.32f, 0.82f));
        var movingMat = MakeMaterial("MovingPlatMat", new Color(0.22f, 0.68f, 0.28f));
        var fallingMat = MakeMaterial("FallingPlatMat", new Color(0.88f, 0.38f, 0.18f));
        var goalMat = MakeMaterial("GoalPlatMat", new Color(0.88f, 0.78f, 0.08f));

        // Platform layout: position, size, type
        var platforms = new (Vector3 pos, Vector3 size, string type)[]
        {
            (new Vector3(0,    0.2f,  0),  new Vector3(8, 0.4f, 5),  "start"),
            (new Vector3(0,    1f,    7),  new Vector3(4, 0.4f, 3),  "normal"),
            (new Vector3(3,    2f,   13),  new Vector3(3, 0.4f, 3),  "normal"),
            (new Vector3(-2,   3f,   19),  new Vector3(3, 0.4f, 3),  "moving"),
            (new Vector3(3,    4.5f, 25),  new Vector3(3, 0.4f, 3),  "normal"),
            (new Vector3(0,    5.5f, 31),  new Vector3(4, 0.4f, 3),  "normal"),
            (new Vector3(-3,   7f,   37),  new Vector3(3, 0.4f, 3),  "falling"),
            (new Vector3(3,    8.5f, 43),  new Vector3(3, 0.4f, 3),  "normal"),
            (new Vector3(0,    9.5f, 49),  new Vector3(5, 0.4f, 3),  "normal"),
            (new Vector3(-2,  11f,   55),  new Vector3(3, 0.4f, 3),  "moving"),
            (new Vector3(3,   12.5f, 61),  new Vector3(3, 0.4f, 3),  "normal"),
            (new Vector3(0,   14f,   68),  new Vector3(6, 0.4f, 4),  "goal"),
        };

        foreach (var (pos, size, type) in platforms)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = $"Platform_{type}";
            p.transform.position = pos;
            p.transform.localScale = size;
            p.transform.SetParent(parent.transform);

            Material m = type switch {
                "moving"  => movingMat,
                "falling" => fallingMat,
                "goal"    => goalMat,
                _         => platMat,
            };
            p.GetComponent<Renderer>().sharedMaterial = m;

            if (type == "moving")
            {
                var script = p.AddComponent<MovingPlatform>();
                var wpA = new GameObject("WP_A");
                var wpB = new GameObject("WP_B");
                wpA.transform.position = pos + new Vector3(-3.5f, 0, 0);
                wpB.transform.position = pos + new Vector3(3.5f, 0, 0);
                wpA.transform.SetParent(parent.transform);
                wpB.transform.SetParent(parent.transform);
                var so = new SerializedObject(script);
                so.FindProperty("pointA").objectReferenceValue = wpA.transform;
                so.FindProperty("pointB").objectReferenceValue = wpB.transform;
                so.ApplyModifiedProperties();
            }
            else if (type == "falling")
            {
                p.AddComponent<FallingPlatform>();
            }
        }

        // Jump pads - PENSUM: Trigger (Chapter 6)
        PlaceJumpPad("JumpPad_1", new Vector3(0,  0.3f,  4));
        PlaceJumpPad("JumpPad_2", new Vector3(0,  5.6f, 31));
        PlaceJumpPad("JumpPad_3", new Vector3(0,  9.6f, 49));

        // Lava hazard zone
        var lava = GameObject.CreatePrimitive(PrimitiveType.Plane);
        lava.name = "LavaZone";
        lava.transform.position = new Vector3(0, -1f, 20);
        lava.transform.localScale = new Vector3(5, 1, 5);
        var lavaMesh = lava.GetComponent<MeshCollider>();
        if (lavaMesh != null) Object.DestroyImmediate(lavaMesh);
        var lavaBox = lava.AddComponent<BoxCollider>();
        lavaBox.isTrigger = true;
        lavaBox.size = new Vector3(1, 0.5f, 1);
        lava.AddComponent<Hazard>();
        lava.GetComponent<Renderer>().sharedMaterial =
            MakeMaterial("LavaMat", new Color(1f, 0.28f, 0.04f));
    }

    static void PlaceJumpPad(string name, Vector3 pos)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = name;
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);
        pad.GetComponent<Collider>().isTrigger = true;
        pad.AddComponent<JumpPad>();
        pad.GetComponent<Renderer>().sharedMaterial =
            MakeMaterial("JumpPadMat", new Color(1f, 0.52f, 0.04f));
    }

    // ============================================================
    // PLAYER - PENSUM: CharacterController (Chapter 5)
    // ============================================================
    static void PlacePlayer(AnimatorController animController)
    {
        GameObject playerGO;
        var yBot = AssetDatabase.LoadAssetAtPath<GameObject>(YBOT_PATH);

        if (yBot != null)
        {
            playerGO = new GameObject("Player");
            playerGO.transform.position = new Vector3(0, 1.1f, -4);

            // Add YBot as visual child
            var yBotInst = (GameObject)PrefabUtility.InstantiatePrefab(yBot);
            yBotInst.name = "YBot_Visual";
            yBotInst.transform.SetParent(playerGO.transform);
            yBotInst.transform.localPosition = new Vector3(0, -1f, 0);
            yBotInst.transform.localRotation = Quaternion.identity;
            yBotInst.transform.localScale = Vector3.one * 0.01f;
            Debug.Log("ChaosQuest: ✅ YBot added as player visual");
        }
        else
        {
            playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGO.name = "Player";
            playerGO.transform.position = new Vector3(0, 1.1f, -4);
            playerGO.GetComponent<Renderer>().sharedMaterial =
                MakeMaterial("PlayerMat", new Color(0.2f, 0.5f, 1f));
            Debug.LogWarning("ChaosQuest: YBot not found at " + YBOT_PATH);
        }

        playerGO.tag = "Player";

        // Remove default collider
        var caps = playerGO.GetComponent<CapsuleCollider>();
        if (caps != null) Object.DestroyImmediate(caps);

        // PENSUM: CharacterController (Chapter 5)
        var cc = playerGO.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;

        playerGO.AddComponent<PlayerController>();
        playerGO.AddComponent<PlayerHealth>();
        playerGO.AddComponent<PlayerRespawn>();

        // PENSUM: Animator component (Lecture 9)
        var anim = playerGO.AddComponent<Animator>();
        if (animController != null)
            anim.runtimeAnimatorController = animController;
        else
        {
            var savedCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/_Project/Animators/PlayerAnimator.controller");
            if (savedCtrl != null) anim.runtimeAnimatorController = savedCtrl;
        }
        anim.applyRootMotion = false;
    }

    // ============================================================
    // MANAGERS
    // ============================================================
    static void PlaceManagers()
    {
        AddManager("GameManager",        typeof(GameManager));
        AddManager("AudioManager",       typeof(AudioManager));
        AddManager("CheckpointManager",  typeof(CheckpointManager));
        AddManager("LevelTimer",         typeof(LevelTimer));
    }

    // ============================================================
    // COLLECTIBLES - PENSUM: Triggers + Particle System (Ch 6+16)
    // ============================================================
    static void PlaceCollectibles()
    {
        var parent = new GameObject("ChaosOrbs");
        var orbMat = MakeMaterial("OrbMat", new Color(1f, 0.85f, 0.08f));
        orbMat.SetFloat("_Metallic", 0.3f);

        Vector3[] positions = {
            new Vector3(0,  1.8f,  7),  new Vector3(3,  3f,   13),
            new Vector3(-2, 4f,   19),  new Vector3(3,  5.5f, 25),
            new Vector3(0,  6.5f, 31),  new Vector3(-3, 8f,   37),
            new Vector3(3,  9.5f, 43),  new Vector3(0, 10.5f, 49),
            new Vector3(-2, 12f,  55),  new Vector3(3, 13.5f, 61),
        };

        foreach (var pos in positions)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "ChaosOrb";
            orb.tag = "Collectible";
            orb.transform.position = pos;
            orb.transform.localScale = Vector3.one * 0.42f;
            orb.transform.SetParent(parent.transform);

            // PENSUM: Trigger collider (Chapter 6)
            orb.GetComponent<SphereCollider>().isTrigger = true;
            orb.GetComponent<Renderer>().sharedMaterial = orbMat;
            orb.AddComponent<Collectible>();

            // Glow light
            var light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.88f, 0.3f);
            light.intensity = 0.7f;
            light.range = 2.2f;
        }
    }

    // ============================================================
    // ENEMIES - PENSUM: FSM AI (Lecture 5-6)
    // ============================================================
    static void PlaceEnemies()
    {
        var parent = new GameObject("Enemies");
        var enemyMat = MakeMaterial("EnemyMat", new Color(0.78f, 0.18f, 0.78f));

        var enemyAnimCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
            "Assets/_Project/Animators/EnemyAnimator.controller");

        var enemies = new (Vector3 pos, Vector3 wpA, Vector3 wpB)[]
        {
            (new Vector3(0,  1f, 10),  new Vector3(-3.5f,1f,10),  new Vector3(3.5f,1f,10)),
            (new Vector3(0,  6f, 33),  new Vector3(-2.5f,6f,33),  new Vector3(2.5f,6f,33)),
            (new Vector3(0, 10f, 51),  new Vector3(-2.5f,10f,51), new Vector3(2.5f,10f,51)),
        };

        foreach (var (pos, wpA, wpB) in enemies)
        {
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "WobbleSlime";
            enemy.tag = "Enemy";
            enemy.transform.position = pos;
            enemy.transform.localScale = Vector3.one * 0.85f;
            enemy.transform.SetParent(parent.transform);
            enemy.GetComponent<Renderer>().sharedMaterial = enemyMat;

            // PENSUM: FSM AI (Lecture 5-6)
            var fsm = enemy.AddComponent<EnemyFSM>();

            var wp1 = new GameObject("WP_A");
            wp1.transform.position = wpA;
            wp1.transform.SetParent(parent.transform);
            var wp2 = new GameObject("WP_B");
            wp2.transform.position = wpB;
            wp2.transform.SetParent(parent.transform);

            var so = new SerializedObject(fsm);
            so.FindProperty("waypoints").arraySize = 2;
            so.FindProperty("waypoints").GetArrayElementAtIndex(0).objectReferenceValue = wp1.transform;
            so.FindProperty("waypoints").GetArrayElementAtIndex(1).objectReferenceValue = wp2.transform;
            so.ApplyModifiedProperties();

            // PENSUM: Animator (Lecture 9)
            var anim = enemy.AddComponent<Animator>();
            if (enemyAnimCtrl != null) anim.runtimeAnimatorController = enemyAnimCtrl;
            anim.applyRootMotion = false;
        }
    }

    // ============================================================
    // CHECKPOINTS - PENSUM: Triggers + Renderer (Ch 6+4)
    // ============================================================
    static void PlaceCheckpoints()
    {
        PlaceCheckpoint("Checkpoint_1", new Vector3(0, 6f, 31));
        PlaceCheckpoint("Checkpoint_2", new Vector3(0, 10f, 51));
    }

    static void PlaceCheckpoint(string name, Vector3 pos)
    {
        var cp = new GameObject(name);
        cp.transform.position = pos;

        var col = cp.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3, 4, 1);

        var script = cp.AddComponent<Checkpoint>();

        // Visual flag
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(cp.transform);
        pole.transform.localPosition = new Vector3(0, 1.5f, 0);
        pole.transform.localScale = new Vector3(0.15f, 1.5f, 0.15f);
        pole.GetComponent<Renderer>().sharedMaterial = MakeMaterial("PoleMat", Color.white);

        var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.name = "Flag";
        flag.transform.SetParent(cp.transform);
        flag.transform.localPosition = new Vector3(0.4f, 2.8f, 0);
        flag.transform.localScale = new Vector3(0.8f, 0.5f, 0.1f);
        var flagRenderer = flag.GetComponent<Renderer>();
        flagRenderer.sharedMaterial = MakeMaterial("FlagMat_Inactive", Color.gray);

        var so = new SerializedObject(script);
        so.FindProperty("flagRenderer").objectReferenceValue = flagRenderer;
        so.ApplyModifiedProperties();
    }

    // ============================================================
    // FINISH PORTAL - PENSUM: Trigger + Particle System (Ch 6+16)
    // ============================================================
    static void PlaceFinishPortal()
    {
        var portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        portal.name = "FinishPortal";
        portal.transform.position = new Vector3(0, 15f, 70);
        portal.transform.localScale = new Vector3(2.2f, 2.8f, 2.2f);
        portal.GetComponent<Collider>().isTrigger = true;

        var script = portal.AddComponent<FinishPortal>();

        // PENSUM: Material color (Chapter 4)
        var mat = MakeMaterial("PortalMat", new Color(0.05f, 0.88f, 1f));
        mat.SetFloat("_Metallic", 0.75f);
        mat.SetFloat("_Smoothness", 0.9f);
        portal.GetComponent<Renderer>().sharedMaterial = mat;

        // PENSUM: Particle System (Chapter 16)
        var ps = portal.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.05f, 0.85f, 1f, 0.8f);
        main.startSize = 0.25f;
        main.startSpeed = 2.5f;
        main.startLifetime = 1.2f;
        main.loop = true;
        main.maxParticles = 80;

        // Glow light
        var glow = portal.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.05f, 0.85f, 1f);
        glow.intensity = 2.5f;
        glow.range = 10f;
    }

    // ============================================================
    // CAMERA - PENSUM: Camera (Chapter 5)
    // ============================================================
    static void SetupCamera()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 65f;
        cam.farClipPlane = 160f;
        cam.nearClipPlane = 0.1f;
        camGO.AddComponent<AudioListener>();
        var follow = camGO.AddComponent<CameraFollow>();
        camGO.transform.position = new Vector3(0, 6, -12);
        camGO.transform.rotation = Quaternion.Euler(15, 0, 0);
        // CameraFollow finds Player by tag automatically
    }

    // ============================================================
    // UI - PENSUM: Canvas + UI (Chapter 14)
    // ============================================================
    static void BuildHUD()
    {
        var canvas = new GameObject("HUD_Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 1;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        canvas.AddComponent<HUD>();

        MakeText(canvas, "ScoreText", "Score: 0",
            new Vector2(0,1), new Vector2(0,1), new Vector2(10,-10), new Vector2(180, 36), 20);
        MakeText(canvas, "OrbsText", "Orbs: 0",
            new Vector2(0,1), new Vector2(0,1), new Vector2(10,-46), new Vector2(180, 36), 20);
        MakeText(canvas, "HealthText", "HP: 3/3",
            new Vector2(0,1), new Vector2(0,1), new Vector2(10,-82), new Vector2(180, 36), 20);
        MakeText(canvas, "TimerText", "00:00",
            new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0,-14), new Vector2(180, 42), 24);
    }

    static void BuildPauseMenu()
    {
        var canvas = new GameObject("PauseMenu_Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvas.transform, false);
        var bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0, 0, 0, 0.78f);
        StretchFull(panel);
        panel.SetActive(false);

        MakeText(panel, "PauseTitle", "PAUSED",
            new Vector2(0.5f,0.68f), new Vector2(0.5f,0.68f), Vector2.zero, new Vector2(300,70), 38);

        MakeButton(panel, "ResumeBtn",    "RESUME",     new Vector2(0,  65));
        MakeButton(panel, "RestartBtn",   "RESTART",    new Vector2(0,   0));
        MakeButton(panel, "MainMenuBtn",  "MAIN MENU",  new Vector2(0, -65));
        MakeButton(panel, "QuitBtn",      "QUIT",       new Vector2(0,-130));

        var script = canvas.AddComponent<PauseMenu>();
        var so = new SerializedObject(script);
        so.FindProperty("pausePanel").objectReferenceValue = panel;
        so.ApplyModifiedProperties();
    }

    static void BuildLevelCompleteScreen()
    {
        var canvas = new GameObject("LevelComplete_Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 20;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var panel = new GameObject("LevelCompletePanel");
        panel.transform.SetParent(canvas.transform, false);
        var bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.08f, 0.15f, 0.9f);
        StretchFull(panel);
        panel.SetActive(false);

        MakeText(panel, "TitleText",  "LEVEL COMPLETE!",
            new Vector2(0.5f,0.72f), new Vector2(0.5f,0.72f), Vector2.zero, new Vector2(450,70), 36);
        MakeText(panel, "ScoreText",  "Score: 0",
            new Vector2(0.5f,0.58f), new Vector2(0.5f,0.58f), Vector2.zero, new Vector2(300,40), 24);
        MakeText(panel, "OrbsText",   "Orbs: 0",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(300,40), 24);
        MakeText(panel, "TimeText",   "Time: 00:00",
            new Vector2(0.5f,0.42f), new Vector2(0.5f,0.42f), Vector2.zero, new Vector2(300,40), 24);
        MakeButton(panel, "ContinueBtn", "CONTINUE", new Vector2(0,-80));
        MakeButton(panel, "MenuBtn",     "MAIN MENU", new Vector2(0,-150));

        var script = canvas.AddComponent<LevelCompleteUI>();
        var so = new SerializedObject(script);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.ApplyModifiedProperties();
    }

    // ============================================================
    // MAIN MENU SCENE - PENSUM: SceneManagement (Chapter 23)
    // ============================================================
    [MenuItem("ChaosQuest/5. Build Main Menu Scene")]
    public static void BuildMainMenu()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGO = new GameObject("DirectionalLight");
        lightGO.AddComponent<Light>().type = LightType.Directional;

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camGO.GetComponent<Camera>().backgroundColor = new Color(0.05f, 0.08f, 0.18f);
        camGO.AddComponent<AudioListener>();

        AddManager("GameManager",  typeof(GameManager));
        AddManager("AudioManager", typeof(AudioManager));

        var canvas = new GameObject("MainMenu_Canvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvas.transform, false);
        var bgImg = bg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.04f, 0.06f, 0.16f);
        StretchFull(bg);

        MakeText(canvas, "TitleText", "CHAOS QUEST 3D",
            new Vector2(0.5f,0.75f), new Vector2(0.5f,0.75f), Vector2.zero, new Vector2(650,110), 58);
        MakeText(canvas, "SubText", "A 3D Platformer Adventure",
            new Vector2(0.5f,0.63f), new Vector2(0.5f,0.63f), Vector2.zero, new Vector2(500,45), 22);

        MakeButton(canvas, "StartBtn",    "START GAME",  new Vector2(0,  50));
        MakeButton(canvas, "ControlsBtn", "CONTROLS",    new Vector2(0, -20));
        MakeButton(canvas, "QuitBtn",     "QUIT",        new Vector2(0, -90));

        var menuScript = canvas.AddComponent<MainMenu>();

        // Controls panel
        var ctrlPanel = new GameObject("ControlsPanel");
        ctrlPanel.transform.SetParent(canvas.transform, false);
        var ctrlBg = ctrlPanel.AddComponent<UnityEngine.UI.Image>();
        ctrlBg.color = new Color(0, 0, 0, 0.85f);
        StretchFull(ctrlPanel);

        MakeText(ctrlPanel, "ControlsTitle", "CONTROLS",
            new Vector2(0.5f,0.75f), new Vector2(0.5f,0.75f), Vector2.zero, new Vector2(400,60), 32);
        MakeText(ctrlPanel, "ControlsList",
            "WASD / Arrow Keys  —  Move\nMouse  —  Look Around\nSpace  —  Jump\nLeft Shift  —  Sprint\nEscape  —  Pause",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(500,200), 20);
        MakeButton(ctrlPanel, "BackBtn", "BACK", new Vector2(0,-130));
        ctrlPanel.SetActive(false);

        var menuSo = new SerializedObject(menuScript);
        menuSo.FindProperty("mainPanel").objectReferenceValue = canvas.transform.Find("StartBtn")?.gameObject.transform.parent?.gameObject;
        menuSo.FindProperty("controlsPanel").objectReferenceValue = ctrlPanel;
        menuSo.ApplyModifiedProperties();

        string path = "Assets/_Project/Scenes/MainMenu.unity";
        EditorSceneManager.SaveScene(scene, path);

        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level_ForestDash.unity");
        Debug.Log("ChaosQuest: ✅ Main Menu built");
    }

    // ============================================================
    // BUILD SETTINGS
    // ============================================================
    [MenuItem("ChaosQuest/6. Update Build Settings")]
    public static void UpdateBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Level_ForestDash.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("ChaosQuest: ✅ Build Settings updated");
    }

    // ============================================================
    // GIT PUSH GUIDE
    // ============================================================
    [MenuItem("ChaosQuest/★ Show Git Push Instructions")]
    public static void ShowGitInstructions()
    {
        EditorUtility.DisplayDialog("Git Push",
            "Open Terminal (Cmd+Space → Terminal)\n\n" +
            "Navigate to your project:\n" +
            "cd ~/Desktop/MarioEksamen/Chaos\\ Quest\\ 3D\n\n" +
            "Stage all changes:\n" +
            "git add .\n\n" +
            "Commit:\n" +
            "git commit -m \"Complete game setup with YBot and Level 1\"\n\n" +
            "Push:\n" +
            "git push\n\n" +
            "First time? Run these first:\n" +
            "git init\n" +
            "git remote add origin YOUR_GITHUB_URL\n" +
            "git branch -M main\n" +
            "git push -u origin main",
            "Got it!");
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    static Dictionary<string, AnimationClip> FindMixamoClips()
    {
        var clips = new Dictionary<string, AnimationClip>();
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (!(asset is AnimationClip clip)) continue;
                if (clip.name.StartsWith("__preview__")) continue;
                string n = clip.name.ToLower();

                if ((n.Contains("idle") || n.Contains("standing")) && !clips.ContainsKey("idle"))
                    clips["idle"] = clip;
                else if (n.Contains("walk") && !clips.ContainsKey("walk"))
                    clips["walk"] = clip;
                else if (n.Contains("run") && !clips.ContainsKey("run"))
                    clips["run"] = clip;
                else if ((n.Contains("jump") || n.Contains("jumping")) && !clips.ContainsKey("jump"))
                    clips["jump"] = clip;
                else if ((n.Contains("fall") || n.Contains("falling")) && !clips.ContainsKey("fall"))
                    clips["fall"] = clip;
                else if ((n.Contains("death") || n.Contains("die") || n.Contains("dying")) && !clips.ContainsKey("die"))
                    clips["die"] = clip;
            }
        }
        return clips;
    }

    static AnimationClip GetClip(Dictionary<string, AnimationClip> clips, string key)
    {
        AnimationClip clip;
        clips.TryGetValue(key, out clip);
        return clip;
    }

    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        var state = sm.AddState(name);
        if (clip != null) { state.motion = clip; Debug.Log($"  State '{name}' → {clip.name}"); }
        else Debug.LogWarning($"  State '{name}' → NO CLIP");
        return state;
    }

    static void AddFloatTransition(AnimatorState from, AnimatorState to, string param,
        AnimatorConditionMode mode, float val)
    {
        var t = from.AddTransition(to);
        t.AddCondition(mode, val, param);
        t.hasExitTime = false; t.duration = 0.12f;
    }

    static void AddBoolTransition(AnimatorState from, AnimatorState to, string param, bool val)
    {
        var t = from.AddTransition(to);
        t.AddCondition(val ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        t.hasExitTime = false; t.duration = 0.12f;
    }

    static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false; t.duration = 0.08f;
    }

    static GameObject AddManager(string name, System.Type type)
    {
        var go = new GameObject(name);
        go.AddComponent(type);
        return go;
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find(LIT) ?? Shader.Find("Standard"));
        mat.color = color;
        Directory.CreateDirectory("Assets/_Project/Materials");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static float GetTerrainY(float x, float z)
    {
        return Terrain.activeTerrain != null
            ? Terrain.activeTerrain.SampleHeight(new Vector3(x, 0, z)) - 2f
            : 0f;
    }

    static string[] FindAssetPaths(string filter, string[] keywords, string[] searchFolders)
    {
        var results = new List<string>();
        foreach (var folder in searchFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLower();
                foreach (var kw in keywords)
                    if (lower.Contains(kw.ToLower()) && !results.Contains(path))
                        results.Add(path);
            }
        }
        return results.ToArray();
    }

    static GameObject InstantiateAsset(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
    }

    static GameObject MakePrimTree(Vector3 pos)
    {
        var t = new GameObject("Tree");
        t.transform.position = pos;
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(t.transform);
        trunk.transform.localPosition = new Vector3(0, 1.1f, 0);
        trunk.transform.localScale = new Vector3(0.28f, 1.1f, 0.28f);
        trunk.GetComponent<Renderer>().sharedMaterial = MakeMaterial("TrunkMat", new Color(0.38f, 0.22f, 0.08f));
        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.transform.SetParent(t.transform);
        crown.transform.localPosition = new Vector3(0, 2.8f, 0);
        crown.transform.localScale = new Vector3(1.7f, 1.9f, 1.7f);
        crown.GetComponent<Renderer>().sharedMaterial = MakeMaterial("CrownMat",
            new Color(0.12f + Random.Range(0,0.08f), 0.52f + Random.Range(0,0.12f), 0.12f));
        return t;
    }

    static GameObject MakePrimRock(Vector3 pos)
    {
        var r = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        r.transform.position = pos;
        r.transform.localScale = new Vector3(
            Random.Range(0.4f, 1.4f), Random.Range(0.28f, 0.75f), Random.Range(0.4f, 1.4f));
        r.GetComponent<Renderer>().sharedMaterial = MakeMaterial("RockMat", new Color(0.44f, 0.44f, 0.44f));
        return r;
    }

    static GameObject MakePrimBush(Vector3 pos)
    {
        var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        b.transform.position = pos;
        b.transform.localScale = new Vector3(
            Random.Range(0.55f, 1.1f), Random.Range(0.38f, 0.72f), Random.Range(0.55f, 1.1f));
        b.GetComponent<Renderer>().sharedMaterial = MakeMaterial("BushMat",
            new Color(0.12f, 0.42f + Random.Range(0,0.1f), 0.08f));
        return b;
    }

    static GameObject MakeText(GameObject parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = anchorMin; r.anchorMax = anchorMax;
        r.anchoredPosition = pos; r.sizeDelta = size;
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.14f, 0.48f, 0.88f);
        go.AddComponent<UnityEngine.UI.Button>();
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(260, 55);
        r.anchoredPosition = pos;

        var txt = new GameObject("Label");
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 22;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        var tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        return go;
    }

    static void StretchFull(GameObject go)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

// ============================================================
// MOVING PLATFORM - PENSUM: Transform, Coroutine (Ch 6+8)
// ============================================================
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2.2f;
    [SerializeField] private float waitTime = 0.6f;
    private Vector3 target;
    private bool waiting;

    private void Start()
    {
        if (pointA && pointB) target = pointB.position;
        else enabled = false;
    }

    private void Update()
    {
        if (waiting) return;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f)
            StartCoroutine(Switch());
    }

    private System.Collections.IEnumerator Switch()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        target = (target == pointA.position) ? pointB.position : pointA.position;
        waiting = false;
    }

    private void OnTriggerEnter(Collider other)
    { if (other.CompareTag("Player")) other.transform.SetParent(transform); }

    private void OnTriggerExit(Collider other)
    { if (other.CompareTag("Player")) other.transform.SetParent(null); }
}

// ============================================================
// FALLING PLATFORM - PENSUM: Rigidbody, Coroutine (Ch 6+8)
// ============================================================
public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallDelay = 0.75f;
    [SerializeField] private float respawnTime = 4f;
    private Rigidbody rb;
    private Vector3 startPos;
    private Quaternion startRot;
    private bool falling;

    private void Awake()
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        startPos = transform.position;
        startRot = transform.rotation;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (!col.gameObject.CompareTag("Player") || falling) return;
        StartCoroutine(Fall());
    }

    private System.Collections.IEnumerator Fall()
    {
        falling = true;
        yield return new WaitForSeconds(fallDelay);
        rb.isKinematic = false;
        yield return new WaitForSeconds(respawnTime);
        rb.isKinematic = true;
        rb.linearVelocity = rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(startPos, startRot);
        falling = false;
    }
}

// ============================================================
// JUMP PAD - PENSUM: Trigger (Chapter 6)
// ============================================================
public class JumpPad : MonoBehaviour
{
    [SerializeField] private float force = 18f;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        other.GetComponent<PlayerController>()?.ApplyJumpPadForce(force);
        AudioManager.Instance?.PlayJump();
    }
}
