using UnityEngine;
using UnityEngine.InputSystem;

namespace SpiderProjection.Runtime
{
    public sealed class ProjectionCalibrationController : MonoBehaviour
    {
        [SerializeField] private ProjectionLayout layout;
        [SerializeField] private TextAsset exampleLayoutJson;
        [SerializeField] private SpriteRenderer calibrationImage;
        [SerializeField] private SpriteRenderer blackoutImage;
        [SerializeField] private SpriteRenderer safeAreaOverlay;
        [SerializeField] private bool startInCalibrationMode;
        [SerializeField] private bool realWallMode;

        private MappedSurface2D[] mappedSurfaces;
        private bool calibrationMode;
        private bool blackoutMode;

        public static ProjectionCalibrationController Active { get; private set; }
        public bool CalibrationMode => calibrationMode;
        public bool BlackoutMode => blackoutMode;
        public bool RealWallMode => realWallMode;
        public ProjectionLayout Layout => layout;

        private void Awake()
        {
            Active = this;
            mappedSurfaces = FindObjectsByType<MappedSurface2D>(FindObjectsSortMode.None);
            SetCalibrationMode(startInCalibrationMode);
            SetRealWallMode(realWallMode);
            LogExampleLayoutNotice();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                SetBlackout(!blackoutMode);
            }
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                SetRealWallMode(!realWallMode);
            }
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void Configure(
            ProjectionLayout projectionLayout,
            TextAsset exampleJson,
            SpriteRenderer calibration,
            SpriteRenderer blackout,
            SpriteRenderer safeArea)
        {
            layout = projectionLayout;
            exampleLayoutJson = exampleJson;
            calibrationImage = calibration;
            blackoutImage = blackout;
            safeAreaOverlay = safeArea;
        }

        public void SetStartupModes(bool beginInCalibrationMode, bool beginInRealWallMode)
        {
            startInCalibrationMode = beginInCalibrationMode;
            realWallMode = beginInRealWallMode;
        }

        public void ToggleCalibrationMode()
        {
            SetCalibrationMode(!calibrationMode);
        }

        public void SetCalibrationMode(bool enabled)
        {
            calibrationMode = enabled;
            blackoutMode = false;
            if (calibrationImage != null)
            {
                calibrationImage.enabled = enabled;
            }
            if (safeAreaOverlay != null)
            {
                safeAreaOverlay.enabled = enabled;
            }
            if (blackoutImage != null)
            {
                blackoutImage.enabled = false;
            }
        }

        public void SetBlackout(bool enabled)
        {
            blackoutMode = enabled;
            if (blackoutImage != null)
            {
                blackoutImage.enabled = enabled;
            }
            if (enabled)
            {
                if (calibrationImage != null)
                {
                    calibrationImage.enabled = false;
                }
                if (safeAreaOverlay != null)
                {
                    safeAreaOverlay.enabled = false;
                }
            }
            else
            {
                if (calibrationImage != null)
                {
                    calibrationImage.enabled = calibrationMode;
                }
                if (safeAreaOverlay != null)
                {
                    safeAreaOverlay.enabled = calibrationMode;
                }
            }
        }

        public void SetRealWallMode(bool enabled)
        {
            realWallMode = enabled;
            mappedSurfaces ??= FindObjectsByType<MappedSurface2D>(FindObjectsSortMode.None);
            foreach (MappedSurface2D surface in mappedSurfaces)
            {
                surface.SetDemoVisible(!enabled);
            }
        }

        public void LogExampleLayoutNotice()
        {
            string source = exampleLayoutJson != null ? exampleLayoutJson.name : "missing";
            Debug.Log($"[SpiderProjection] Loaded {source} as an example only. Replace normalized surfaces with measured wall values before installation.");
        }
    }
}
