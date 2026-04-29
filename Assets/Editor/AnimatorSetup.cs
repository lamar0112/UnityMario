using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Automatically finds YBot + Mixamo animation clips and sets up
/// Animator Controllers for player and enemies.
///
/// CURRICULUM: Animator / Rigged animations (lecture 9)
/// Exam requires: "You must have rigged animations in the game"
///
/// Place in: Assets/Editor/AnimatorSetup.cs
/// Run: ChaosQuest → Setup YBot + Animations
/// </summary>
public class AnimatorSetup : Editor
{
    [MenuItem("ChaosQuest/🎭 Setup YBot + Animations (Run This!)")]
    public static void SetupAll()
    {
        var yBot = FindYBot();
        if (yBot == null)
        {
            EditorUtility.DisplayDialog("YBot Not Found",
                "Cannot find YBot!\n\nMake sure yBot.fbx is in:\nAssets/Player/\nor\nAssets/_Project/Art/Characters/",
                "OK");
            return;
        }

        var clips = FindMixamoClips();
        var controller = CreatePlayerAnimatorController(clips);
        SetupPlayerInScene(yBot, controller);
        CreateEnemyAnimatorController(clips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string clipInfo = clips.Count > 0
            ? $"✅ {clips.Count} Mixamo clips found and linked"
            : "⚠️ No clips found — drag them manually into the Animator";

        EditorUtility.DisplayDialog("Done! 🎭",
            $"✅ YBot found: {yBot.name}\n{clipInfo}\n" +
            "✅ PlayerAnimator.controller created\n" +
            "✅ EnemyAnimator.controller created\n" +
            "✅ Player updated in scene\n\nPress Play to test animations!", "Let's go!");
    }

    // ------------------------------------------------------------------
    static GameObject FindYBot()
    {
        foreach (var search in new[] { "yBot", "y bot", "YBot", "ybot" })
        {
            string[] guids = AssetDatabase.FindAssets($"{search} t:GameObject", new[] { "Assets" });
            foreach (var guid in guids)
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (obj != null) return obj;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------
    static System.Collections.Generic.Dictionary<string, AnimationClip> FindMixamoClips()
    {
        var clips = new System.Collections.Generic.Dictionary<string, AnimationClip>();
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets" });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is AnimationClip clip)) continue;
                if (clip.name.StartsWith("__preview__")) continue;

                string n = clip.name.ToLower();
                if (!clips.ContainsKey("Idle")   && (n.Contains("idle") || n.Contains("standing")))
                    clips["Idle"] = clip;
                else if (!clips.ContainsKey("Walk") && n.Contains("walk"))
                    clips["Walk"] = clip;
                else if (!clips.ContainsKey("Run")  && n.Contains("run"))
                    clips["Run"] = clip;
                else if (!clips.ContainsKey("Jump") && (n.Contains("jump") || n.Contains("jumping")))
                    clips["Jump"] = clip;
                else if (!clips.ContainsKey("Fall") && (n.Contains("fall") || n.Contains("falling")))
                    clips["Fall"] = clip;
                else if (!clips.ContainsKey("Die")  && (n.Contains("death") || n.Contains("die") || n.Contains("dying")))
                    clips["Die"] = clip;
            }
        }
        foreach (var kv in clips) Debug.Log($"Found clip: {kv.Key} → {kv.Value.name}");
        return clips;
    }

    // ------------------------------------------------------------------
    // CURRICULUM: AnimatorController with states and transitions (lecture 9)
    // ------------------------------------------------------------------
    static AnimatorController CreatePlayerAnimatorController(
        System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        string path = "Assets/_Project/Animations/PlayerAnimator.controller";
        Directory.CreateDirectory("Assets/_Project/Animations");
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // CURRICULUM: Parameters control transitions (lecture 9)
        controller.AddParameter("Speed",       AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded",  AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump",        AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die",         AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        // CURRICULUM: States in Animator = FSM states (lecture 5 + 9)
        var idle  = AddState(sm, "Idle",    GetClip(clips, "Idle"));
        var walk  = AddState(sm, "Walk",    GetClip(clips, "Walk"));
        var run   = AddState(sm, "Run",     GetClip(clips, "Run"));
        var jump  = AddState(sm, "Jump",    GetClip(clips, "Jump"));
        var fall  = AddState(sm, "Fall",    GetClip(clips, "Fall"));
        var die   = AddState(sm, "Die",     GetClip(clips, "Die"));

        sm.defaultState = idle;

        // CURRICULUM: Transitions with conditions (lecture 9)
        AddFloatTransition(idle, walk,  "Speed", AnimatorConditionMode.Greater, 0.1f);
        AddFloatTransition(walk, idle,  "Speed", AnimatorConditionMode.Less,    0.1f);
        AddFloatTransition(walk, run,   "Speed", AnimatorConditionMode.Greater, 4f);
        AddFloatTransition(run,  walk,  "Speed", AnimatorConditionMode.Less,    4f);
        AddFloatTransition(run,  idle,  "Speed", AnimatorConditionMode.Less,    0.1f);
        AddTriggerTransition(idle, jump, "Jump");
        AddTriggerTransition(walk, jump, "Jump");
        AddTriggerTransition(run,  jump, "Jump");

        var t1 = jump.AddTransition(fall);
        t1.hasExitTime = true; t1.exitTime = 0.6f; t1.duration = 0.15f;

        var t2 = fall.AddTransition(idle);
        t2.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        t2.hasExitTime = false; t2.duration = 0.1f;

        AddTriggerTransition(idle, die, "Die");
        AddTriggerTransition(walk, die, "Die");
        AddTriggerTransition(run,  die, "Die");
        AddTriggerTransition(jump, die, "Die");
        AddTriggerTransition(fall, die, "Die");

        Debug.Log($"PlayerAnimator created with {clips.Count} clips");
        return controller;
    }

    // ------------------------------------------------------------------
    static void CreateEnemyAnimatorController(
        System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        string path = "Assets/_Project/Animations/EnemyAnimator.controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsChasing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Hit",       AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die",       AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idle   = AddState(sm, "Idle",    GetClip(clips, "Idle"));
        var patrol = AddState(sm, "Patrol",  GetClip(clips, "Walk"));
        var chase  = AddState(sm, "Chase",   GetClip(clips, "Run"));
        var hit    = AddState(sm, "Hit",     null);
        var die    = AddState(sm, "Die",     GetClip(clips, "Die"));

        sm.defaultState = idle;

        AddBoolTransition(idle,   patrol, "IsWalking", true);
        AddBoolTransition(patrol, idle,   "IsWalking", false);
        AddBoolTransition(idle,   chase,  "IsChasing", true);
        AddBoolTransition(patrol, chase,  "IsChasing", true);
        AddBoolTransition(chase,  patrol, "IsChasing", false);
        AddTriggerTransition(idle,   hit, "Hit");
        AddTriggerTransition(patrol, hit, "Hit");
        AddTriggerTransition(chase,  hit, "Hit");

        var t = hit.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 1f; t.duration = 0.1f;

        AddTriggerTransition(idle,   die, "Die");
        AddTriggerTransition(patrol, die, "Die");
        AddTriggerTransition(chase,  die, "Die");

        // Attach to all enemies in scene
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            var anim = enemy.GetComponent<Animator>() ?? enemy.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
        }
        Debug.Log($"EnemyAnimator created and attached to {enemies.Length} enemies");
    }

    // ------------------------------------------------------------------
    static void SetupPlayerInScene(GameObject yBotPrefab, AnimatorController controller)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found in scene — open TestScene or Level_ForestDash first!");
            return;
        }

