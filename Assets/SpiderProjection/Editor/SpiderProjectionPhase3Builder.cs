using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpiderProjection.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace SpiderProjection.Editor
{
    public static class SpiderProjectionPhase3Builder
    {
        private const string Root = "Assets/SpiderProjection";
        private const string WallDemoScenePath = Root + "/Scenes/WallDemo.unity";
        private const string CalibrationScenePath = Root + "/Scenes/ProjectionCalibration.unity";
        private const string LayoutPath = Root + "/Settings/ProjectionLayout_Example.asset";
        private const string SpriteMaterialPath = Root + "/Materials/SpriteUnlit.mat";
        private const string PlayerPrefabPath = Root + "/Prefabs/PF_CrimsonCrawler.prefab";
        private const string AnchorPrefabPath = Root + "/Prefabs/PF_WebAnchor.prefab";
        private const string CalibrationPrefabPath = Root + "/Prefabs/PF_ProjectionCalibration.prefab";
        private const float TargetAspect = 16f / 9f;
        private const float OneWayThickness = 0.14f;

        [MenuItem("Tools/Spider Projection/Phase 3 - Build Projection Wall")]
        public static void BuildPhase3()
        {
            ProjectionLayout layout = AssetDatabase.LoadAssetAtPath<ProjectionLayout>(LayoutPath);
            Material spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefabPath);
            GameObject calibrationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CalibrationPrefabPath);
            ValidateDependencies(layout, spriteMaterial, playerPrefab, anchorPrefab, calibrationPrefab);

            BuildWallDemo(layout, spriteMaterial, playerPrefab, anchorPrefab, calibrationPrefab);
            BuildCalibrationScene(layout, anchorPrefab, calibrationPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidatePhase3();
            Debug.Log("[SpiderProjection] Phase 3 complete: visible wall, separate mapped physics, one-way tops, anchors, and projection modes are ready.");
        }

        [MenuItem("Tools/Spider Projection/Validate Phase 3")]
        public static void ValidatePhase3Menu()
        {
            ValidatePhase3();
        }

        private static void BuildWallDemo(
            ProjectionLayout layout,
            Material spriteMaterial,
            GameObject playerPrefab,
            GameObject anchorPrefab,
            GameObject calibrationPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(layout);

            GameObject systems = new GameObject("WallDemoSystems");
            systems.AddComponent<WallDemoRuntime>();
            MusicDirector music = systems.AddComponent<MusicDirector>();
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "/Audio/SpiderProjectionMixer.mixer");
            music.Configure(
                mixer?.FindMatchingGroups("Music").FirstOrDefault(),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Music/music_projection_idle.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Music/music_wallcrawler_action.wav"));

            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (player == null)
            {
                throw new InvalidOperationException("PF_CrimsonCrawler could not be instantiated.");
            }
            player.transform.position = new Vector3(-3.25f, -3.15f, 0f);

            GameObject calibration = PrefabUtility.InstantiatePrefab(calibrationPrefab) as GameObject;
            calibration.name = "ProjectionCalibration";
            ProjectionCalibrationController controller = calibration.GetComponent<ProjectionCalibrationController>();
            controller.SetStartupModes(false, false);
            controller.SetCalibrationMode(false);

            BuildMappedLayout(layout, spriteMaterial, anchorPrefab, true);
            EditorSceneManager.SaveScene(scene, WallDemoScenePath);
        }

        private static void BuildCalibrationScene(
            ProjectionLayout layout,
            GameObject anchorPrefab,
            GameObject calibrationPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(layout);

            GameObject calibration = PrefabUtility.InstantiatePrefab(calibrationPrefab) as GameObject;
            calibration.name = "ProjectionCalibration";
            ProjectionCalibrationController controller = calibration.GetComponent<ProjectionCalibrationController>();
            controller.SetStartupModes(true, false);
            controller.SetCalibrationMode(true);

            BuildMappedLayout(layout, null, anchorPrefab, false);
            EditorSceneManager.SaveScene(scene, CalibrationScenePath);
        }

        private static void BuildMappedLayout(
            ProjectionLayout layout,
            Material spriteMaterial,
            GameObject anchorPrefab,
            bool includeDemoSprites)
        {
            GameObject demoRoot = new GameObject("DemoSprites");
            demoRoot.SetActive(includeDemoSprites);
            GameObject physicsRoot = new GameObject("MappedSurfaces");
            GameObject anchorRoot = new GameObject("WebAnchors");

            foreach (ProjectionLayout.SurfaceLayout surface in layout.surfaces)
            {
                Rect worldRect = ProjectionCoordinates.NormalizedRectToWorld(
                    surface.normalizedRect,
                    layout.orthographicSize,
                    TargetAspect);
                SpriteRenderer demoRenderer = includeDemoSprites
                    ? CreateDemoRenderer(demoRoot.transform, surface, worldRect, spriteMaterial)
                    : null;
                CreateMappedSurface(physicsRoot.transform, surface, worldRect, demoRenderer);
                if (surface.webAttachable)
                {
                    CreateAnchors(anchorRoot.transform, anchorPrefab, surface, worldRect);
                }
            }
        }

        private static SpriteRenderer CreateDemoRenderer(
            Transform parent,
            ProjectionLayout.SurfaceLayout surface,
            Rect worldRect,
            Material material)
        {
            if (string.IsNullOrEmpty(surface.visibleSprite))
            {
                return null;
            }

            string spritePath = Root + "/Art/Environment/" + surface.visibleSprite;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) ??
                            AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().FirstOrDefault();
            if (sprite == null)
            {
                throw new FileNotFoundException("Visible surface sprite was not found.", spritePath);
            }

            GameObject visual = new GameObject("Demo_" + surface.id);
            visual.transform.SetParent(parent, false);
            visual.transform.position = new Vector3(worldRect.center.x, worldRect.center.y, 0f);
            visual.transform.localScale = new Vector3(
                worldRect.width / sprite.bounds.size.x,
                worldRect.height / sprite.bounds.size.y,
                1f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 0;
            return renderer;
        }

        private static void CreateMappedSurface(
            Transform parent,
            ProjectionLayout.SurfaceLayout surface,
            Rect worldRect,
            SpriteRenderer demoRenderer)
        {
            GameObject physics = new GameObject("Mapped_" + surface.id);
            physics.transform.SetParent(parent, false);

            int layer = LayerMask.NameToLayer(surface.oneWay ? "OneWayPlatform" : "WorldSolid");
            if (layer >= 0)
            {
                physics.layer = layer;
            }

            BoxCollider2D collider = physics.AddComponent<BoxCollider2D>();
            if (surface.oneWay)
            {
                physics.transform.position = new Vector3(
                    worldRect.center.x,
                    worldRect.yMax - OneWayThickness * 0.5f,
                    0f);
                collider.size = new Vector2(worldRect.width, OneWayThickness);
                PlatformEffector2D effector = physics.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useSideFriction = false;
                effector.surfaceArc = 160f;
                collider.usedByEffector = true;
            }
            else
            {
                physics.transform.position = new Vector3(worldRect.center.x, worldRect.center.y, 0f);
                collider.size = worldRect.size;
            }

            MappedSurface2D mapped = physics.AddComponent<MappedSurface2D>();
            if (demoRenderer == null)
            {
                mapped.Configure(
                    surface.id,
                    surface.kind,
                    surface.landable,
                    surface.oneWay,
                    surface.webAttachable);
            }
            else
            {
                mapped.Configure(
                    surface.id,
                    surface.kind,
                    surface.landable,
                    surface.oneWay,
                    surface.webAttachable,
                    demoRenderer);
            }
        }

        private static void CreateAnchors(
            Transform parent,
            GameObject anchorPrefab,
            ProjectionLayout.SurfaceLayout surface,
            Rect worldRect)
        {
            float[] xPositions = { worldRect.xMin, worldRect.center.x, worldRect.xMax };
            string[] suffixes = { "left", "center", "right" };
            for (int index = 0; index < xPositions.Length; index++)
            {
                GameObject anchor = PrefabUtility.InstantiatePrefab(anchorPrefab) as GameObject;
                if (anchor == null)
                {
                    throw new InvalidOperationException("PF_WebAnchor could not be instantiated.");
                }
                anchor.name = "Anchor_" + surface.id + "_" + suffixes[index];
                anchor.transform.SetParent(parent, true);
                anchor.transform.position = new Vector3(xPositions[index], worldRect.yMax + 0.08f, 0f);
                WebAnchor2D webAnchor = anchor.GetComponent<WebAnchor2D>();
                webAnchor.Configure(
                    surface.kind == MappedSurfaceKind.Shelf || surface.kind == MappedSurfaceKind.PictureFrame
                        ? WebSurfaceType.Wood
                        : WebSurfaceType.Wall);
            }
        }

        private static Camera CreateCamera(ProjectionLayout layout)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = layout.orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = layout.clearColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void ValidateDependencies(
            ProjectionLayout layout,
            Material spriteMaterial,
            GameObject playerPrefab,
            GameObject anchorPrefab,
            GameObject calibrationPrefab)
        {
            List<string> missing = new List<string>();
            if (layout == null) missing.Add(LayoutPath);
            if (spriteMaterial == null) missing.Add(SpriteMaterialPath);
            if (playerPrefab == null) missing.Add(PlayerPrefabPath);
            if (anchorPrefab == null) missing.Add(AnchorPrefabPath);
            if (calibrationPrefab == null) missing.Add(CalibrationPrefabPath);
            if (missing.Count > 0)
            {
                throw new FileNotFoundException(
                    "Phase 3 dependencies are missing. Run Phases 1 and 2 first:\n- " +
                    string.Join("\n- ", missing));
            }
        }

        private static void ValidatePhase3()
        {
            List<string> failures = new List<string>();
            ValidateScene(WallDemoScenePath, true, failures);
            ValidateScene(CalibrationScenePath, false, failures);
            if (failures.Count > 0)
            {
                throw new InvalidDataException(
                    "[SpiderProjection] Phase 3 validation failed:\n- " +
                    string.Join("\n- ", failures));
            }

            Debug.Log("[SpiderProjection] Phase 3 validation passed: 4 visible wall sprites, 5 separate mapped surfaces, 4 one-way tops, 12 anchors, and both projection modes.");
        }

        private static void ValidateScene(string path, bool expectDemo, List<string> failures)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            MappedSurface2D[] surfaces = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MappedSurface2D>(true))
                .ToArray();
            WebAnchor2D[] anchors = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WebAnchor2D>(true))
                .ToArray();
            ProjectionCalibrationController controller = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ProjectionCalibrationController>(true))
                .FirstOrDefault();
            int oneWayCount = surfaces.Count(surface => surface.OneWay);
            int demoCount = scene.GetRootGameObjects()
                .Where(root => root.name == "DemoSprites")
                .SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .Count(renderer => renderer.name.StartsWith("Demo_", StringComparison.Ordinal));

            if (surfaces.Length != 5) failures.Add(path + " expected 5 mapped surfaces, found " + surfaces.Length + ".");
            if (anchors.Length != 12) failures.Add(path + " expected 12 anchors, found " + anchors.Length + ".");
            if (oneWayCount != 4) failures.Add(path + " expected 4 one-way surfaces, found " + oneWayCount + ".");
            if (controller == null) failures.Add(path + " is missing ProjectionCalibrationController.");
            if (expectDemo && demoCount != 4) failures.Add(path + " expected 4 demo sprites, found " + demoCount + ".");
            if (!expectDemo && demoCount != 0) failures.Add(path + " should not contain demo sprites.");
        }
    }
}
