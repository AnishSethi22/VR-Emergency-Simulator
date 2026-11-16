#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using Unity.AI.Navigation;
using Unity.XR.CoreUtils;
using TMPro;
using EmergencyRescue.Core;
using EmergencyRescue.Audio;
using EmergencyRescue.Player;
using EmergencyRescue.Interaction;
using EmergencyRescue.Fire;
using EmergencyRescue.NPC;
using EmergencyRescue.Missions;
using EmergencyRescue.UI;

namespace EmergencyRescue.Editor
{
    /// <summary>
    /// Automated Unity Editor builder that generates materials, prefabs, NavMesh,
    /// MainMenu.unity, RescueBuilding.unity, and configures Build Settings.
    /// Run via menu: RescueSim -> Build Complete Project, or -executeMethod.
    /// </summary>
    public static class RescueProjectBuilder
    {
        private const string MatDir = "Assets/Materials";
        private const string PrefabDir = "Assets/Prefabs";
        private const string SceneDir = "Assets/Scenes";

        [MenuItem("RescueSim/Build Complete Project")]
        public static void BuildAll()
        {
            EnsureTMPEssentials();
            EnsureFolders();
            var mats = CreateMaterials();
            var prefabs = CreatePrefabs(mats);

            string menuPath = BuildMainMenuScene(mats);
            string gamePath = BuildRescueBuildingScene(mats, prefabs);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(menuPath, true),
                new EditorBuildSettingsScene(gamePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RescueProjectBuilder] Complete project built successfully: MainMenu & RescueBuilding.");
        }

        private static void EnsureTMPEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null)
                return;

            string pkgPath = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (File.Exists(pkgPath))
            {
                AssetDatabase.ImportPackage(pkgPath, false);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureFolders()
        {
            string[] folders = { MatDir, PrefabDir, SceneDir };
            foreach (var f in folders)
            {
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = Path.GetDirectoryName(f).Replace("\\", "/");
                    string name = Path.GetFileName(f);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }
        }

        private class MaterialSet
        {
            public Material FloorConcrete;
            public Material CarpetOffice;
            public Material WallDrywall;
            public Material WallSmokeStained;
            public Material Ceiling;
            public Material WoodDesk;
            public Material DarkMetal;
            public Material ChairFabric;
            public Material GlassWindow;
            public Material ExtinguisherRed;
            public Material SafeZoneGreen;
            public Material ExitSignGreen;
            public Material AlarmRedEmissive;
            public Material DoorWood;
            public Material NpcSkin;
            public Material NpcShirt;
            public Material NpcPants;
            public Material FlameParticle;
            public Material SmokeParticle;
            public Material FoamParticle;
            public Material CrateWood;
        }

        private static MaterialSet CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Shader particleUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");

            Material MakeLit(string name, Color color, float smoothness = 0.35f, float metallic = 0.1f, Color? emission = null)
            {
                string path = $"{MatDir}/{name}.mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(lit);
                    AssetDatabase.CreateAsset(m, path);
                }
                else
                {
                    m.shader = lit;
                }
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", metallic);
                if (emission.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emission.Value);
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            Material MakeTransparent(string name, Color color)
            {
                Material m = MakeLit(name, color, 0.85f, 0.1f);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
                EditorUtility.SetDirty(m);
                return m;
            }

            Material MakeParticle(string name, Color color, bool additive)
            {
                string path = $"{MatDir}/{name}.mat";
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(particleUnlit);
                    AssetDatabase.CreateAsset(m, path);
                }
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Surface", 1f);
                if (additive)
                {
                    m.SetFloat("_Blend", 2f); // Additive
                }
                EditorUtility.SetDirty(m);
                return m;
            }