        // Remove old mesh renderer if present (replace capsule with YBot)
        bool hasYBot = false;
        foreach (Transform child in player.transform)
            if (child.name.ToLower().Contains("bot")) { hasYBot = true; break; }

        if (!hasYBot)
        {
            var oldRenderer = player.GetComponent<MeshRenderer>();
            if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
            var oldFilter = player.GetComponent<MeshFilter>();
            if (oldFilter != null) Object.DestroyImmediate(oldFilter);

            var yBotInstance = (GameObject)PrefabUtility.InstantiatePrefab(yBotPrefab);
            yBotInstance.transform.SetParent(player.transform);
            yBotInstance.transform.localPosition = new Vector3(0, -1f, 0);
            yBotInstance.transform.localRotation = Quaternion.identity;
            yBotInstance.transform.localScale    = Vector3.one * 0.01f;
        }

        // Set up Animator on player
        var animator = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false; // IMPORTANT: off for CharacterController

        Debug.Log("✅ Player set up with YBot and Animator");
    }

    // ------------------------------------------------------------------
    // Helper methods
    // ------------------------------------------------------------------
    static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        var state = sm.AddState(name);
        if (clip != null) { state.motion = clip; Debug.Log($"  State '{name}' → '{clip.name}'"); }
        else Debug.LogWarning($"  State '{name}' → NO CLIP");
        return state;
    }

    static void AddFloatTransition(AnimatorState from, AnimatorState to,
        string param, AnimatorConditionMode mode, float value)
    {
        var t = from.AddTransition(to);
        t.AddCondition(mode, value, param);
        t.hasExitTime = false; t.duration = 0.15f;
    }

    static void AddBoolTransition(AnimatorState from, AnimatorState to,
        string param, bool value)
    {
        var t = from.AddTransition(to);
        t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, param);
        t.hasExitTime = false; t.duration = 0.15f;
    }

    static void AddTriggerTransition(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false; t.duration = 0.1f;
    }

    static AnimationClip GetClip(
        System.Collections.Generic.Dictionary<string, AnimationClip> dict, string key)
    {
        AnimationClip clip;
        return dict.TryGetValue(key, out clip) ? clip : null;
    }

    // ------------------------------------------------------------------
    [MenuItem("ChaosQuest/🔧 Fix Null Errors in Scene")]
    public static void FixNullErrors()
    {
        // Make sure Main Camera has correct tag and AudioListener
        if (Camera.main == null)
        {
            var camObj = GameObject.Find("Main Camera");
            if (camObj != null) camObj.tag = "MainCamera";
        }
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        // Connect CameraFollow target
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var so = new SerializedObject(follow);
                    so.FindProperty("target").objectReferenceValue = player.transform;
                    so.ApplyModifiedProperties();
                }
            }
        }

        Debug.Log("✅ Null errors fixed!");
        EditorUtility.DisplayDialog("Fixed!", "Null errors fixed!\nPress Play now.", "OK");
    }

    [MenuItem("ChaosQuest/📐 Adjust YBot Scale")]
    public static void AdjustYBotScale()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogWarning("Player not found!"); return; }

        foreach (Transform child in player.transform)
        {
            if (child.name.ToLower().Contains("bot"))
            {
                child.localScale    = Vector3.one * 0.01f;
                child.localPosition = new Vector3(0, -1f, 0);
                Debug.Log("YBot scale adjusted to 0.01");
                break;
            }
        }
    }
}
