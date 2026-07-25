using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SpiderProjection.Runtime;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpiderProjection.Editor
{
    public static class SpiderProjectionPhase2Builder
    {
        private const string Root = "Assets/SpiderProjection";
        private const string SettingsRoot = Root + "/Settings";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string SceneRoot = Root + "/Scenes";
        private const string MaterialRoot = Root + "/Materials";
        private const string InputActionsPath = SettingsRoot + "/SpiderProjection.inputactions";
        private const string TuningPath = SettingsRoot + "/PlayerTuning.asset";
        private const string LayoutPath = SettingsRoot + "/ProjectionLayout_Example.asset";
        private const string SpriteMaterialPath = MaterialRoot + "/SpriteUnlit.mat";
        private const string WebMaterialPath = MaterialRoot + "/WebLineUnlit.mat";
        private const string PlayerPrefabPath = PrefabRoot + "/PF_CrimsonCrawler.prefab";
        private const string AnchorPrefabPath = PrefabRoot + "/PF_WebAnchor.prefab";
        private const string SurfacePrefabPath = PrefabRoot + "/PF_MappedSurface.prefab";
        private const string CalibrationPrefabPath = PrefabRoot + "/PF_ProjectionCalibration.prefab";
        private const string WallDemoScenePath = SceneRoot + "/WallDemo.unity";
        private const string CalibrationScenePath = SceneRoot + "/ProjectionCalibration.unity";

        [MenuItem("Tools/Spider Projection/Phase 2 - Build Runtime Vertical Slice")]
        public static void BuildPhase2()
        {
            EnsureFolder(SettingsRoot);
            EnsureFolder(PrefabRoot);
            EnsureFolder(SceneRoot);
            EnsureFolder(MaterialRoot);

            PlayerTuning tuning = CreateTuning();
            InputActionAsset inputActions = CreateInputActions();
            ProjectionLayout layout = CreateProjectionLayout();
            Material spriteMaterial = CreateMaterial(
                SpriteMaterialPath,
                "Universal Render Pipeline/2D/Sprite-Unlit-Default",
                "Sprites/Default");
            Material webMaterial = CreateMaterial(
                WebMaterialPath,
                "Universal Render Pipeline/Unlit",
                "Sprites/Default");

            CreateAnchorPrefab(spriteMaterial);
            CreateMappedSurfacePrefab();
            CreateCalibrationPrefab(layout, spriteMaterial);
            CreatePlayerPrefab(tuning, inputActions, spriteMaterial, webMaterial);
            CreateWallDemoScene();
            CreateCalibrationScene();
            PreserveAndAppendBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ValidatePhase2();
            Debug.Log("[SpiderProjection] Phase 2 complete: runtime modules, input actions, prefabs, and base scenes are ready.");
        }

        [MenuItem("Tools/Spider Projection/Validate Phase 2")]
        public static void ValidatePhase2Menu()
        {
            ValidatePhase2();
        }

        private static PlayerTuning CreateTuning()
        {
            PlayerTuning tuning = AssetDatabase.LoadAssetAtPath<PlayerTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<PlayerTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }
            EditorUtility.SetDirty(tuning);
            return tuning;
        }

        private static InputActionAsset CreateInputActions()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "SpiderProjection";
            InputActionMap gameplay = asset.AddActionMap("Gameplay");

            InputAction move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", "Keyboard&Mouse")
                .With("Down", "<Keyboard>/s", "Keyboard&Mouse")
                .With("Left", "<Keyboard>/a", "Keyboard&Mouse")
                .With("Right", "<Keyboard>/d", "Keyboard&Mouse");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", "Keyboard&Mouse")
                .With("Down", "<Keyboard>/downArrow", "Keyboard&Mouse")
                .With("Left", "<Keyboard>/leftArrow", "Keyboard&Mouse")
                .With("Right", "<Keyboard>/rightArrow", "Keyboard&Mouse");
            move.AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
            move.AddBinding("<Gamepad>/dpad", groups: "Gamepad");
            move.AddBinding("<Joystick>/stick", groups: "Joystick");

            InputAction aim = gameplay.AddAction("Aim", InputActionType.Value, expectedControlLayout: "Vector2");
            aim.AddBinding("<Pointer>/delta", processors: "ScaleVector2(x=0.05,y=0.05)", groups: "Keyboard&Mouse");
            aim.AddBinding("<Gamepad>/rightStick", groups: "Gamepad");
            aim.AddBinding("<Joystick>/{Hatswitch}", groups: "Joystick");

            AddButton(gameplay, "Jump", "Press", ("<Keyboard>/space", "Keyboard&Mouse"), ("<Gamepad>/buttonSouth", "Gamepad"), ("<Joystick>/trigger", "Joystick"), ("<Joystick>/button1", "Joystick"));
            AddButton(gameplay, "Swing", "Press", ("<Mouse>/leftButton", "Keyboard&Mouse"), ("<Keyboard>/f", "Keyboard&Mouse"), ("<Gamepad>/rightTrigger", "Gamepad"), ("<Joystick>/button0", "Joystick"));
            AddButton(gameplay, "Roll", "Press", ("<Keyboard>/leftCtrl", "Keyboard&Mouse"), ("<Keyboard>/c", "Keyboard&Mouse"), ("<Gamepad>/buttonEast", "Gamepad"), ("<Joystick>/button2", "Joystick"));
            AddButton(gameplay, "WebZip", "Press", ("<Keyboard>/q", "Keyboard&Mouse"), ("<Gamepad>/leftShoulder", "Gamepad"), ("<Joystick>/button4", "Joystick"));
            AddButton(gameplay, "Interact", "Press", ("<Keyboard>/e", "Keyboard&Mouse"), ("<Gamepad>/buttonNorth", "Gamepad"), ("<Joystick>/button3", "Joystick"));
            AddButton(gameplay, "Pause", "Press", ("<Keyboard>/escape", "Keyboard&Mouse"), ("<Gamepad>/start", "Gamepad"), ("<Joystick>/button9", "Joystick"));
            AddButton(gameplay, "Reset", "Hold(duration=0.5)", ("<Keyboard>/r", "Keyboard&Mouse"), ("<Gamepad>/select", "Gamepad"), ("<Joystick>/button8", "Joystick"));
            InputAction calibration = AddButton(
                gameplay,
                "ToggleCalibration",
                "Hold(duration=0.75)",
                ("<Keyboard>/f1", "Keyboard&Mouse"));
            calibration.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Gamepad>/select", "Gamepad")
                .With("Binding", "<Gamepad>/start", "Gamepad");
            calibration.AddCompositeBinding("OneModifier")
                .With("Modifier", "<Joystick>/button8", "Joystick")
                .With("Binding", "<Joystick>/button9", "Joystick");

            asset.AddControlScheme("Keyboard&Mouse")
                .WithRequiredDevice("<Keyboard>")
                .WithOptionalDevice("<Mouse>");
            asset.AddControlScheme("Gamepad")
                .WithRequiredDevice("<Gamepad>");
            asset.AddControlScheme("Joystick")
                .WithRequiredDevice("<Joystick>");

            string absolutePath = Path.GetFullPath(InputActionsPath);
            File.WriteAllText(absolutePath, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceSynchronousImport);

            InputActionAsset imported = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (imported == null || imported.FindActionMap("Gameplay") == null)
            {
                throw new InvalidDataException("Spider Projection InputActionAsset failed to import.");
            }
            return imported;
        }

        private static InputAction AddButton(
            InputActionMap map,
            string name,
            string interactions,
            params (string path, string group)[] bindings)
        {
            InputAction action = map.AddAction(
                name,
                InputActionType.Button,
                interactions: interactions,
                expectedControlLayout: "Button");
            foreach ((string path, string group) binding in bindings)
            {
                action.AddBinding(binding.path, groups: binding.group);
            }
            return action;
        }

        private static ProjectionLayout CreateProjectionLayout()
        {
            ProjectionLayout layout = AssetDatabase.LoadAssetAtPath<ProjectionLayout>(LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<ProjectionLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }

            layout.layoutName = "Example Wall - Replace With Measured Values";
            layout.outputResolution = new Vector2Int(1920, 1080);
            layout.displayIndex = 0;
            layout.fullScreen = true;
            layout.orthographicSize = 5.4f;
            layout.clearColor = Color.black;
            layout.surfaces = new List<ProjectionLayout.SurfaceLayout>
            {
                Surface("floor", MappedSurfaceKind.Ground, new Rect(0f, 0f, 1f, 0.035f), true, false, false, null),
                Surface("frame_left", MappedSurfaceKind.PictureFrame, new Rect(0.10f, 0.56f, 0.18f, 0.24f), true, true, true, "frame_0.png"),
                Surface("frame_center", MappedSurfaceKind.PictureFrame, new Rect(0.40f, 0.67f, 0.20f, 0.23f), true, true, true, "frame_1.png"),
                Surface("frame_right", MappedSurfaceKind.PictureFrame, new Rect(0.72f, 0.50f, 0.18f, 0.27f), true, true, true, "frame_2.png"),
                Surface("shelf", MappedSurfaceKind.Shelf, new Rect(0.23f, 0.32f, 0.38f, 0.045f), true, true, true, "shelf_long.png")
            };
            EditorUtility.SetDirty(layout);
            return layout;
        }

        private static ProjectionLayout.SurfaceLayout Surface(
            string id,
            MappedSurfaceKind kind,
            Rect rect,
            bool landable,
            bool oneWay,
            bool webAttachable,
            string sprite)
        {
            return new ProjectionLayout.SurfaceLayout
            {
                id = id,
                kind = kind,
                normalizedRect = rect,
                landable = landable,
                oneWay = oneWay,
                webAttachable = webAttachable,
                visibleSprite = sprite
            };
        }

        private static Material CreateMaterial(string path, string preferredShader, string fallbackShader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(preferredShader) ?? Shader.Find(fallbackShader);
            if (shader == null)
            {
                throw new InvalidOperationException("Required unlit shader was not found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            material.color = Color.white;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreatePlayerPrefab(
            PlayerTuning tuning,
            InputActionAsset inputActions,
            Material spriteMaterial,
            Material webMaterial)
        {
            GameObject root = new GameObject("PF_CrimsonCrawler");
            try
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                {
                    root.layer = playerLayer;
                }

                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = tuning.normalGravityScale;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;

                CapsuleCollider2D capsule = root.AddComponent<CapsuleCollider2D>();
                capsule.size = new Vector2(0.82f, 1.65f);
                capsule.offset = new Vector2(0f, 0.25f);

                DistanceJoint2D joint = root.AddComponent<DistanceJoint2D>();
                joint.autoConfigureDistance = false;
                joint.enableCollision = false;
                joint.enabled = false;

                PlayerInput playerInput = root.AddComponent<PlayerInput>();
                playerInput.actions = inputActions;
                playerInput.defaultActionMap = "Gameplay";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                root.AddComponent<PlayerInputReader>();
                root.AddComponent<PlayerSensors2D>();
                root.AddComponent<SpiderMotor2D>();
                root.AddComponent<SwingController2D>();
                root.AddComponent<WallTraversal2D>();
                WebTargetResolver2D resolver = root.AddComponent<WebTargetResolver2D>();
                int collisionMask = LayerMask.GetMask("WorldSolid", "OneWayPlatform");
                resolver.Configure(collisionMask);
                PlayerBrain brain = root.AddComponent<PlayerBrain>();
                brain.Configure(tuning, collisionMask);

                AudioSource playerSource = root.AddComponent<AudioSource>();
                playerSource.playOnAwake = false;
                PlayerAudioView audioView = root.AddComponent<PlayerAudioView>();
                ConfigurePlayerAudio(audioView);

                GameObject visual = Child(root, "Visual", Vector3.zero);
                SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = LoadFirstSprite(Root + "/Art/Character/crimson_crawler_idle.png");
                spriteRenderer.sharedMaterial = spriteMaterial;
                spriteRenderer.sortingOrder = 20;
                Animator animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Animations/CrimsonCrawler.controller");
                animator.applyRootMotion = false;
                visual.AddComponent<AnimationDriver2D>();

                Child(root, "WristSocket", new Vector3(0.45f, 0.55f, 0f));
                Child(root, "GroundProbe", new Vector3(0f, -0.62f, 0f));
                Child(root, "WallProbe", new Vector3(0.48f, 0.22f, 0f));
                Child(root, "LedgeProbe", new Vector3(0.48f, 1.05f, 0f));

                GameObject webLineObject = Child(root, "WebLine", Vector3.zero);
                LineRenderer line = webLineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = webMaterial;
                line.widthMultiplier = 0.045f;
                line.numCapVertices = 2;
                line.textureMode = LineTextureMode.Stretch;
                line.sortingOrder = 15;
                webLineObject.AddComponent<WebLineView2D>();

                GameObject debug = Child(root, "Debug", Vector3.zero);
                AnchorDebugView2D debugView = debug.AddComponent<AnchorDebugView2D>();
                GameObject marker = Child(debug, "CandidateMarker", Vector3.zero);
                SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
                markerRenderer.sprite = LoadSingleSprite(Root + "/Art/VFX/anchor_debug.png");
                markerRenderer.sharedMaterial = spriteMaterial;
                markerRenderer.sortingOrder = 100;
                marker.transform.localScale = Vector3.one * 0.35f;
                markerRenderer.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigurePlayerAudio(PlayerAudioView audioView)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "/Audio/SpiderProjectionMixer.mixer");
            AudioMixerGroup output = mixer?.FindMatchingGroups("SFX").FirstOrDefault();
            audioView.Configure(
                output,
                LoadAudio("sfx_jump"),
                LoadAudio("sfx_land_soft"),
                LoadAudio("sfx_land_hard"),
                LoadAudio("sfx_roll"),
                LoadAudio("sfx_skid"),
                LoadAudio("sfx_wall_crawl"),
                LoadAudio("sfx_web_release"),
                LoadAudio("sfx_pause_open"),
                LoadAudio("sfx_pause_close"),
                new[] { LoadAudio("sfx_web_shoot_01"), LoadAudio("sfx_web_shoot_02"), LoadAudio("sfx_web_shoot_03") },
                new[] { LoadAudio("sfx_web_attach_wall"), LoadAudio("sfx_web_attach_wood"), LoadAudio("sfx_web_attach_metal") });
        }

        private static void CreateAnchorPrefab(Material spriteMaterial)
        {
            GameObject root = new GameObject("PF_WebAnchor");
            try
            {
                int layer = LayerMask.NameToLayer("WebAnchor");
                if (layer >= 0)
                {
                    root.layer = layer;
                }
                WebAnchor2D anchor = root.AddComponent<WebAnchor2D>();
                anchor.Configure(WebSurfaceType.Wall);
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = LoadSingleSprite(Root + "/Art/VFX/anchor_debug.png");
                renderer.sharedMaterial = spriteMaterial;
                renderer.sortingOrder = 80;
                renderer.color = new Color(0.35f, 0.95f, 1f, 0.9f);
                root.transform.localScale = Vector3.one * 0.24f;
                PrefabUtility.SaveAsPrefabAsset(root, AnchorPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateMappedSurfacePrefab()
        {
            GameObject root = new GameObject("PF_MappedSurface");
            try
            {
                int layer = LayerMask.NameToLayer("WorldSolid");
                if (layer >= 0)
                {
                    root.layer = layer;
                }
                BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
                collider.size = Vector2.one;
                MappedSurface2D mapped = root.AddComponent<MappedSurface2D>();
                mapped.Configure("surface", MappedSurfaceKind.Wall, true, false, false);
                PrefabUtility.SaveAsPrefabAsset(root, SurfacePrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCalibrationPrefab(ProjectionLayout layout, Material material)
        {
            GameObject root = new GameObject("PF_ProjectionCalibration");
            try
            {
                ProjectionCalibrationController controller = root.AddComponent<ProjectionCalibrationController>();
                SpriteRenderer calibration = CreateOverlay(
                    root,
                    "CalibrationImage",
                    Root + "/Art/Projection/projection_calibration_1920x1080.png",
                    material,
                    -100);
                SpriteRenderer blackout = CreateOverlay(
                    root,
                    "BlackoutImage",
                    Root + "/Art/Projection/projection_blackout_1920x1080.png",
                    material,
                    500);
                SpriteRenderer safeArea = CreateOverlay(
                    root,
                    "SafeAreaOverlay",
                    Root + "/Art/Projection/projection_safe_area_overlay.png",
                    material,
                    -90);
                blackout.enabled = false;
                TextAsset exampleJson = AssetDatabase.LoadAssetAtPath<TextAsset>(
                    Root + "/Documentation/projection_layout.example.json");
                controller.Configure(layout, exampleJson, calibration, blackout, safeArea);
                controller.SetCalibrationMode(true);
                PrefabUtility.SaveAsPrefabAsset(root, CalibrationPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer CreateOverlay(
            GameObject root,
            string name,
            string spritePath,
            Material material,
            int sortingOrder)
        {
            GameObject child = Child(root, name, Vector3.zero);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSingleSprite(spritePath);
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            child.transform.localScale = Vector3.one * 0.32f;
            return renderer;
        }

        private static void CreateWallDemoScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(5.4f);

            GameObject systems = new GameObject("WallDemoSystems");
            systems.AddComponent<WallDemoRuntime>();
            MusicDirector music = systems.AddComponent<MusicDirector>();
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Root + "/Audio/SpiderProjectionMixer.mixer");
            music.Configure(
                mixer?.FindMatchingGroups("Music").FirstOrDefault(),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Music/music_projection_idle.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Music/music_wallcrawler_action.wav"));

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.transform.position = new Vector3(-3.25f, -3.15f, 0f);

            CreateBasicSurface("Floor", new Vector2(0f, -4.65f), new Vector2(20f, 1f));
            CreateBasicSurface("LeftWall", new Vector2(-9.5f, 0f), new Vector2(1f, 10f));
            CreateBasicSurface("RightWall", new Vector2(9.5f, 0f), new Vector2(1f, 10f));

            GameObject anchorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnchorPrefabPath);
            foreach (Vector2 position in new[] { new Vector2(-4f, 3.4f), new Vector2(1f, 4.2f), new Vector2(6f, 3.2f) })
            {
                GameObject anchor = PrefabUtility.InstantiatePrefab(anchorPrefab) as GameObject;
                anchor.name = "WebAnchor";
                anchor.transform.position = position;
            }

            EditorSceneManager.SaveScene(scene, WallDemoScenePath);
        }

        private static void CreateCalibrationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(5.4f);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CalibrationPrefabPath);
            PrefabUtility.InstantiatePrefab(prefab);
            EditorSceneManager.SaveScene(scene, CalibrationScenePath);
        }

        private static Camera CreateCamera(float orthographicSize)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void CreateBasicSurface(string name, Vector2 position, Vector2 size)
        {
            GameObject surface = new GameObject(name);
            surface.transform.position = position;
            int layer = LayerMask.NameToLayer("WorldSolid");
            if (layer >= 0)
            {
                surface.layer = layer;
            }
            BoxCollider2D collider = surface.AddComponent<BoxCollider2D>();
            collider.size = size;
            MappedSurface2D mapped = surface.AddComponent<MappedSurface2D>();
            mapped.Configure(name.ToLowerInvariant(), name == "Floor" ? MappedSurfaceKind.Ground : MappedSurfaceKind.Wall, true, false, false);
        }

        private static void PreserveAndAppendBuildScenes()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { WallDemoScenePath, CalibrationScenePath })
            {
                if (scenes.All(scene => scene.path != path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidatePhase2()
        {
            List<string> failures = new List<string>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            InputActionMap gameplay = actions?.FindActionMap("Gameplay");
            if (gameplay == null || gameplay.actions.Count != 10)
            {
                failures.Add("Gameplay action map is missing or does not contain 10 actions.");
            }
            if (actions == null || actions.controlSchemes.Count != 3)
            {
                failures.Add("Keyboard&Mouse, Gamepad, and Joystick schemes were not all created.");
            }

            foreach (string path in new[] { PlayerPrefabPath, AnchorPrefabPath, SurfacePrefabPath, CalibrationPrefabPath })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    failures.Add("Prefab missing: " + path);
                }
            }

            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (player == null ||
                player.GetComponent<PlayerBrain>() == null ||
                player.GetComponent<DistanceJoint2D>() == null ||
                player.GetComponent<PlayerInput>() == null ||
                player.GetComponentInChildren<AnimationDriver2D>(true) == null)
            {
                failures.Add("PF_CrimsonCrawler component graph is incomplete.");
            }

            foreach (string path in new[] { WallDemoScenePath, CalibrationScenePath })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                {
                    failures.Add("Scene missing: " + path);
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidDataException("[SpiderProjection] Phase 2 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("[SpiderProjection] Phase 2 validation passed: 10 actions, 3 schemes, 4 prefabs, 2 scenes, and the complete player component graph.");
        }

        private static AudioClip LoadAudio(string name)
        {
            string guid = AssetDatabase.FindAssets(name + " t:AudioClip", new[] { Root + "/Audio" }).FirstOrDefault();
            return string.IsNullOrEmpty(guid)
                ? null
                : AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static Sprite LoadFirstSprite(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name).FirstOrDefault();
        }

        private static Sprite LoadSingleSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
                   AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static GameObject Child(GameObject parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = localPosition;
            return child;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Replace('\\', '/').Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }
                current = next;
            }
        }
    }
}
