using GTA;
using GTA.Math;
using SPKiller.Classes;

namespace SPKiller.Managers
{
    public static class CameraManager
    {
        #region Properties
        public static bool IsActive { get; private set; } = false;
        public static bool IsPlayerHidden { get; private set; } = false;
        public static Camera Current { get; private set; } = null;
        #endregion

        #region Methods
        public static CameraData GetClueCamera(int index)
        {
            if (index < 0 || index >= _clueCameraLocations.Length)
            {
                return null;
            }

            return _clueCameraLocations[index];
        }

        public static CameraData GetVanCamera(int index)
        {
            if (index < 0 || index >= _vanCameraLocations.Length)
            {
                return null;
            }

            return _vanCameraLocations[index];
        }

        public static void SetCurrent(Camera camera, bool hidePlayer)
        {
            if (camera == null || !camera.Exists())
            {
                return;
            }

            // Make player visible again if the new camera has hidePlayer set to false
            if (IsActive && IsPlayerHidden && !hidePlayer)
            {
                Game.Player.Character.IsVisible = true;
            }

            IsActive = true;
            IsPlayerHidden = hidePlayer;
            Current = camera;

            World.RenderingCamera = camera;

            if (hidePlayer)
            {
                Game.Player.Character.IsVisible = false;
            }
        }

        public static void Disable()
        {
            if (IsActive && Current != null && World.RenderingCamera == Current)
            {
                World.RenderingCamera = null;

                GameplayCamera.RelativeHeading = 0.0f;
                GameplayCamera.RelativePitch = 0.0f;
            }

            if (IsPlayerHidden)
            {
                Game.Player.Character.IsVisible = true;
            }

            IsActive = false;
            IsPlayerHidden = false;
            Current = null;
        }
        #endregion

        #region Clue camera data
        // Limb -> writing -> handprint -> machete
        private static readonly CameraData[] _clueCameraLocations =
        {
            new CameraData(new Vector3(1110.951f, 3142.362f, 38.1071f), new Vector3(-22.9355f, 0f, -17.367f), 50.0f),
            new CameraData(new Vector3(-135.514f, 1912.729f, 197.5915f), new Vector3(0.7938f, 0.0826f, -97.936f), 50.0f),
            new CameraData(new Vector3(-679.731f, 5799.711f, 17.7268f), new Vector3(-5.5064f, -0.0475f, -22.9284f), 50.0f),
            new CameraData(new Vector3(1902.635f, 4912.922f, 49.5961f), new Vector3(-10.8839f, -0.0418f, -95.3479f), 50.0f)
        };
        #endregion

        #region Van camera data
        private static readonly CameraData[] _vanCameraLocations =
        {
            new CameraData(new Vector3(2899.235f, 3656.279f, 45.4003f), new Vector3(-11.9789f, 0.0503f, 178.0333f), 50.0f),
            new CameraData(new Vector3(2569f, 1262.166f, 45.4624f), new Vector3(-23.0265f, 0f, 16.3236f), 50.0f),
            new CameraData(new Vector3(-1568.882f, 4425.205f, 8.1692f), new Vector3(-23.8526f, 0f, -172.6049f), 50.0f),
            new CameraData(new Vector3(-1710.804f, 2620.803f, 3.7698f), new Vector3(-14.4015f, 0.0455f, -150.7166f), 50.0f),
            new CameraData(new Vector3(2437.681f, 5837.861f, 59.5102f), new Vector3(-11.5291f, 0f, -162.9725f), 50.0f)
        };
        #endregion
    }
}