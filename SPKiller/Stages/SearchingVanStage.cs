using GTA;
using GTA.Math;
using GTA.Native;
using AreaLib;
using SPKiller.Enums;
using SPKiller.Managers;

namespace SPKiller.Classes
{
    public class SearchingVanStage : StageBase
    {
        #region Constants
        private const float AudioRange = 50.0f;
        #endregion

        // Entities
        private Vehicle _van = null;
        private Prop _bags = null;
        private Camera _cam = null;
        private int _soundId = -1;

        // Areas
        private Sphere _audioArea = null;
        private Sphere _interactionArea = null;

        #region Properties
        public override KillerStage NextStage => KillerStage.SearchingKiller;
        #endregion

        #region Private methods
        private void DestroyImmersive()
        {
            if (_bags != null)
            {
                _bags.MarkAsNoLongerNeeded();
                _bags = null;
            }

            if (_van != null)
            {
                _van.MarkAsNoLongerNeeded();
                _van = null;
            }

            DestroyAreas();
        }

        private void DestroyAreas()
        {
            if (_audioArea != null)
            {
                AreaLibrary.Untrack(_audioArea);

                _audioArea.PlayerEnter -= EnterAudioArea;
                _audioArea.PlayerLeave -= LeaveAudioArea;
                _audioArea = null;
            }

            if (_interactionArea != null)
            {
                AreaLibrary.Untrack(_interactionArea);

                _interactionArea.PlayerLeave -= LeaveInteractionArea;
                _interactionArea = null;
            }
        }
        #endregion

        #region Public methods
        public override void Init(bool scriptStart)
        {
            Location location = LocationManager.GetVanLocation(SaveManager.VanIndex);

            // Entities
            _van = World.CreateVehicle(VehicleHash.Speedo, location.Position, location.Heading);
            _van.FreezePosition = true;
            _van.LockStatus = VehicleLockStatus.Locked;
            _van.DirtLevel = 15.0f;
            _van.PrimaryColor = VehicleColor.MetallicGraphiteBlack;
            _van.SecondaryColor = VehicleColor.MetallicGraphiteBlack;

            _van.OpenDoor(VehicleDoor.BackLeftDoor, false, true);
            _van.OpenDoor(VehicleDoor.BackRightDoor, false, true);

            Function.Call(Hash.SET_VEHICLE_EXTRA_COLOURS, _van.Handle, 1, 156);
            Function.Call((Hash)0x1DDA078D12879EEE, _van.Handle, false, false); // _SET_VEHICLE_CAN_BE_LOCKED_ON
            Function.Call(Hash._0x2B6747FAA9DB9D6B, _van.Handle, true); // SET_VEHICLE_DISABLE_TOWING
            Function.Call(Hash.SET_ENTITY_PROOFS, _van.Handle, true, true, true, true, true, true, true, true);

            _bags = World.CreateProp("ch_prop_collectibles_garbage_01a", location.Position, false, false);
            _bags.Heading = location.Heading;
            _bags.IsInvincible = true;
            _bags.HasCollision = false;

            Function.Call(Hash._0x3910051CCECDB00C, _bags.Handle, true); // _SET_ENTITY_SOMETHING
            Function.Call(Hash._0x77F33F2CCF64B3AA, _bags.Handle, true); // _SET_OBJECT_SOMETHING

            _bags.AttachTo(_van, 0, new Vector3(0.1f, -1.0f, -0.18f), Vector3.Zero);

            // Areas
            _audioArea = new Sphere(location.Position, AudioRange);
            _audioArea.PlayerEnter += EnterAudioArea;
            _audioArea.PlayerLeave += LeaveAudioArea;

            _interactionArea = new Sphere(_van.GetOffsetInWorldCoords(new Vector3(0.0f, -4.0f, 0.0f)), 1.5f);
            _interactionArea.PlayerLeave += LeaveInteractionArea;

            AreaLibrary.Track(_audioArea);
            AreaLibrary.Track(_interactionArea);
        }

        public override bool Update()
        {
            if (_interactionArea != null && _interactionArea.IsPlayerInside)
            {
                if (Game.Player.Character.IsInVehicle())
                {
                    Util.DisplayHelpTextThisFrame("TREA1_HINTB");
                    return false;
                }

                Util.DisplayHelpTextThisFrame(CameraManager.IsActive ? "TREA1_EXIT" : "SERIAL1_HINT");

                if (Game.IsControlJustPressed(0, Control.Context))
                {
                    if (CameraManager.IsActive)
                    {
                        CameraManager.Disable();
                        DestroyImmersive();

                        SaveManager.AddFlag(KillerFlags.FoundVan);
                        SaveManager.Save();

                        Util.StopSound(ref _soundId);
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "clue_seen", "dlc_ch_hidden_collectibles_sk_sounds", false);

                        Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "SERIAL_KIL_COLLECT");
                        Function.Call(Hash.ADD_TEXT_COMPONENT_INTEGER, 5);
                        Function.Call(Hash._DRAW_NOTIFICATION, false, true);

                        Game.Player.Money += SaveManager.ClueReward;
                        return true;
                    }
                    else
                    {
                        CameraData cameraData = CameraManager.GetVanCamera(SaveManager.VanIndex);

                        _cam = World.CreateCamera(cameraData.Position, cameraData.Rotation, cameraData.FOV);
                        _cam.Shake(CameraShake.Hand, 0.19f);

                        CameraManager.SetCurrent(_cam, true);
                    }
                }

                if (CameraManager.IsActive)
                {
                    Game.DisableAllControlsThisFrame(0);
                    Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
                }
            }

            return false;
        }

        public override void Destroy(bool scriptExit)
        {
            CameraManager.Disable();

            // Remove entities and areas
            if (scriptExit)
            {
                if (_bags != null)
                {
                    _bags.Delete();
                    _bags = null;
                }

                if (_van != null)
                {
                    _van.Delete();
                    _van = null;
                }
            }
            else
            {
                DestroyImmersive();
            }

            // Remove the camera
            if (_cam != null)
            {
                _cam.Destroy();
                _cam = null;
            }

            // Stop the sound
            Util.StopSound(ref _soundId);
        }
        #endregion

        #region Events
        private void EnterAudioArea(AreaBase area)
        {
            if (_soundId == -1)
            {
                _soundId = Util.PlaySoundFromCoord((area as Sphere).Center, "dlc_ch_hidden_collectibles_sk_sounds", "attract_05");
            }
        }

        private void LeaveAudioArea(AreaBase area)
        {
            Util.StopSound(ref _soundId);
        }

        private void LeaveInteractionArea(AreaBase area)
        {
            CameraManager.Disable();
        }
        #endregion
    }
}
