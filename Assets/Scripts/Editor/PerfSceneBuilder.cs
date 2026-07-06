#nullable enable
using RagsToRiches.View;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RagsToRiches.Editor
{
    /// <summary>
    /// Стадия 0: автоматическая настройка URP и сборка тестовой сцены
    /// производительности (15 low-poly машин, 10 NPC с NavMeshAgent,
    /// капсула-игрок, изометрическая камера, FPS-оверлей).
    /// Меню: RagsToRiches → 1, затем 2. Скрипт идемпотентен — можно запускать повторно.
    /// </summary>
    public static class PerfSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PerfTest.unity";
        private const string SettingsFolder = "Assets/Settings";
        private const string RendererDataPath = SettingsFolder + "/URP-Renderer.asset";
        private const string PipelineAssetPath = SettingsFolder + "/URP-Pipeline.asset";
        private const string UrpPackagePath = "Packages/com.unity.render-pipelines.universal";

        private const int CarCount = 15;
        private const int NpcCount = 10;
        private const float GroundHalfSize = 28f;

        private static readonly Color[] CarPalette =
        {
            new(0.85f, 0.25f, 0.20f), new(0.20f, 0.45f, 0.85f), new(0.95f, 0.75f, 0.20f),
            new(0.30f, 0.70f, 0.35f), new(0.90f, 0.90f, 0.92f), new(0.25f, 0.25f, 0.30f)
        };

        [MenuItem("RagsToRiches/1. Настроить URP")]
        public static void SetupUrp()
        {
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets", "Settings");

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                ResourceReloader.ReloadAllNullIn(rendererData, UrpPackagePath);
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);

                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                var serialized = new SerializedObject(pipeline);
                SerializedProperty list = serialized.FindProperty("m_RendererDataList");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage0] URP настроен: {PipelineAssetPath}");
        }

        [MenuItem("RagsToRiches/2. Собрать перф-сцену")]
        public static void BuildPerfScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SetupUrp();

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Random.InitState(42);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);

            CreateLight();
            CreateGround();
            CreateCars();
            BakeNavMesh(); // до создания игрока и NPC — иначе их капсулы запекутся как препятствия
            GameObject player = CreatePlayer();
            CreateNpcs();
            CreateCamera(player.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[Stage0] Сцена собрана и сохранена: {ScenePath}. Нажмите Play для проверки.");
        }

        private static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft; // худший случай для перф-теста
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateGround()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(GroundHalfSize / 5f, 1f, GroundHalfSize / 5f);
            ground.isStatic = true;
            ApplyMaterial(ground, "Mat_Ground", new Color(0.42f, 0.47f, 0.42f));
        }

        private static void CreateCars()
        {
            var root = new GameObject("Cars");
            for (int i = 0; i < CarCount; i++)
            {
                Vector2 pos2 = RandomPointOnRing(minRadius: 7f, maxRadius: GroundHalfSize - 4f);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = $"Car_{i:00}";
                body.transform.SetParent(root.transform);
                body.transform.position = new Vector3(pos2.x, 0.7f, pos2.y);
                body.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                body.transform.localScale = new Vector3(1.8f, 1.4f, 4.2f);
                body.isStatic = true;

                var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cabin.name = "Cabin";
                cabin.transform.SetParent(body.transform);
                cabin.transform.localPosition = new Vector3(0f, 0.65f, -0.1f);
                cabin.transform.localRotation = Quaternion.identity;
                cabin.transform.localScale = new Vector3(0.9f, 0.5f, 0.5f);
                cabin.isStatic = true;

                Color color = CarPalette[i % CarPalette.Length];
                string matName = $"Mat_Car_{i % CarPalette.Length}";
                ApplyMaterial(body, matName, color);
                ApplyMaterial(cabin, matName, color);
            }
        }

        private static void BakeNavMesh()
        {
            var go = new GameObject("NavMeshSurface");
            var surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.transform.position = new Vector3(0f, 1.1f, 0f);
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerController>();
            ApplyMaterial(player, "Mat_Player", new Color(0.25f, 0.55f, 0.95f));
            return player;
        }

        private static void CreateNpcs()
        {
            var root = new GameObject("NPCs");
            for (int i = 0; i < NpcCount; i++)
            {
                Vector2 pos2 = RandomPointOnRing(minRadius: 5f, maxRadius: GroundHalfSize - 6f);
                GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                npc.name = $"NPC_{i:00}";
                npc.transform.SetParent(root.transform);
                npc.transform.position = new Vector3(pos2.x, 1f, pos2.y);
                Object.DestroyImmediate(npc.GetComponent<CapsuleCollider>());

                var agent = npc.AddComponent<NavMeshAgent>();
                agent.speed = 3.5f;
                agent.angularSpeed = 720f;
                agent.baseOffset = 1f; // pivot капсулы в центре, высота 2
                npc.AddComponent<NpcWanderer>();
                ApplyMaterial(npc, "Mat_Npc", new Color(0.95f, 0.55f, 0.25f));
            }
        }

        private static void CreateCamera(Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 45f;
            go.AddComponent<AudioListener>();
            go.AddComponent<FpsCounter>();
            var follow = go.AddComponent<IsometricCameraFollow>();
            follow.SetTarget(target);
        }

        private static Vector2 RandomPointOnRing(float minRadius, float maxRadius)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minRadius, maxRadius);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static void ApplyMaterial(GameObject go, string materialName, Color color)
        {
            string path = $"{SettingsFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color);
                AssetDatabase.CreateAsset(material, path);
            }

            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
