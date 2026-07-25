using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiderProjection.Editor
{
    public static class SpiderProjectionAssetBuilder
    {
        private const string Root = "Assets/SpiderProjection";
        private const string ArtRoot = Root + "/Art";
        private const string CharacterRoot = ArtRoot + "/Character";
        private const string AnimationRoot = Root + "/Animations";
        private const string ClipRoot = AnimationRoot + "/Clips";
        private const string AnimatorSpecPath = Root + "/Documentation/animator_state_machine_spec.json";
        private const string ControllerPath = AnimationRoot + "/CrimsonCrawler.controller";
        private const string MixerPath = Root + "/Audio/SpiderProjectionMixer.mixer";
        private static readonly Vector2 CharacterPivot = new Vector2(0.5f, 0.32f);

        [Serializable]
        private sealed class AnimatorSpec
        {
            public string controller_name;
            public string default_state;
            public AnimatorParameterSpec[] parameters;
            public AnimatorStateSpec[] states;
        }

        [Serializable]
        private sealed class AnimatorParameterSpec
        {
            public string name;
            public string type;
            public float @default;
        }

        [Serializable]
        private sealed class AnimatorStateSpec
        {
            public string name;
            public string strip;
            public int frames;
            public float fps;
            public bool loop;
        }

        [MenuItem("Tools/Spider Projection/Phase 1 - Build Presentation Assets")]
        public static void BuildPhase1()
        {
            try
            {
                EnsureFolder(AnimationRoot);
                EnsureFolder(ClipRoot);

                AnimatorSpec spec = LoadAnimatorSpec();
                ConfigureAllTextures(spec);
                Dictionary<string, AnimationClip> clips = BuildAnimationClips(spec);
                BuildAnimatorController(spec, clips);
                ConfigureAudioImporters();
                BuildAudioMixer();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidatePhase1(spec);
                Debug.Log("[SpiderProjection] Phase 1 complete: sprite importers, 21 clips, Animator, audio importers, and mixer are ready.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Tools/Spider Projection/Validate Phase 1")]
        public static void ValidatePhase1Menu()
        {
            ValidatePhase1(LoadAnimatorSpec());
        }

        private static AnimatorSpec LoadAnimatorSpec()
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(AnimatorSpecPath);
            if (asset == null)
            {
                throw new FileNotFoundException("Animator state-machine spec was not imported.", AnimatorSpecPath);
            }

            AnimatorSpec spec = JsonUtility.FromJson<AnimatorSpec>(asset.text);
            if (spec == null || spec.states == null || spec.states.Length != 21)
            {
                throw new InvalidDataException("Animator spec must contain exactly 21 states.");
            }

            return spec;
        }

        private static void ConfigureAllTextures(AnimatorSpec spec)
        {
            HashSet<string> characterStrips = new HashSet<string>(
                spec.states.Select(state => ResolveSpecPath(AnimatorSpecPath, state.strip)),
                StringComparer.Ordinal);

            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (characterStrips.Contains(path))
                {
                    AnimatorStateSpec state = spec.states.First(candidate =>
                        string.Equals(ResolveSpecPath(AnimatorSpecPath, candidate.strip), path, StringComparison.Ordinal));
                    ConfigureTexture(path, SpriteImportMode.Multiple);
                    SliceGrid(path, 64, 64, state.frames, CharacterPivot, state.name);
                    continue;
                }

                if (path == CharacterRoot + "/crimson_crawler_atlas.png")
                {
                    ConfigureTexture(path, SpriteImportMode.Multiple);
                    SliceGrid(path, 64, 64, 88, CharacterPivot, "Atlas");
                    continue;
                }

                if (path.EndsWith("/wall_obstacle_atlas.png", StringComparison.Ordinal))
                {
                    ConfigureTexture(path, SpriteImportMode.Multiple);
                    SliceGrid(path, 128, 128, 16, new Vector2(0.5f, 0.5f), "Obstacle");
                    continue;
                }

                if (path.EndsWith("/vfx_atlas.png", StringComparison.Ordinal))
                {
                    ConfigureTexture(path, SpriteImportMode.Multiple);
                    SliceGrid(path, 64, 64, 12, new Vector2(0.5f, 0.5f), "Vfx");
                    continue;
                }

                if (path.EndsWith("/input_glyphs.png", StringComparison.Ordinal))
                {
                    ConfigureTexture(path, SpriteImportMode.Multiple);
                    SliceGrid(path, 96, 64, 12, new Vector2(0.5f, 0.5f), "Glyph");
                    continue;
                }

                ConfigureTexture(path, SpriteImportMode.Single);
            }
        }

        private static void ConfigureTexture(string path, SpriteImportMode mode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("TextureImporter missing for " + path);
            }

            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != mode ||
                           Math.Abs(importer.spritePixelsPerUnit - 32f) > 0.001f ||
                           importer.filterMode != FilterMode.Point ||
                           importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           importer.mipmapEnabled ||
                           importer.wrapMode != TextureWrapMode.Clamp ||
                           !importer.alphaIsTransparency;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = mode;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;

            TextureImporterSettings textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            if (mode == SpriteImportMode.Single)
            {
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                textureSettings.spriteAlignment = (int)SpriteAlignment.Center;
            }
            importer.SetTextureSettings(textureSettings);

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SliceGrid(
            string path,
            int cellWidth,
            int cellHeight,
            int requestedFrames,
            Vector2 pivot,
            string namePrefix)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("TextureImporter missing for " + path);
            }

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null)
            {
                throw new InvalidOperationException("Sprite data provider missing for " + path);
            }

            provider.InitSpriteEditorDataProvider();
            ITextureDataProvider textureProvider = provider.GetDataProvider<ITextureDataProvider>();
            textureProvider.GetTextureActualWidthAndHeight(out int width, out int height);

            int columns = width / cellWidth;
            int rows = height / cellHeight;
            int frameCount = Mathf.Min(requestedFrames, columns * rows);
            SpriteRect[] existing = provider.GetSpriteRects();
            Dictionary<string, GUID> priorIds = existing.ToDictionary(rect => rect.name, rect => rect.spriteID);
            List<SpriteRect> rects = new List<SpriteRect>(frameCount);

            for (int index = 0; index < frameCount; index++)
            {
                int column = index % columns;
                int rowFromTop = index / columns;
                int y = height - ((rowFromTop + 1) * cellHeight);
                string spriteName = namePrefix + "_" + index.ToString("D2");
                GUID spriteId = priorIds.TryGetValue(spriteName, out GUID priorId) ? priorId : GUID.Generate();

                rects.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(column * cellWidth, y, cellWidth, cellHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    border = Vector4.zero,
                    spriteID = spriteId
                });
            }

            provider.SetSpriteRects(rects.ToArray());
            ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameProvider != null)
            {
                nameProvider.SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            }

            provider.Apply();
            importer.SaveAndReimport();
        }

        private static Dictionary<string, AnimationClip> BuildAnimationClips(AnimatorSpec spec)
        {
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            foreach (AnimatorStateSpec state in spec.states)
            {
                string stripPath = ResolveSpecPath(AnimatorSpecPath, state.strip);
                Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(stripPath)
                    .OfType<Sprite>()
                    .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                    .Take(state.frames)
                    .ToArray();

                if (sprites.Length != state.frames)
                {
                    throw new InvalidDataException(
                        $"{state.name} expected {state.frames} sprites at {stripPath}, found {sprites.Length}.");
                }

                string clipPath = $"{ClipRoot}/{state.name}.anim";
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    clip = new AnimationClip { name = state.name };
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                clip.frameRate = state.fps;
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    path = string.Empty,
                    type = typeof(SpriteRenderer),
                    propertyName = "m_Sprite"
                };
                ObjectReferenceKeyframe[] keyframes = sprites
                    .Select((sprite, index) => new ObjectReferenceKeyframe
                    {
                        time = index / state.fps,
                        value = sprite
                    })
                    .ToArray();
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                SerializedObject serializedClip = new SerializedObject(clip);
                SerializedProperty loopTime = serializedClip.FindProperty("m_AnimationClipSettings.m_LoopTime");
                if (loopTime != null)
                {
                    loopTime.boolValue = state.loop;
                }
                serializedClip.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(clip);
                clips.Add(state.name, clip);
            }

            return clips;
        }

        private static void BuildAnimatorController(AnimatorSpec spec, IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            foreach (AnimatorParameterSpec parameterSpec in spec.parameters)
            {
                AnimatorControllerParameter parameter = new AnimatorControllerParameter
                {
                    name = parameterSpec.name,
                    type = ParseParameterType(parameterSpec.type),
                    defaultBool = parameterSpec.@default > 0.5f,
                    defaultFloat = parameterSpec.@default,
                    defaultInt = Mathf.RoundToInt(parameterSpec.@default)
                };
                controller.AddParameter(parameter);
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                stateMachine.RemoveState(child.state);
            }

            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            AnimatorState defaultState = null;
            for (int index = 0; index < spec.states.Length; index++)
            {
                AnimatorStateSpec stateSpec = spec.states[index];
                Vector3 position = new Vector3((index % 4) * 240f, (index / 4) * 80f, 0f);
                AnimatorState state = stateMachine.AddState(stateSpec.name, position);
                state.motion = clips[stateSpec.name];
                state.writeDefaultValues = false;
                if (stateSpec.name == spec.default_state)
                {
                    defaultState = state;
                }
            }

            stateMachine.defaultState = defaultState;
            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
        }

        private static AnimatorControllerParameterType ParseParameterType(string type)
        {
            switch (type)
            {
                case "Float":
                    return AnimatorControllerParameterType.Float;
                case "Int":
                    return AnimatorControllerParameterType.Int;
                case "Bool":
                    return AnimatorControllerParameterType.Bool;
                case "Trigger":
                    return AnimatorControllerParameterType.Trigger;
                default:
                    throw new InvalidDataException("Unsupported Animator parameter type: " + type);
            }
        }

        private static void ConfigureAudioImporters()
        {
            ConfigureAudioFolder(Root + "/Audio/SFX", AudioClipLoadType.DecompressOnLoad, AudioCompressionFormat.PCM, 1f, true);
            ConfigureAudioFolder(Root + "/Audio/Music", AudioClipLoadType.Streaming, AudioCompressionFormat.Vorbis, 0.7f, false);
        }

        private static void ConfigureAudioFolder(
            string folder,
            AudioClipLoadType loadType,
            AudioCompressionFormat compressionFormat,
            float quality,
            bool preload)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    continue;
                }

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = loadType;
                settings.compressionFormat = compressionFormat;
                settings.quality = quality;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = preload;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.loadInBackground = loadType == AudioClipLoadType.Streaming;
                importer.SaveAndReimport();
            }
        }

        private static void BuildAudioMixer()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                Type controllerType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController");
                MethodInfo createMethod = controllerType?.GetMethod(
                    "CreateMixerControllerAtPath",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (createMethod == null)
                {
                    throw new MissingMethodException("Unity AudioMixer creation API was not found.");
                }

                mixer = createMethod.Invoke(null, new object[] { MixerPath }) as AudioMixer;
                if (mixer == null)
                {
                    throw new InvalidOperationException("Unity did not create the AudioMixer.");
                }
            }

            EnsureMixerGroups(mixer, "Music", "SFX", "UI");
            EditorUtility.SetDirty(mixer);
        }

        private static void EnsureMixerGroups(AudioMixer mixer, params string[] names)
        {
            Type controllerType = mixer.GetType();
            MethodInfo createMethod = controllerType.GetMethod(
                "CreateNewGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo addMethod = controllerType.GetMethod(
                "AddChildToParent",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            PropertyInfo masterProperty = controllerType.GetProperty(
                "masterGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (createMethod == null || addMethod == null || masterProperty == null)
            {
                throw new MissingMethodException("Unity AudioMixer group APIs were not found.");
            }

            object master = masterProperty.GetValue(mixer);
            HashSet<string> existing = new HashSet<string>(
                mixer.FindMatchingGroups(string.Empty).Select(group => group.name),
                StringComparer.Ordinal);

            foreach (string name in names)
            {
                if (existing.Contains(name))
                {
                    continue;
                }

                object child = InvokeWithDefaults(createMethod, mixer, name);
                InvokeWithDefaults(addMethod, mixer, child, master);
            }
        }

        private static object InvokeWithDefaults(MethodInfo method, object target, params object[] supplied)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                if (index < supplied.Length)
                {
                    arguments[index] = supplied[index];
                }
                else if (parameters[index].HasDefaultValue)
                {
                    arguments[index] = parameters[index].DefaultValue;
                }
                else if (parameters[index].ParameterType == typeof(bool))
                {
                    arguments[index] = false;
                }
                else
                {
                    arguments[index] = parameters[index].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[index].ParameterType)
                        : null;
                }
            }

            return method.Invoke(target, arguments);
        }

        private static void ValidatePhase1(AnimatorSpec spec)
        {
            List<string> failures = new List<string>();
            foreach (AnimatorStateSpec state in spec.states)
            {
                string stripPath = ResolveSpecPath(AnimatorSpecPath, state.strip);
                TextureImporter importer = AssetImporter.GetAtPath(stripPath) as TextureImporter;
                int spriteCount = AssetDatabase.LoadAllAssetsAtPath(stripPath).OfType<Sprite>().Count();
                if (importer == null ||
                    importer.filterMode != FilterMode.Point ||
                    importer.textureCompression != TextureImporterCompression.Uncompressed ||
                    importer.mipmapEnabled ||
                    importer.spriteImportMode != SpriteImportMode.Multiple ||
                    spriteCount != state.frames)
                {
                    failures.Add($"{state.name}: importer/slice mismatch ({spriteCount}/{state.frames}).");
                }

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipRoot}/{state.name}.anim");
                if (clip == null || Math.Abs(clip.frameRate - state.fps) > 0.01f)
                {
                    failures.Add($"{state.name}: clip missing or FPS mismatch.");
                }
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null || controller.layers[0].stateMachine.states.Length != spec.states.Length)
            {
                failures.Add("Animator controller is missing or does not contain 21 states.");
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                failures.Add("Audio mixer is missing.");
            }
            else
            {
                string[] groups = mixer.FindMatchingGroups(string.Empty).Select(group => group.name).ToArray();
                foreach (string expected in new[] { "Master", "Music", "SFX", "UI" })
                {
                    if (!groups.Contains(expected))
                    {
                        failures.Add("Audio mixer group missing: " + expected);
                    }
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidDataException("[SpiderProjection] Phase 1 validation failed:\n- " + string.Join("\n- ", failures));
            }

            Debug.Log("[SpiderProjection] Phase 1 validation passed: 21 sliced strips/clips, point filtering, uncompressed textures, Animator states, and mixer groups.");
        }

        private static string ResolveSpecPath(string specPath, string relativePath)
        {
            string directory = Path.GetDirectoryName(specPath)?.Replace('\\', '/');
            string combined = Path.GetFullPath(Path.Combine(directory ?? string.Empty, relativePath))
                .Replace('\\', '/');
            string projectRoot = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/');
            if (!combined.StartsWith(projectRoot + "/", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Spec path escapes the project: " + relativePath);
            }

            return combined.Substring(projectRoot.Length + 1);
        }

        private static void EnsureFolder(string path)
        {
            string normalized = path.Replace('\\', '/');
            string[] parts = normalized.Split('/');
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