            return new MaterialSet
            {
                FloorConcrete = MakeLit("FloorConcrete", new Color(0.42f, 0.44f, 0.47f), 0.5f, 0.1f),
                CarpetOffice = MakeLit("CarpetOffice", new Color(0.28f, 0.35f, 0.44f), 0.2f, 0.05f),
                WallDrywall = MakeLit("WallDrywall", new Color(0.88f, 0.86f, 0.82f), 0.25f, 0.05f),
                WallSmokeStained = MakeLit("WallSmokeStained", new Color(0.42f, 0.38f, 0.35f), 0.15f),
                Ceiling = MakeLit("Ceiling", new Color(0.75f, 0.77f, 0.80f), 0.2f),
                WoodDesk = MakeLit("WoodDesk", new Color(0.58f, 0.38f, 0.22f), 0.55f, 0.1f),
                DarkMetal = MakeLit("DarkMetal", new Color(0.22f, 0.24f, 0.28f), 0.75f, 0.85f),
                ChairFabric = MakeLit("ChairFabric", new Color(0.20f, 0.32f, 0.52f), 0.3f),
                GlassWindow = MakeTransparent("GlassWindow", new Color(0.65f, 0.85f, 0.98f, 0.42f)),
                ExtinguisherRed = MakeLit("ExtinguisherRed", new Color(0.95f, 0.12f, 0.12f), 0.8f, 0.4f, new Color(0.3f, 0.02f, 0.02f)),
                SafeZoneGreen = MakeLit("SafeZoneGreen", new Color(0.15f, 0.85f, 0.35f), 0.4f, 0f, new Color(0.25f, 1.2f, 0.45f)),
                ExitSignGreen = MakeLit("ExitSignGreen", new Color(0.1f, 1f, 0.4f), 0.4f, 0f, new Color(0.5f, 3.5f, 1.2f)),
                AlarmRedEmissive = MakeLit("AlarmRedEmissive", new Color(1f, 0.2f, 0.15f), 0.6f, 0f, new Color(3.5f, 0.4f, 0.2f)),
                DoorWood = MakeLit("DoorWood", new Color(0.52f, 0.32f, 0.18f), 0.45f),
                NpcSkin = MakeLit("NpcSkin", new Color(0.92f, 0.74f, 0.62f), 0.3f),
                NpcShirt = MakeLit("NpcShirt", new Color(0.25f, 0.60f, 0.95f), 0.25f, 0f, new Color(0.05f, 0.15f, 0.25f)),
                NpcPants = MakeLit("NpcPants", new Color(0.22f, 0.25f, 0.32f), 0.25f),
                FlameParticle = MakeParticle("FlameParticle", new Color(1f, 0.62f, 0.15f, 0.95f), true),
                SmokeParticle = MakeParticle("SmokeParticle", new Color(0.18f, 0.18f, 0.20f, 0.5f), false),
                FoamParticle = MakeParticle("FoamParticle", new Color(0.92f, 0.98f, 1f, 0.85f), false),
                CrateWood = MakeLit("CrateWood", new Color(0.62f, 0.45f, 0.26f), 0.35f)
            };
        }

        private class PrefabSet
        {
            public GameObject FireHazard;
            public GameObject FireExtinguisher;
            public GameObject InteractiveDoor;
            public GameObject TrappedNPC;
            public GameObject RefillStation;
            public GameObject EmergencyLight;
            public GameObject OfficeDesk;
            public GameObject OfficeChair;
            public GameObject PhysicsCrate;
        }

        private static PrefabSet CreatePrefabs(MaterialSet mats)
        {
            return new PrefabSet
            {
                FireHazard = BuildFireHazardPrefab(mats),
                FireExtinguisher = BuildFireExtinguisherPrefab(mats),
                InteractiveDoor = BuildDoorPrefab(mats),
                TrappedNPC = BuildNPCPrefab(mats),
                RefillStation = BuildRefillStationPrefab(mats),
                EmergencyLight = BuildEmergencyLightPrefab(mats),
                OfficeDesk = BuildDeskPrefab(mats),
                OfficeChair = BuildChairPrefab(mats),
                PhysicsCrate = BuildCratePrefab(mats)
            };
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildFireHazardPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("FireHazard");
            SphereCollider col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 2.6f;

            FireController fc = root.AddComponent<FireController>();

            // Flame Particles
            GameObject flameGo = new GameObject("Flames");
            flameGo.transform.SetParent(root.transform, false);
            ParticleSystem flamePS = flameGo.AddComponent<ParticleSystem>();
            var fMain = flamePS.main;
            fMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
            fMain.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.8f);
            fMain.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
            fMain.startColor = new Color(1f, 0.52f, 0.1f, 0.9f);
            var fEm = flamePS.emission;
            fEm.rateOverTime = 45f;
            var fShape = flamePS.shape;
            fShape.shapeType = ParticleSystemShapeType.Cone;
            fShape.angle = 18f;
            fShape.radius = 0.55f;
            flameGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            flameGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.FlameParticle;

            // Smoke Particles
            GameObject smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(root.transform, false);
            smokeGo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            ParticleSystem smokePS = smokeGo.AddComponent<ParticleSystem>();
            var sMain = smokePS.main;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            sMain.startColor = new Color(0.15f, 0.15f, 0.16f, 0.55f);
            var sEm = smokePS.emission;
            sEm.rateOverTime = 16f;
            var sShape = smokePS.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 25f;
            sShape.radius = 0.5f;
            smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            smokeGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.SmokeParticle;

            // Spark Particles
            GameObject sparkGo = new GameObject("Sparks");
            sparkGo.transform.SetParent(root.transform, false);
            ParticleSystem sparkPS = sparkGo.AddComponent<ParticleSystem>();
            var pMain = sparkPS.main;
            pMain.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            pMain.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            pMain.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            pMain.startColor = new Color(1f, 0.85f, 0.35f, 1f);
            var pEm = sparkPS.emission;
            pEm.rateOverTime = 15f;
            var pShape = sparkPS.shape;
            pShape.shapeType = ParticleSystemShapeType.Cone;
            pShape.angle = 30f;
            pShape.radius = 0.4f;
            sparkGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            sparkGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.FlameParticle;

            // Fire Light
            GameObject lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            Light pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.45f, 0.12f);
            pointLight.range = 10f;
            pointLight.intensity = 6f;

            AudioSource audioSrc = root.AddComponent<AudioSource>();
            audioSrc.loop = true;
            audioSrc.spatialBlend = 1f;
            audioSrc.minDistance = 1.5f;
            audioSrc.maxDistance = 16f;

            fc.ConfigureReferences(flamePS, smokePS, sparkPS, pointLight, audioSrc, true, true);
            return SavePrefab(root, "FireHazard");
        }

        private static GameObject BuildFireExtinguisherPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("FireExtinguisher");
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 4.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 0.65f;
            col.radius = 0.11f;
            col.center = new Vector3(0f, 0.32f, 0f);

            // Body cylinder
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
            body.transform.localScale = new Vector3(0.18f, 0.25f, 0.18f);
            body.GetComponent<Renderer>().sharedMaterial = mats.ExtinguisherRed;

            // Valve head
            GameObject valve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(valve.GetComponent<Collider>());
            valve.name = "Valve";
            valve.transform.SetParent(root.transform, false);
            valve.transform.localPosition = new Vector3(0f, 0.56f, 0f);
            valve.transform.localScale = new Vector3(0.06f, 0.05f, 0.06f);
            valve.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;

            // Nozzle
            GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(nozzle.GetComponent<Collider>());
            nozzle.name = "Nozzle";
            nozzle.transform.SetParent(root.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, 0.55f, 0.16f);
            nozzle.transform.localScale = new Vector3(0.04f, 0.04f, 0.22f);
            nozzle.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;

            // Pressure Gauge Indicator Bar
            GameObject gauge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(gauge.GetComponent<Collider>());
            gauge.name = "PressureGaugeBar";
            gauge.transform.SetParent(root.transform, false);
            gauge.transform.localPosition = new Vector3(0f, 0.42f, -0.095f);
            gauge.transform.localScale = new Vector3(0.03f, 0.18f, 0.02f);
            gauge.GetComponent<Renderer>().sharedMaterial = mats.ExitSignGreen;

            // Spray Particles
            GameObject sprayGo = new GameObject("SprayParticles");
            sprayGo.transform.SetParent(nozzle.transform, false);
            sprayGo.transform.localPosition = new Vector3(0f, 0f, 0.55f);
            ParticleSystem sprayPS = sprayGo.AddComponent<ParticleSystem>();
            var sMain = sprayPS.main;
            sMain.playOnAwake = false;
            sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
            sMain.startSpeed = new ParticleSystem.MinMaxCurve(9f, 12f);
            sMain.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            sMain.startColor = new Color(0.92f, 0.97f, 1f, 0.75f);
            var sEm = sprayPS.emission;
            sEm.rateOverTime = 70f;
            var sShape = sprayPS.shape;
            sShape.shapeType = ParticleSystemShapeType.Cone;
            sShape.angle = 10f;
            sShape.radius = 0.04f;
            var sCol = sprayPS.collision;
            sCol.enabled = true;
            sCol.type = ParticleSystemCollisionType.World;
            sCol.sendCollisionMessages = true;
            sprayGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.FoamParticle;

            AudioSource sprayAudio = root.AddComponent<AudioSource>();
            sprayAudio.playOnAwake = false;
            sprayAudio.loop = true;
            sprayAudio.spatialBlend = 1f;

            FireExtinguisher ext = root.AddComponent<FireExtinguisher>();
            ext.ConfigureReferences(sprayGo.transform, sprayPS, sprayAudio, gauge.transform);

            // Right Arm - Straight forward holding extinguisher handle
            // Extinguisher is upright, positioned naturally in front right
            ext.ConfigureHoldOffset(new Vector3(0.28f, -0.32f, 0.72f), new Vector3(0f, 0f, 0f));
            var soExt = new SerializedObject(ext);
            soExt.FindProperty("objectDisplayName").stringValue = "Fire Extinguisher [Hold Left Click to Spray Aag]";
            soExt.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, "FireExtinguisher");
        }

        private static GameObject BuildDoorPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("InteractiveDoor");
            GameObject pivot = new GameObject("DoorPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(-0.6f, 0f, 0f);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "DoorLeaf";
            leaf.transform.SetParent(pivot.transform, false);
            leaf.transform.localPosition = new Vector3(0.6f, 1.2f, 0f);
            leaf.transform.localScale = new Vector3(1.2f, 2.4f, 0.08f);
            leaf.GetComponent<Renderer>().sharedMaterial = mats.DoorWood;

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(handle.GetComponent<Collider>());
            handle.name = "Handle";
            handle.transform.SetParent(leaf.transform, false);
            handle.transform.localPosition = new Vector3(0.4f, -0.05f, 0.7f);
            handle.transform.localScale = new Vector3(0.12f, 0.05f, 0.7f);
            handle.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;

            DoorController dc = root.AddComponent<DoorController>();
            var so = new SerializedObject(dc);
            so.FindProperty("doorPivot").objectReferenceValue = pivot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, "InteractiveDoor");
        }

        private static GameObject BuildNPCPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("TrappedNPC");
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.9f, 0f);

            NavMeshAgent agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.8f;
            agent.speed = 3.2f;

            GameObject bodyRoot = new GameObject("BodyRoot");
            bodyRoot.transform.SetParent(root.transform, false);

            // Legs
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(leg.GetComponent<Collider>());
                leg.transform.SetParent(bodyRoot.transform, false);
                leg.transform.localPosition = new Vector3(i * 0.13f, 0.45f, 0f);
                leg.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
                leg.GetComponent<Renderer>().sharedMaterial = mats.NpcPants;
            }

            // Torso
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(torso.GetComponent<Collider>());
            torso.transform.SetParent(bodyRoot.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            torso.transform.localScale = new Vector3(0.42f, 0.42f, 0.28f);
            torso.GetComponent<Renderer>().sharedMaterial = mats.NpcShirt;

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.transform.SetParent(bodyRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            head.transform.localScale = new Vector3(0.28f, 0.3f, 0.28f);
            head.GetComponent<Renderer>().sharedMaterial = mats.NpcSkin;

            // Right & Left Arm Pivots
            Transform MakeArm(string name, float xOffset)
            {
                GameObject armPivot = new GameObject(name);
                armPivot.transform.SetParent(bodyRoot.transform, false);
                armPivot.transform.localPosition = new Vector3(xOffset, 1.35f, 0f);

                GameObject armMesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(armMesh.GetComponent<Collider>());
                armMesh.transform.SetParent(armPivot.transform, false);
                armMesh.transform.localPosition = new Vector3(0f, -0.25f, 0f);
                armMesh.transform.localScale = new Vector3(0.13f, 0.3f, 0.13f);
                armMesh.GetComponent<Renderer>().sharedMaterial = mats.NpcShirt;
                return armPivot.transform;
            }

            Transform rArm = MakeArm("RightArmPivot", 0.28f);
            Transform lArm = MakeArm("LeftArmPivot", -0.28f);

            // Floating Status Label
            GameObject labelGo = new GameObject("StatusLabel");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.25f, 0f);
            TextMeshPro tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = "Civilian\n[HELP! PRESS E]";
            tmp.fontSize = 3.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);

            NPCRescueController ctrl = root.AddComponent<NPCRescueController>();
            ctrl.Configure("Civilian", null, bodyRoot.transform, rArm, lArm, tmp);

            return SavePrefab(root, "TrappedNPC");
        }

        private static GameObject BuildRefillStationPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("RefillStation");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 1.4f, 0.5f);
            col.center = new Vector3(0f, 0.7f, 0f);

            GameObject cabinet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cabinet.GetComponent<Collider>());
            cabinet.transform.SetParent(root.transform, false);
            cabinet.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            cabinet.transform.localScale = new Vector3(0.75f, 1.35f, 0.35f);
            cabinet.GetComponent<Renderer>().sharedMaterial = mats.ExtinguisherRed;

            GameObject sign = new GameObject("Sign");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 1.55f, 0.19f);
            TextMeshPro tmp = sign.AddComponent<TextMeshPro>();
            tmp.text = "REFILL\n[E]";
            tmp.fontSize = 2.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(1.5f, 0.7f);

            root.AddComponent<ExtinguisherRefillStation>();
            return SavePrefab(root, "RefillStation");
        }

        private static GameObject BuildEmergencyLightPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("EmergencyLight");
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(housing.GetComponent<Collider>());
            housing.transform.SetParent(root.transform, false);
            housing.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            housing.GetComponent<Renderer>().sharedMaterial = mats.AlarmRedEmissive;

            Light lt = root.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.color = new Color(1f, 0.18f, 0.1f);
            lt.range = 8f;
            lt.intensity = 2.2f;

            root.AddComponent<EmergencyLightFlicker>();
            return SavePrefab(root, "EmergencyLight");
        }

        private static GameObject BuildDeskPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("OfficeDesk");
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.6f, 0.78f, 0.8f);
            col.center = new Vector3(0f, 0.39f, 0f);

            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(top.GetComponent<Collider>());
            top.transform.SetParent(root.transform, false);
            top.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            top.transform.localScale = new Vector3(1.6f, 0.06f, 0.8f);
            top.GetComponent<Renderer>().sharedMaterial = mats.WoodDesk;

            // 4 legs
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.DestroyImmediate(leg.GetComponent<Collider>());
                    leg.transform.SetParent(root.transform, false);
                    leg.transform.localPosition = new Vector3(x * 0.72f, 0.36f, z * 0.33f);
                    leg.transform.localScale = new Vector3(0.06f, 0.72f, 0.06f);
                    leg.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;
                }
            }

            // Monitor
            GameObject mon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(mon.GetComponent<Collider>());
            mon.transform.SetParent(root.transform, false);
            mon.transform.localPosition = new Vector3(0f, 1.02f, -0.2f);
            mon.transform.localScale = new Vector3(0.55f, 0.34f, 0.04f);
            mon.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;

            return SavePrefab(root, "OfficeDesk");
        }

        private static GameObject BuildChairPrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("OfficeChair");
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 12f;

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.55f, 0.95f, 0.55f);
            col.center = new Vector3(0f, 0.48f, 0f);

            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(seat.GetComponent<Collider>());
            seat.transform.SetParent(root.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            seat.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
            seat.GetComponent<Renderer>().sharedMaterial = mats.ChairFabric;

            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(back.GetComponent<Collider>());
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, 0.75f, -0.22f);
            back.transform.localScale = new Vector3(0.48f, 0.55f, 0.07f);
            back.GetComponent<Renderer>().sharedMaterial = mats.ChairFabric;

            GameObject baseCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(baseCol.GetComponent<Collider>());
            baseCol.transform.SetParent(root.transform, false);
            baseCol.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            baseCol.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
            baseCol.GetComponent<Renderer>().sharedMaterial = mats.DarkMetal;

            return SavePrefab(root, "OfficeChair");
        }

        private static GameObject BuildCratePrefab(MaterialSet mats)
        {
            GameObject root = new GameObject("PhysicsCrate");
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 18f;
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.size = Vector3.one * 0.85f;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = Vector3.one * 0.85f;
            box.GetComponent<Renderer>().sharedMaterial = mats.CrateWood;

            GrabbableObject grabbable = root.AddComponent<GrabbableObject>();
            var so = new SerializedObject(grabbable);
            so.FindProperty("objectDisplayName").stringValue = "Obstacle Crate [Move out of path & press G to drop]";
            so.ApplyModifiedPropertiesWithoutUndo();
            grabbable.ConfigureHoldOffset(new Vector3(0f, -0.2f, 1.1f), Vector3.zero);
            return SavePrefab(root, "PhysicsCrate");
        }

        // =========================================================================
        // MAIN MENU SCENE
        // =========================================================================
        private static string BuildMainMenuScene(MaterialSet mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject gmGo = new GameObject("GameManager");
            gmGo.AddComponent<GameManager>();

            GameObject audioGo = new GameObject("AudioManager");
            audioGo.AddComponent<AudioManager>();

            EnsureEventSystem();

            // Dark dramatic background camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.05f, 0.07f, 0.1f);
            }

            // Canvas
            GameObject canvasGo = CreateUICanvas("MainMenuCanvas");
            MainMenuUI menuUI = canvasGo.AddComponent<MainMenuUI>();

            // Background Panel
            GameObject bg = CreatePanel(canvasGo.transform, "Backdrop", Vector2.zero, Vector2.one, new Color(0.06f, 0.08f, 0.12f, 0.96f));

            // Main Panel
            GameObject mainPanel = CreatePanel(bg.transform, "MainPanel", new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f), new Color(0.08f, 0.11f, 0.16f, 0.94f));

            CreateTMP(mainPanel.transform, "Title", "VR EMERGENCY RESCUE SIMULATOR", 46, FontStyles.Bold, new Color(1f, 0.65f, 0.2f),
                new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f));
            CreateTMP(mainPanel.transform, "Subtitle", "Firefighter Training Experience  |  Universal Render Pipeline (URP)", 22, FontStyles.Normal, new Color(0.8f, 0.88f, 0.95f),
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.82f));

            Button playBtn = CreateButton(mainPanel.transform, "PlayButton", "PLAY SIMULATION (START GAME)", new Color(0.92f, 0.38f, 0.14f), new Vector2(0.25f, 0.50f), new Vector2(0.75f, 0.64f));
            Button instBtn = CreateButton(mainPanel.transform, "InstructionsButton", "HOW TO PLAY & MISSION GUIDE (KAISE KHELE)", new Color(0.25f, 0.45f, 0.70f), new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.44f));
            Button exitBtn = CreateButton(mainPanel.transform, "ExitButton", "EXIT SIMULATOR", new Color(0.55f, 0.18f, 0.18f), new Vector2(0.25f, 0.14f), new Vector2(0.75f, 0.25f));

            // Instructions Panel
            GameObject instPanel = CreatePanel(bg.transform, "InstructionsPanel", new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.94f), new Color(0.08f, 0.11f, 0.16f, 0.97f));
            CreateTMP(instPanel.transform, "InstTitle", "HOW TO PLAY & MISSION GUIDE (KAISE KHELE)", 34, FontStyles.Bold, new Color(1f, 0.7f, 0.25f), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f));
            CreateTMP(instPanel.transform, "InstBody",
                "1. DESKTOP CONTROLS:\n" +
                "  - WASD : Walk in any direction (chalne ke liye)\n" +
                "  - Left Shift (Hold) : Sprint / Fast run (tez bhaagne ke liye)\n" +
                "  - Mouse : Look around smoothly | Click anywhere on screen to lock cursor\n" +
                "  - [E] Key : Pick up Fire Extinguisher / Open Doors / Rescue Civilians / Refill\n" +
                "  - Left Mouse Button (Hold) : Spray Extinguisher (Aag bujhane ke liye)\n" +
                "  - [G] or [Q] : Drop held extinguisher | [Esc] : Pause game\n\n" +
                "2. OBJECTIVES (AAPKA MISSION):\n" +
                "  - Mission 1 (Fire Control): Enter the building -> Pick up red extinguisher -> Put out lobby fire -> Go forward.\n" +
                "  - Mission 2 (Human Rescue): Find 2 trapped civilians (Alex in Office A, Maya in Office B) -> Press E to rescue them.\n" +
                "  - Mission 3 (Final Escape): Run through smoke corridor -> Watch Oxygen level -> Reach green Emergency Exit!\n\n" +
                "3. SURVIVAL RULES:\n" +
                "  - Stay away from high flames to prevent Health damage.\n" +
                "  - Inside smoke, Oxygen drops rapidly! Step into fresh air or complete rescue quickly.",
                19, FontStyles.Normal, new Color(0.92f, 0.96f, 1f), new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.86f), TextAlignmentOptions.TopLeft);
            Button instBack = CreateButton(instPanel.transform, "BackButton", "BACK TO MAIN MENU", new Color(0.28f, 0.35f, 0.48f), new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.12f));
            instPanel.SetActive(false);

            // Wire MainMenuUI
            var so = new SerializedObject(menuUI);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("instructionsPanel").objectReferenceValue = instPanel;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddPersistentListener(playBtn.onClick, menuUI.OnPlayClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(instBtn.onClick, menuUI.ShowInstructions);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(exitBtn.onClick, menuUI.OnExitClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(instBack.onClick, menuUI.ShowMainPanel);

            string path = $"{SceneDir}/MainMenu.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // =========================================================================
        // RESCUE BUILDING GAMEPLAY SCENE
        // =========================================================================
        private static string BuildRescueBuildingScene(MaterialSet mats, PrefabSet prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default camera (players have their own cameras)
            if (Camera.main != null) Object.DestroyImmediate(Camera.main.gameObject);

            // Atmospheric smoky lighting & fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.40f, 0.44f); // Much clearer ambient light
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.26f, 0.22f, 0.22f);
            RenderSettings.fogDensity = 0.012f; // Reduced from 0.025f for crisp readability

            // Core Managers
            GameObject managers = new GameObject("--- MANAGERS ---");
            new GameObject("GameManager").AddComponent<GameManager>().transform.SetParent(managers.transform);
            new GameObject("AudioManager").AddComponent<AudioManager>().transform.SetParent(managers.transform);
            MissionManager missionMgr = new GameObject("MissionManager").AddComponent<MissionManager>();
            missionMgr.transform.SetParent(managers.transform);
            new GameObject("ScoreManager").AddComponent<ScoreManager>().transform.SetParent(managers.transform);
            new GameObject("FireSpreadSystem").AddComponent<FireSpreadSystem>().transform.SetParent(managers.transform);
            PlayerModeManager modeMgr = new GameObject("PlayerModeManager").AddComponent<PlayerModeManager>();
            modeMgr.transform.SetParent(managers.transform);

            EnsureEventSystem();

            // =====================================================================
            // ENVIRONMENT ARCHITECTURE (Office Building: X [-12..12], Z [-6..24])
            // =====================================================================
            GameObject env = new GameObject("--- ENVIRONMENT ---");
            NavMeshSurface navSurface = env.AddComponent<NavMeshSurface>();

            // Exterior Plaza & Ground
            CreateBox(env.transform, "ExteriorGround", new Vector3(0f, -0.1f, -10f), new Vector3(36f, 0.2f, 20f), mats.FloorConcrete);
            // Building Main Floor
            CreateBox(env.transform, "BuildingFloor", new Vector3(0f, -0.1f, 10f), new Vector3(24f, 0.2f, 26f), mats.CarpetOffice);
            // Ceiling
            CreateBox(env.transform, "BuildingCeiling", new Vector3(0f, 3.7f, 10f), new Vector3(24f, 0.2f, 26f), mats.Ceiling);

            // Perimeter Walls (Front Z=-3 has Main Entrance Gap [-1.5..1.5], Back Z=23 has Emergency Exit Gap [7.5..10.5])
            CreateBox(env.transform, "FrontWall_Left", new Vector3(-6.75f, 1.8f, -3f), new Vector3(10.5f, 3.6f, 0.3f), mats.WallDrywall);
            CreateBox(env.transform, "FrontWall_Right", new Vector3(6.75f, 1.8f, -3f), new Vector3(10.5f, 3.6f, 0.3f), mats.WallDrywall);
            CreateBox(env.transform, "FrontEntranceHeader", new Vector3(0f, 3.05f, -3f), new Vector3(3f, 1.1f, 0.3f), mats.WallDrywall);

            CreateBox(env.transform, "BackWall_Left", new Vector3(-2.25f, 1.8f, 23f), new Vector3(19.5f, 3.6f, 0.3f), mats.WallSmokeStained);
            CreateBox(env.transform, "BackWall_Right", new Vector3(11.25f, 1.8f, 23f), new Vector3(1.5f, 3.6f, 0.3f), mats.WallSmokeStained);
            CreateBox(env.transform, "BackExitHeader", new Vector3(9f, 3.05f, 23f), new Vector3(3f, 1.1f, 0.3f), mats.WallSmokeStained);

            CreateBox(env.transform, "WestWall", new Vector3(-12f, 1.8f, 10f), new Vector3(0.3f, 3.6f, 26f), mats.WallDrywall);
            CreateBox(env.transform, "EastWall", new Vector3(12f, 1.8f, 10f), new Vector3(0.3f, 3.6f, 26f), mats.WallDrywall);

            // Windows on West Wall (decorative transparent glass)
            CreateBox(env.transform, "WestWindow1", new Vector3(-11.82f, 1.8f, 2f), new Vector3(0.08f, 1.6f, 4f), mats.GlassWindow, false);
            CreateBox(env.transform, "WestWindow2", new Vector3(-11.82f, 1.8f, 12f), new Vector3(0.08f, 1.6f, 4f), mats.GlassWindow, false);

            // Ceiling Fluorescent Office Lights for realistic environment lighting & visibility
            void AddCeilingLight(string name, Vector3 pos)
            {
                GameObject cl = new GameObject(name);
                cl.transform.SetParent(env.transform, false);
                cl.transform.position = pos;
                Light l = cl.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(0.95f, 0.96f, 1.0f);
                l.range = 14f;
                l.intensity = 2.4f;
            }
            AddCeilingLight("CeilingLight_Lobby", new Vector3(0f, 3.4f, 1.5f));
            AddCeilingLight("CeilingLight_Hallway", new Vector3(0f, 3.4f, 8.5f));
            AddCeilingLight("CeilingLight_OfficeA", new Vector3(-7.5f, 3.4f, 10f));
            AddCeilingLight("CeilingLight_OfficeB", new Vector3(7.5f, 3.4f, 10f));
            AddCeilingLight("CeilingLight_Exterior", new Vector3(0f, 5.0f, -6f));

            // Interior Partitions: Lobby ends at Z=5 (doorway at X=[-1..1])
            CreateBox(env.transform, "LobbyDivider_Left", new Vector3(-6.5f, 1.8f, 5f), new Vector3(11f, 3.6f, 0.25f), mats.WallDrywall);
            CreateBox(env.transform, "LobbyDivider_Right", new Vector3(6.5f, 1.8f, 5f), new Vector3(11f, 3.6f, 0.25f), mats.WallDrywall);
            CreateBox(env.transform, "LobbyDoorHeader", new Vector3(0f, 3.05f, 5f), new Vector3(2f, 1.1f, 0.25f), mats.WallDrywall);

            // Office Room A (West: X [-12..-3], Z [5..15]) - doorway at Z=10
            CreateBox(env.transform, "OfficeA_Wall_South", new Vector3(-3f, 1.8f, 7f), new Vector3(0.25f, 3.6f, 4f), mats.WallDrywall);
            CreateBox(env.transform, "OfficeA_Wall_North", new Vector3(-3f, 1.8f, 13f), new Vector3(0.25f, 3.6f, 4f), mats.WallDrywall);
            CreateBox(env.transform, "OfficeA_NorthDivider", new Vector3(-7.5f, 1.8f, 15f), new Vector3(9f, 3.6f, 0.25f), mats.WallSmokeStained);

            // Office Room B (East: X [3..12], Z [5..15]) - doorway at Z=10
            CreateBox(env.transform, "OfficeB_Wall_South", new Vector3(3f, 1.8f, 7f), new Vector3(0.25f, 3.6f, 4f), mats.WallDrywall);
            CreateBox(env.transform, "OfficeB_Wall_North", new Vector3(3f, 1.8f, 13f), new Vector3(0.25f, 3.6f, 4f), mats.WallDrywall);
            CreateBox(env.transform, "OfficeB_NorthDivider_W", new Vector3(4.5f, 1.8f, 15f), new Vector3(3f, 3.6f, 0.25f), mats.WallSmokeStained);
            CreateBox(env.transform, "OfficeB_NorthDivider_E", new Vector3(10.5f, 1.8f, 15f), new Vector3(3f, 3.6f, 0.25f), mats.WallSmokeStained);

            // Smoke Corridor Partition (Z=15..23)
            CreateBox(env.transform, "SmokeCorridor_WestBlock", new Vector3(-2f, 1.8f, 19f), new Vector3(0.25f, 3.6f, 8f), mats.WallSmokeStained);

            // Doors
            InstantiatePrefab(prefabs.InteractiveDoor, env.transform, "LobbyDoor", new Vector3(0f, 0f, 5f), Quaternion.identity);
            InstantiatePrefab(prefabs.InteractiveDoor, env.transform, "OfficeADoor", new Vector3(-3f, 0f, 10f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefab(prefabs.InteractiveDoor, env.transform, "OfficeBDoor", new Vector3(3f, 0f, 10f), Quaternion.Euler(0f, 90f, 0f));

            // Safe Evacuation Zone (Exterior Front-Left)
            GameObject evacZone = CreateBox(env.transform, "SafeEvacuationZone", new Vector3(-6.5f, 0.02f, -7.5f), new Vector3(5f, 0.05f, 5f), mats.SafeZoneGreen, true);
            BoxCollider evacCol = evacZone.GetComponent<BoxCollider>();
            evacCol.isTrigger = true;
            evacCol.size = new Vector3(1f, 60f, 1f);
            SmokeZone freshAir = evacZone.AddComponent<SmokeZone>();
            var fSo = new SerializedObject(freshAir);
            fSo.FindProperty("isFreshAirZone").boolValue = true;
            fSo.ApplyModifiedPropertiesWithoutUndo();
            CreateWorldSign(evacZone.transform, "SAFE EVACUATION ZONE", new Vector3(0f, 2.2f, 0f), new Color(0.25f, 1f, 0.45f));

            // Emergency Exit Sign & Trigger
            GameObject exitSign = CreateBox(env.transform, "EmergencyExitSign", new Vector3(9f, 2.75f, 22.7f), new Vector3(1.6f, 0.45f, 0.12f), mats.ExitSignGreen, false);
            CreateWorldSign(exitSign.transform, "EMERGENCY EXIT", new Vector3(0f, 0f, -0.15f), Color.white);

            // Furniture & Physics Obstacles
            InstantiatePrefab(prefabs.OfficeDesk, env.transform, "ReceptionDesk", new Vector3(-4.5f, 0f, 1.5f), Quaternion.identity);
            InstantiatePrefab(prefabs.OfficeChair, env.transform, "ReceptionChair", new Vector3(-4.5f, 0f, 2.5f), Quaternion.Euler(0f, 180f, 0f));

            InstantiatePrefab(prefabs.OfficeDesk, env.transform, "OfficeA_Desk1", new Vector3(-8.5f, 0f, 8.5f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefab(prefabs.OfficeChair, env.transform, "OfficeA_Chair1", new Vector3(-7.4f, 0f, 8.5f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefab(prefabs.OfficeDesk, env.transform, "OfficeA_Desk2", new Vector3(-8.5f, 0f, 12.2f), Quaternion.Euler(0f, 90f, 0f));

            InstantiatePrefab(prefabs.OfficeDesk, env.transform, "OfficeB_Desk1", new Vector3(8.5f, 0f, 8.5f), Quaternion.Euler(0f, -90f, 0f));
            InstantiatePrefab(prefabs.OfficeChair, env.transform, "OfficeB_Chair1", new Vector3(7.4f, 0f, 8.5f), Quaternion.Euler(0f, 90f, 0f));
            InstantiatePrefab(prefabs.OfficeDesk, env.transform, "OfficeB_Desk2", new Vector3(8.5f, 0f, 12.2f), Quaternion.Euler(0f, -90f, 0f));

            // Physics debris crates in smoke corridor
            InstantiatePrefab(prefabs.PhysicsCrate, env.transform, "DebrisCrate1", new Vector3(6.8f, 0.45f, 17.5f), Quaternion.Euler(0f, 22f, 0f));
            InstantiatePrefab(prefabs.PhysicsCrate, env.transform, "DebrisCrate2", new Vector3(8.2f, 0.45f, 19.2f), Quaternion.Euler(0f, -15f, 0f));

            // Emergency Lights & Alarm
            GameObject alarmLight = InstantiatePrefab(prefabs.EmergencyLight, env.transform, "LobbyAlarmLight", new Vector3(0f, 3.4f, 1f), Quaternion.identity);
            var alSo = new SerializedObject(alarmLight.GetComponent<EmergencyLightFlicker>());
            alSo.FindProperty("playAlarmAudio").boolValue = true;
            alSo.ApplyModifiedPropertiesWithoutUndo();

            InstantiatePrefab(prefabs.EmergencyLight, env.transform, "HallwayAlarmLight", new Vector3(0f, 3.4f, 10f), Quaternion.identity);
            InstantiatePrefab(prefabs.EmergencyLight, env.transform, "CorridorAlarmLight", new Vector3(7.5f, 3.4f, 19f), Quaternion.identity);

            // Bake NavMesh!
            navSurface.BuildNavMesh();

            // =====================================================================
            // GAMEPLAY OBJECTS: EXTINGUISHERS, FIRES, NPCS, CHECKPOINTS, SMOKE ZONES
            // =====================================================================
            GameObject gameplay = new GameObject("--- GAMEPLAY ---");

            // Fire Extinguishers & Refill Station
            InstantiatePrefab(prefabs.FireExtinguisher, gameplay.transform, "LobbyExtinguisher", new Vector3(2.8f, 0.05f, -0.8f), Quaternion.identity);
            InstantiatePrefab(prefabs.FireExtinguisher, gameplay.transform, "HallwayExtinguisher", new Vector3(-2.2f, 0.05f, 6.2f), Quaternion.identity);
            InstantiatePrefab(prefabs.RefillStation, gameplay.transform, "LobbyRefillStation", new Vector3(5.2f, 0f, 4.7f), Quaternion.Euler(0f, 180f, 0f));

            // Fires
            InstantiatePrefab(prefabs.FireHazard, gameplay.transform, "Fire_Lobby", new Vector3(0.3f, 0.05f, 3.0f), Quaternion.identity);
            InstantiatePrefab(prefabs.FireHazard, gameplay.transform, "Fire_OfficeA", new Vector3(-6.2f, 0.05f, 10.2f), Quaternion.identity);
            InstantiatePrefab(prefabs.FireHazard, gameplay.transform, "Fire_OfficeB", new Vector3(6.2f, 0.05f, 11.5f), Quaternion.identity);
            InstantiatePrefab(prefabs.FireHazard, gameplay.transform, "Fire_Corridor", new Vector3(7.4f, 0.05f, 16.5f), Quaternion.identity);

            // Dormant spreadable fire node
            GameObject dormantFire = InstantiatePrefab(prefabs.FireHazard, gameplay.transform, "Fire_SpreadNode", new Vector3(1.8f, 0.05f, 3.2f), Quaternion.identity);
            var dfSo = new SerializedObject(dormantFire.GetComponent<FireController>());
            dfSo.FindProperty("startLit").boolValue = false;
            dfSo.FindProperty("countsTowardObjective").boolValue = false;
            dfSo.ApplyModifiedPropertiesWithoutUndo();

            // Trapped NPCs
            GameObject npc1 = InstantiatePrefab(prefabs.TrappedNPC, gameplay.transform, "NPC_Alex", new Vector3(-9.5f, 0f, 10.5f), Quaternion.Euler(0f, 90f, 0f));
            var n1So = new SerializedObject(npc1.GetComponent<NPCRescueController>());
            n1So.FindProperty("npcName").stringValue = "Alex";
            n1So.FindProperty("evacuationTarget").objectReferenceValue = evacZone.transform;
            n1So.ApplyModifiedPropertiesWithoutUndo();

            GameObject npc2 = InstantiatePrefab(prefabs.TrappedNPC, gameplay.transform, "NPC_Maya", new Vector3(9.5f, 0f, 10.5f), Quaternion.Euler(0f, -90f, 0f));
            var n2So = new SerializedObject(npc2.GetComponent<NPCRescueController>());
            n2So.FindProperty("npcName").stringValue = "Maya";
            n2So.FindProperty("evacuationTarget").objectReferenceValue = evacZone.transform;
            n2So.ApplyModifiedPropertiesWithoutUndo();

            // Smoke Zone (Corridor + Offices)
            GameObject smokeZoneGo = new GameObject("SmokeZone_Corridor");
            smokeZoneGo.transform.SetParent(gameplay.transform, false);
            smokeZoneGo.transform.position = new Vector3(7.5f, 1.8f, 19f);
            BoxCollider szCol = smokeZoneGo.AddComponent<BoxCollider>();
            szCol.isTrigger = true;
            szCol.size = new Vector3(8.5f, 3.5f, 8f);
            smokeZoneGo.AddComponent<SmokeZone>();

            // Ambient corridor smoke particles
            ParticleSystem corrSmoke = smokeZoneGo.AddComponent<ParticleSystem>();
            var csMain = corrSmoke.main;
            csMain.startLifetime = 4f;
            csMain.startSpeed = 0.2f;
            csMain.startSize = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
            csMain.startColor = new Color(0.14f, 0.14f, 0.15f, 0.45f);
            var csShape = corrSmoke.shape;
            csShape.shapeType = ParticleSystemShapeType.Box;
            csShape.scale = new Vector3(7f, 2f, 7f);
            smokeZoneGo.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.SmokeParticle;

            // Checkpoints
            CreateCheckpoint(gameplay.transform, "Checkpoint_Entrance", new Vector3(0f, 1.5f, -1.8f), new Vector3(3.2f, 3f, 1.8f), CheckpointType.BuildingEntrance);
            CreateCheckpoint(gameplay.transform, "Checkpoint_Mission1", new Vector3(0f, 1.5f, 7.2f), new Vector3(3.5f, 3f, 2.2f), CheckpointType.Mission1Checkpoint);
            CreateCheckpoint(gameplay.transform, "Checkpoint_EmergencyExit", new Vector3(9f, 1.5f, 22.8f), new Vector3(3.2f, 3f, 1.8f), CheckpointType.EmergencyExit);

            // =====================================================================
            // PLAYERS (DESKTOP + VR XR ORIGIN) & HUD
            // =====================================================================
            GameObject desktopPlayer = BuildDesktopPlayer(new Vector3(0f, 0.1f, -6.5f));
            GameObject vrPlayer = BuildVRPlayer(new Vector3(0f, 0.1f, -6.5f));
            vrPlayer.SetActive(false);

            UIManager uiMgr = BuildGameplayHUD();
            modeMgr.Configure(desktopPlayer, vrPlayer, uiMgr);

            string path = $"{SceneDir}/RescueBuilding.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static GameObject BuildDesktopPlayer(Vector3 position)
        {
            GameObject root = new GameObject("DesktopPlayer");
            root.transform.position = position;

            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<HealthSystem>();
            root.AddComponent<OxygenSystem>();
            InteractionManager interactMgr = root.AddComponent<InteractionManager>();
            PlayerController pc = root.AddComponent<PlayerController>();

            GameObject camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.08f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            GameObject holdAnchor = new GameObject("HoldAnchor");
            holdAnchor.transform.SetParent(camGo.transform, false);
            holdAnchor.transform.localPosition = new Vector3(0.34f, -0.34f, 0.78f);

            pc.ConfigureReferences(cam, holdAnchor.transform, interactMgr);
            return root;
        }

        private static GameObject BuildVRPlayer(Vector3 position)
        {
            GameObject root = new GameObject("VR_XROrigin");
            root.transform.position = position;

            XROrigin xrOrigin = root.AddComponent<XROrigin>();
            CharacterController cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            root.AddComponent<HealthSystem>();
            root.AddComponent<OxygenSystem>();
            XRPlayerController xrCtrl = root.AddComponent<XRPlayerController>();

            GameObject camOffset = new GameObject("Camera Offset");
            camOffset.transform.SetParent(root.transform, false);
            xrOrigin.CameraFloorOffsetObject = camOffset;

            GameObject vrCamGo = new GameObject("VR Main Camera");
            vrCamGo.tag = "MainCamera";
            vrCamGo.transform.SetParent(camOffset.transform, false);
            Camera vrCam = vrCamGo.AddComponent<Camera>();
            vrCam.nearClipPlane = 0.05f;
            vrCamGo.AddComponent<AudioListener>();
            vrCamGo.AddComponent<UniversalAdditionalCameraData>();
            xrOrigin.Camera = vrCam;

            // Left & Right Controllers with Direct + Ray Interactors
            CreateVRHand(camOffset.transform, "LeftHand Controller");
            CreateVRHand(camOffset.transform, "RightHand Controller");

            // Locomotion Providers
            ContinuousMoveProvider moveProv = root.AddComponent<ContinuousMoveProvider>();
            SnapTurnProvider snapProv = root.AddComponent<SnapTurnProvider>();
            ContinuousTurnProvider contTurn = root.AddComponent<ContinuousTurnProvider>();
            contTurn.enabled = false;

            var so = new SerializedObject(xrCtrl);
            so.FindProperty("moveProvider").objectReferenceValue = moveProv;
            so.FindProperty("snapTurnProvider").objectReferenceValue = snapProv;
            so.FindProperty("continuousTurnProvider").objectReferenceValue = contTurn;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void CreateVRHand(Transform parent, string name)
        {
            GameObject hand = new GameObject(name);
            hand.transform.SetParent(parent, false);

            GameObject directGo = new GameObject("DirectInteractor");
            directGo.transform.SetParent(hand.transform, false);
            SphereCollider sc = directGo.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.14f;
            directGo.AddComponent<XRDirectInteractor>();

            GameObject rayGo = new GameObject("RayInteractor");
            rayGo.transform.SetParent(hand.transform, false);
            rayGo.AddComponent<XRRayInteractor>();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.name = "ControllerVisual";
            visual.transform.SetParent(hand.transform, false);
            visual.transform.localScale = new Vector3(0.05f, 0.05f, 0.14f);
        }

        private static UIManager BuildGameplayHUD()
        {
            GameObject canvasGo = CreateUICanvas("GameplayHUDCanvas");
            UIManager ui = canvasGo.AddComponent<UIManager>();

            // Damage Vignette (full screen)
            GameObject vigGo = CreatePanel(canvasGo.transform, "DamageVignette", Vector2.zero, Vector2.one, new Color(0.85f, 0.05f, 0.05f, 0f));
            Image vigImg = vigGo.GetComponent<Image>();
            vigImg.raycastTarget = false;

            // Top-Left: Health & Oxygen Status
            GameObject vitals = CreatePanel(canvasGo.transform, "VitalsPanel", new Vector2(0.015f, 0.78f), new Vector2(0.32f, 0.985f), new Color(0.05f, 0.07f, 0.11f, 0.92f));
            CreateTMP(vitals.transform, "HealthLbl", "HEALTH", 18, FontStyles.Bold, new Color(1f, 0.45f, 0.4f), new Vector2(0.04f, 0.68f), new Vector2(0.34f, 0.96f), TextAlignmentOptions.Left);
            Slider hpBar = CreateSlider(vitals.transform, "HealthBar", 0f, 1f, 1f, new Vector2(0.34f, 0.72f), new Vector2(0.78f, 0.92f), new Color(0.92f, 0.22f, 0.22f));
            TextMeshProUGUI hpTxt = CreateTMP(vitals.transform, "HealthText", "100%", 18, FontStyles.Bold, Color.white, new Vector2(0.80f, 0.68f), new Vector2(0.98f, 0.96f));

            CreateTMP(vitals.transform, "OxygenLbl", "OXYGEN", 18, FontStyles.Bold, new Color(0.4f, 0.88f, 1f), new Vector2(0.04f, 0.36f), new Vector2(0.34f, 0.64f), TextAlignmentOptions.Left);
            Slider oxBar = CreateSlider(vitals.transform, "OxygenBar", 0f, 1f, 1f, new Vector2(0.34f, 0.40f), new Vector2(0.78f, 0.60f), new Color(0.2f, 0.8f, 1f));
            TextMeshProUGUI oxTxt = CreateTMP(vitals.transform, "OxygenText", "100%", 18, FontStyles.Bold, Color.white, new Vector2(0.80f, 0.36f), new Vector2(0.98f, 0.64f));

            TextMeshProUGUI fireTxt = CreateTMP(vitals.transform, "FireStatus", "Fires Out: 0 / 4", 17, FontStyles.Bold, new Color(1f, 0.72f, 0.3f), new Vector2(0.04f, 0.05f), new Vector2(0.50f, 0.32f), TextAlignmentOptions.Left);
            TextMeshProUGUI rescueTxt = CreateTMP(vitals.transform, "RescueStatus", "Rescued: 0 / 2", 17, FontStyles.Bold, new Color(0.4f, 1f, 0.6f), new Vector2(0.52f, 0.05f), new Vector2(0.98f, 0.32f), TextAlignmentOptions.Right);

            // Top-Center: Timer & Warning Banner
            GameObject timerPanel = CreatePanel(canvasGo.transform, "TimerPanel", new Vector2(0.42f, 0.88f), new Vector2(0.58f, 0.985f), new Color(0.05f, 0.07f, 0.11f, 0.92f));
            TextMeshProUGUI timerTxt = CreateTMP(timerPanel.transform, "TimerText", "05:00", 32, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);
            TextMeshProUGUI warnTxt = CreateTMP(canvasGo.transform, "WarningBanner", "WARNING: LOW OXYGEN!", 26, FontStyles.Bold, new Color(1f, 0.2f, 0.2f), new Vector2(0.20f, 0.80f), new Vector2(0.80f, 0.88f));

            // Mission Announcement Banner Popup (Shows on Start & whenever mission phase passes)
            GameObject bannerPanel = CreatePanel(canvasGo.transform, "MissionBannerPanel", new Vector2(0.18f, 0.52f), new Vector2(0.82f, 0.70f), new Color(0.04f, 0.06f, 0.10f, 0.96f));
            TextMeshProUGUI bannerTitle = CreateTMP(bannerPanel.transform, "BannerTitle", "MISSION 1: FIRE CONTROL", 24, FontStyles.Bold, new Color(1f, 0.75f, 0.25f), new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.95f));
            TextMeshProUGUI bannerSub = CreateTMP(bannerPanel.transform, "BannerSub", "Enter the burning building and grab the fire extinguisher [E]!", 17, FontStyles.Normal, Color.white, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.55f));

            // Top-Right: Mission Objectives Card & Room Minimap Guide
            GameObject missionPanel = CreatePanel(canvasGo.transform, "MissionPanel", new Vector2(0.66f, 0.72f), new Vector2(0.985f, 0.985f), new Color(0.05f, 0.07f, 0.11f, 0.92f));
            CreateTMP(missionPanel.transform, "ObjectiveHeader", "CURRENT OBJECTIVE", 15, FontStyles.Bold, new Color(0.8f, 0.85f, 0.9f), new Vector2(0.04f, 0.82f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Left);
            TextMeshProUGUI mTitle = CreateTMP(missionPanel.transform, "MissionTitle", "MISSION 1: FIRE CONTROL", 20, FontStyles.Bold, new Color(1f, 0.72f, 0.25f), new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.82f), TextAlignmentOptions.Left);
            TextMeshProUGUI mObj = CreateTMP(missionPanel.transform, "MissionObjectives", "Objectives...", 15, FontStyles.Normal, Color.white, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.60f), TextAlignmentOptions.TopLeft);

            // Building Room Minimap (Schematic Floor Plan on Top-Right under objectives)
            GameObject mapPanel = CreatePanel(canvasGo.transform, "RoomMinimapPanel", new Vector2(0.68f, 0.44f), new Vector2(0.985f, 0.70f), new Color(0.04f, 0.06f, 0.10f, 0.94f));
            CreateTMP(mapPanel.transform, "MapTitle", "FLOOR MAP (KAMRON KA NAKSHA)", 13, FontStyles.Bold, new Color(0.4f, 0.9f, 1f), new Vector2(0.04f, 0.86f), new Vector2(0.96f, 0.98f), TextAlignmentOptions.Left);
            
            // Schematic Map Layout Rooms
            // Exterior / Safe Zone
            GameObject mapSafe = CreatePanel(mapPanel.transform, "Map_SafeZone", new Vector2(0.06f, 0.06f), new Vector2(0.35f, 0.24f), new Color(0.12f, 0.55f, 0.25f, 0.9f));
            CreateTMP(mapSafe.transform, "TxtSafe", "SAFE ZONE\n(Alex/Maya)", 10, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);

            // Entrance / Start
            GameObject mapStart = CreatePanel(mapPanel.transform, "Map_Start", new Vector2(0.40f, 0.06f), new Vector2(0.94f, 0.24f), new Color(0.20f, 0.28f, 0.38f, 0.9f));
            CreateTMP(mapStart.transform, "TxtStart", "MAIN ENTRANCE\n(Start / Extinguisher)", 10, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);

            // Lobby
            GameObject mapLobby = CreatePanel(mapPanel.transform, "Map_Lobby", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.46f), new Color(0.55f, 0.22f, 0.14f, 0.9f));
            CreateTMP(mapLobby.transform, "TxtLobby", "MAIN LOBBY (Fire 1 - Put out with Extinguisher)", 11, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);

            // Office A (West)
            GameObject mapOffA = CreatePanel(mapPanel.transform, "Map_OfficeA", new Vector2(0.06f, 0.50f), new Vector2(0.48f, 0.68f), new Color(0.18f, 0.38f, 0.65f, 0.9f));
            CreateTMP(mapOffA.transform, "TxtOffA", "OFFICE A\n[Alex - Press E]", 11, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);

            // Office B (East)
            GameObject mapOffB = CreatePanel(mapPanel.transform, "Map_OfficeB", new Vector2(0.52f, 0.50f), new Vector2(0.94f, 0.68f), new Color(0.18f, 0.38f, 0.65f, 0.9f));
            CreateTMP(mapOffB.transform, "TxtOffB", "OFFICE B\n[Maya - Press E]", 11, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);

            // Smoke Corridor & Exit
            GameObject mapExit = CreatePanel(mapPanel.transform, "Map_Exit", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.84f), new Color(0.70f, 0.18f, 0.18f, 0.95f));
            CreateTMP(mapExit.transform, "TxtExit", "SMOKE CORRIDOR  --->  [EMERGENCY EXIT]", 11, FontStyles.Bold, new Color(0.35f, 1f, 0.55f), Vector2.zero, Vector2.one);

            // Bottom-Left: In-Game Controls & Instructions (Clean, Large, Readable)
            GameObject controlsPanel = CreatePanel(canvasGo.transform, "ControlsHintPanel", new Vector2(0.015f, 0.02f), new Vector2(0.38f, 0.20f), new Color(0.05f, 0.07f, 0.11f, 0.90f));
            CreateTMP(controlsPanel.transform, "ControlsTitle", "QUICK CONTROLS GUIDE (KAISE KHELE)", 16, FontStyles.Bold, new Color(1f, 0.75f, 0.3f), new Vector2(0.04f, 0.75f), new Vector2(0.96f, 0.96f), TextAlignmentOptions.Left);
            CreateTMP(controlsPanel.transform, "ControlsBody",
                "[W A S D] - Move / Chalne ke liye   |   [Shift] - Run / Tez daudne ke liye\n" +
                "[Mouse] - Look / Nazariya ghumayein   |   [Click] - Lock cursor\n" +
                "[E] - Pick Up / Open Door / Rescue Civilian / Refill Extinguisher\n" +
                "[Left Click] (Hold) - Spray Fire Extinguisher (Aag bujhane ke liye)\n" +
                "[G] or [Q] - Drop Extinguisher   |   [Esc] - Pause Menu",
                14, FontStyles.Normal, new Color(0.92f, 0.95f, 1f), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.73f), TextAlignmentOptions.TopLeft);

            // Bottom-Right: Extinguisher Pressure
            GameObject extPanel = CreatePanel(canvasGo.transform, "ExtinguisherPanel", new Vector2(0.68f, 0.02f), new Vector2(0.985f, 0.16f), new Color(0.05f, 0.07f, 0.11f, 0.92f));
            CreateTMP(extPanel.transform, "ExtLbl", "EXTINGUISHER TANK PRESSURE", 16, FontStyles.Bold, new Color(1f, 0.82f, 0.35f), new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.92f), TextAlignmentOptions.Left);
            Slider extBar = CreateSlider(extPanel.transform, "ExtBar", 0f, 1f, 1f, new Vector2(0.05f, 0.15f), new Vector2(0.74f, 0.50f), new Color(0.98f, 0.55f, 0.15f));
            TextMeshProUGUI extTxt = CreateTMP(extPanel.transform, "ExtText", "100%", 18, FontStyles.Bold, Color.white, new Vector2(0.76f, 0.12f), new Vector2(0.96f, 0.54f));

            // Center Crosshair & Interaction Hint (Huge & crystal clear)
            CreatePanel(canvasGo.transform, "Crosshair", new Vector2(0.497f, 0.495f), new Vector2(0.503f, 0.505f), new Color(1f, 1f, 1f, 0.85f));
            GameObject hintPanel = CreatePanel(canvasGo.transform, "InteractionHintPanel", new Vector2(0.30f, 0.32f), new Vector2(0.70f, 0.42f), new Color(0.04f, 0.06f, 0.10f, 0.95f));
            TextMeshProUGUI hintTxt = CreateTMP(hintPanel.transform, "InteractionHintText", "[E] INTERACT", 24, FontStyles.Bold, new Color(1f, 0.88f, 0.35f), Vector2.zero, Vector2.one);

            // Pause Panel
            GameObject pausePanel = CreatePanel(canvasGo.transform, "PausePanel", Vector2.zero, Vector2.one, new Color(0.04f, 0.06f, 0.09f, 0.88f));
            CreateTMP(pausePanel.transform, "PauseTitle", "SIMULATION PAUSED", 34, FontStyles.Bold, Color.white, new Vector2(0.25f, 0.68f), new Vector2(0.75f, 0.80f));
            Button resBtn = CreateButton(pausePanel.transform, "ResumeBtn", "RESUME", new Color(0.18f, 0.55f, 0.42f), new Vector2(0.38f, 0.52f), new Vector2(0.62f, 0.61f));
            Button rstBtn = CreateButton(pausePanel.transform, "RestartBtn", "RESTART", new Color(0.25f, 0.42f, 0.65f), new Vector2(0.38f, 0.40f), new Vector2(0.62f, 0.49f));
            Button mmBtn = CreateButton(pausePanel.transform, "MainMenuBtn", "MAIN MENU", new Color(0.45f, 0.18f, 0.18f), new Vector2(0.38f, 0.28f), new Vector2(0.62f, 0.37f));

            // Result Panel
            GameObject resultPanel = CreatePanel(canvasGo.transform, "ResultPanel", Vector2.zero, Vector2.one, new Color(0.04f, 0.06f, 0.09f, 0.92f));
            TextMeshProUGUI resTitle = CreateTMP(resultPanel.transform, "ResultTitle", "MISSION ACCOMPLISHED", 36, FontStyles.Bold, Color.green, new Vector2(0.15f, 0.76f), new Vector2(0.85f, 0.88f));
            TextMeshProUGUI resStats = CreateTMP(resultPanel.transform, "ResultStats", "Stats", 22, FontStyles.Normal, Color.white, new Vector2(0.25f, 0.36f), new Vector2(0.75f, 0.72f));
            TextMeshProUGUI resRating = CreateTMP(resultPanel.transform, "ResultRating", "RATING: S", 30, FontStyles.Bold, new Color(1f, 0.82f, 0.2f), new Vector2(0.3f, 0.24f), new Vector2(0.7f, 0.34f));
            Button replayBtn = CreateButton(resultPanel.transform, "ReplayBtn", "REPLAY", new Color(0.18f, 0.55f, 0.42f), new Vector2(0.28f, 0.10f), new Vector2(0.48f, 0.20f));
            Button resMmBtn = CreateButton(resultPanel.transform, "ResultMainMenuBtn", "MAIN MENU", new Color(0.25f, 0.3f, 0.4f), new Vector2(0.52f, 0.10f), new Vector2(0.72f, 0.20f));

            // Wire UIManager
            var so = new SerializedObject(ui);
            so.FindProperty("healthBar").objectReferenceValue = hpBar;
            so.FindProperty("healthText").objectReferenceValue = hpTxt;
            so.FindProperty("oxygenBar").objectReferenceValue = oxBar;
            so.FindProperty("oxygenText").objectReferenceValue = oxTxt;
            so.FindProperty("timerText").objectReferenceValue = timerTxt;
            so.FindProperty("missionTitleText").objectReferenceValue = mTitle;
            so.FindProperty("missionObjectivesText").objectReferenceValue = mObj;
            so.FindProperty("rescueCounterText").objectReferenceValue = rescueTxt;
            so.FindProperty("fireStatusText").objectReferenceValue = fireTxt;
            so.FindProperty("extinguisherPanel").objectReferenceValue = extPanel;
            so.FindProperty("extinguisherPressureBar").objectReferenceValue = extBar;
            so.FindProperty("extinguisherPressureText").objectReferenceValue = extTxt;
            so.FindProperty("interactionHintPanel").objectReferenceValue = hintPanel;
            so.FindProperty("interactionHintText").objectReferenceValue = hintTxt;
            so.FindProperty("damageVignette").objectReferenceValue = vigImg;
            so.FindProperty("warningBannerText").objectReferenceValue = warnTxt;
            so.FindProperty("missionBannerPanel").objectReferenceValue = bannerPanel;
            so.FindProperty("missionBannerTitleText").objectReferenceValue = bannerTitle;
            so.FindProperty("missionBannerSubtitleText").objectReferenceValue = bannerSub;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            so.FindProperty("resultTitleText").objectReferenceValue = resTitle;
            so.FindProperty("resultStatsText").objectReferenceValue = resStats;
            so.FindProperty("resultRatingText").objectReferenceValue = resRating;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddPersistentListener(resBtn.onClick, ui.OnResumeClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(rstBtn.onClick, ui.OnRestartClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(mmBtn.onClick, ui.OnMainMenuClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(replayBtn.onClick, ui.OnRestartClicked);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(resMmBtn.onClick, ui.OnMainMenuClicked);

            return ui;
        }

        // =========================================================================
        // HELPERS
        // =========================================================================
        private static GameObject CreateBox(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool withCollider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (!withCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        private static GameObject InstantiatePrefab(GameObject prefab, Transform parent, string name, Vector3 pos, Quaternion rot)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = name;
            inst.transform.position = pos;
            inst.transform.rotation = rot;
            return inst;
        }

        private static void CreateWorldSign(Transform parent, string text, Vector3 localPos, Color color)
        {
            GameObject go = new GameObject("WorldSign");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.5f;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        }

        private static void CreateCheckpoint(Transform parent, string name, Vector3 pos, Vector3 size, CheckpointType type)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            CheckpointTrigger trig = go.AddComponent<CheckpointTrigger>();
            trig.SetType(type);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            InputSystemUIInputModule uiMod = es.AddComponent<InputSystemUIInputModule>();
            uiMod.AssignDefaultActions();
        }

        private static GameObject CreateUICanvas(string name)
        {
            GameObject go = new GameObject(name);
            Canvas c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = CreatePanel(parent, name, anchorMin, anchorMax, bgColor);
            Button btn = go.AddComponent<Button>();
            CreateTMP(go.transform, "Label", label, 20, FontStyles.Bold, Color.white, Vector2.zero, Vector2.one);
            return btn;
        }

        private static Slider CreateSlider(Transform parent, string name, float min, float max, float val, Vector2 anchorMin, Vector2 anchorMax, Color? fillColor = null)
        {
            GameObject go = CreatePanel(parent, name, anchorMin, anchorMax, new Color(0.15f, 0.18f, 0.22f, 0.9f));
            Slider slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = val;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.offsetMin = Vector2.zero;
            faRt.offsetMax = Vector2.zero;

            GameObject fill = CreatePanel(fillArea.transform, "Fill", Vector2.zero, Vector2.one, fillColor ?? new Color(0.25f, 0.7f, 0.95f));
            slider.fillRect = fill.GetComponent<RectTransform>();
            return slider;
        }

        private static Toggle CreateToggle(Transform parent, string name, string label, bool isOn, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Toggle tog = go.AddComponent<Toggle>();
            tog.isOn = isOn;

            GameObject box = CreatePanel(go.transform, "Background", new Vector2(0f, 0.15f), new Vector2(0.08f, 0.85f), new Color(0.25f, 0.3f, 0.38f));
            GameObject check = CreatePanel(box.transform, "Checkmark", new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), new Color(0.25f, 0.9f, 0.55f));
            tog.targetGraphic = box.GetComponent<Image>();
            tog.graphic = check.GetComponent<Image>();

            CreateTMP(go.transform, "Label", label, 18, FontStyles.Bold, Color.white, new Vector2(0.12f, 0f), new Vector2(1f, 1f), TextAlignmentOptions.Left);
            return tog;
        }
    }
}
#endif
