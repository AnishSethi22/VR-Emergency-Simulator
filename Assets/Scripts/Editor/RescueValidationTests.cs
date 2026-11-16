#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using EmergencyRescue.Core;
using EmergencyRescue.Fire;
using EmergencyRescue.NPC;
using EmergencyRescue.Missions;
using EmergencyRescue.Player;
using EmergencyRescue.UI;

namespace EmergencyRescue.Editor
{
    /// <summary>
    /// Automated Editor validation suite verifying scenes, prefabs, NavMesh,
    /// component wiring, and core simulation logic.
    /// </summary>
    public static class RescueValidationTests
    {
        [MenuItem("RescueSim/Run Validation Suite")]
        public static void RunAll()
        {
            int failures = 0;

            // 1. Verify Build Settings
            if (EditorBuildSettings.scenes.Length < 2)
            {
                Debug.LogError("[Validation] Expected 2 scenes in EditorBuildSettings.");
                failures++;
            }

            // 2. Verify MainMenu Scene
            var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            if (!menuScene.IsValid() || Object.FindFirstObjectByType<MainMenuUI>() == null || Object.FindFirstObjectByType<GameManager>() == null)
            {
                Debug.LogError("[Validation] MainMenu.unity missing MainMenuUI or GameManager.");
                failures++;
            }

            // 3. Verify RescueBuilding Scene
            var gameScene = EditorSceneManager.OpenScene("Assets/Scenes/RescueBuilding.unity", OpenSceneMode.Single);
            if (!gameScene.IsValid())
            {
                Debug.LogError("[Validation] RescueBuilding.unity failed to open.");
                failures++;
            }
            else
            {
                if (Object.FindFirstObjectByType<MissionManager>() == null) { Debug.LogError("[Validation] Missing MissionManager"); failures++; }
                if (Object.FindFirstObjectByType<ScoreManager>() == null) { Debug.LogError("[Validation] Missing ScoreManager"); failures++; }
                if (Object.FindFirstObjectByType<FireSpreadSystem>() == null) { Debug.LogError("[Validation] Missing FireSpreadSystem"); failures++; }
                if (Object.FindFirstObjectByType<PlayerModeManager>() == null) { Debug.LogError("[Validation] Missing PlayerModeManager"); failures++; }
                if (Object.FindFirstObjectByType<UIManager>() == null) { Debug.LogError("[Validation] Missing UIManager"); failures++; }

                var fires = Object.FindObjectsByType<FireController>(FindObjectsSortMode.None);
                if (fires.Length < 4) { Debug.LogError($"[Validation] Expected >=4 FireControllers, found {fires.Length}"); failures++; }

                var npcs = Object.FindObjectsByType<NPCRescueController>(FindObjectsSortMode.None);
                if (npcs.Length < 2) { Debug.LogError($"[Validation] Expected >=2 NPCRescueControllers, found {npcs.Length}"); failures++; }

                var checkpoints = Object.FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
                if (checkpoints.Length < 3) { Debug.LogError($"[Validation] Expected 3 CheckpointTriggers, found {checkpoints.Length}"); failures++; }

                var tri = NavMesh.CalculateTriangulation();
                if (tri.vertices == null || tri.vertices.Length == 0)
                {
                    Debug.LogError("[Validation] NavMesh is not baked (0 vertices).");
                    failures++;
                }

                // 4. Functional logic check on FireController & ScoreManager
                if (fires.Length > 0)
                {
                    float before = fires[0].FireIntensity;
                    fires[0].ApplyExtinguisherSpray(25f);
                    if (fires[0].FireIntensity >= before)
                    {
                        Debug.LogError("[Validation] FireController.ApplyExtinguisherSpray did not reduce intensity.");
                        failures++;
                    }
                    fires[0].ApplyExtinguisherSpray(1000f);
                    if (!fires[0].IsExtinguished)
                    {
                        Debug.LogError("[Validation] FireController did not extinguish at 0 intensity.");
                        failures++;
                    }
                }

                var scoreMgr = Object.FindFirstObjectByType<ScoreManager>();
                if (scoreMgr != null)
                {
                    var res = scoreMgr.CalculateResult(true, 4, 4, 2, 2, 120f, 180f, 90f, 80f);
                    if (res.totalScore <= 0 || res.rating != "S")
                    {
                        Debug.LogError($"[Validation] Unexpected ScoreManager result: score={res.totalScore}, rating={res.rating}");
                        failures++;
                    }
                }
            }

            if (failures > 0)
            {
                Debug.LogError($"[Validation] FAILED with {failures} error(s).");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[Validation] ALL CHECKS PASSED SUCCESSFULLY!");
            }
        }
    }
}
#endif
