using GTA;
using GTA.Math;
using GTA.Native;
using AreaLib;
using SPKiller.Enums;
using SPKiller.Managers;

namespace SPKiller.Classes
{
    public class SearchingCluesStage : StageBase
    {
        #region Constants
        private const int MaxClues = 4;
        private const float AudioRange = 50.0f;

        private readonly string[] ClueModels =
        {
            "ch_prop_collectibles_limb_01a",
            "ch_prop_ch_serialkiller_01a",
            "ch_prop_ch_boodyhand_01a",
            "ch_prop_ch_bloodymachete_01a"
        };

        private readonly KillerFlags[] ClueFlags =
        {
            KillerFlags.FoundLimb,
            KillerFlags.FoundWriting,
            KillerFlags.FoundHandprint,
            KillerFlags.FoundMachete
        };
        #endregion

        // Entities
        private readonly Prop[] _props = new Prop[MaxClues];
        private Camera _cam = null;
        private int _soundId = -1;

        // Areas
        private readonly Sphere[] _audioAreas = new Sphere[MaxClues];
        private readonly Sphere[] _interactionAreas = new Sphere[MaxClues];

        // Misc.
        private KillerFlags _interactionFlag = KillerFlags.None;

        #region Properties
        public override KillerStage NextStage => KillerStage.SearchingVan;
        #endregion

        #region Private methods
        private int FlagToIndex(KillerFlags flag)
        {
            switch (flag)
            {
                case KillerFlags.FoundLimb:
                    return 0;

                case KillerFlags.FoundWriting:
                    return 1;

                case KillerFlags.FoundHandprint:
                    return 2;

                case KillerFlags.FoundMachete:
                    return 3;

                default:
                    return -1;
            }
        }

        private int GetNumCluesFound()
        {
            int num = 0;

            for (int i = 0; i < MaxClues; i++)
            {
                if (SaveManager.HasFlag(ClueFlags[i]))
                {
                    num++;
                }
            }

            return num;
        }

        private void DestroyClueImmersive(KillerFlags flag)
        {
            DestroyClueImmersive(FlagToIndex(flag));
        }

        private void DestroyClueImmersive(int index)
        {
            if (_props[index] != null)
            {
                _props[index].MarkAsNoLongerNeeded();
                _props[index] = null;
            }

            DestroyAreas(index);
        }

        private void DestroyAreas(int index)
        {
            if (_audioAreas[index] != null)
            {
                AreaLibrary.Untrack(_audioAreas[index]);

                _audioAreas[index].PlayerEnter -= EnterAudioArea;
                _audioAreas[index].PlayerLeave -= LeaveAudioArea;
                _audioAreas[index] = null;
            }

            if (_interactionAreas[index] != null)
            {
                AreaLibrary.Untrack(_interactionAreas[index]);

                _interactionAreas[index].PlayerEnter -= EnterInteractionArea;
                _interactionAreas[index].PlayerLeave -= LeaveInteractionArea;
                _interactionAreas[index] = null;
            }
        }
        #endregion

        #region Public methods
        public override void Init(bool scriptStart)
        {
            Vector3 areaOffset = new Vector3(0.0f, 0.0f, 1.05f);

            for (int i = 0; i < MaxClues; i++)
            {
                if (SaveManager.HasFlag(ClueFlags[i]))
                {
                    continue;
                }

                // Entities
                Location location = LocationManager.GetClueLocation(i);
                _props[i] = World.CreateProp(ClueModels[i], location.Position, location.Rotation, false, false);
                _props[i].FreezePosition = true;

                // Areas
                _audioAreas[i] = new Sphere(location.Position, AudioRange);
                _audioAreas[i].SetData("skMod_AudioName", string.Format("attract_{0:00}", i + 1));
                _audioAreas[i].PlayerEnter += EnterAudioArea;
                _audioAreas[i].PlayerLeave += LeaveAudioArea;

                _interactionAreas[i] = new Sphere(CameraManager.GetClueCamera(i).Position - areaOffset, 1.5f);
                _interactionAreas[i].SetData("skMod_AddsFlag", ClueFlags[i]);
                _interactionAreas[i].PlayerEnter += EnterInteractionArea;
                _interactionAreas[i].PlayerLeave += LeaveInteractionArea;

                AreaLibrary.Track(_audioAreas[i]);
                AreaLibrary.Track(_interactionAreas[i]);
            }

            // Clear the area around ch_prop_ch_serialkiller_01a (doesn't seem to work with MP map, needs SET_INSTANCE_PRIORITY_MODE instead?)
            Function.Call(Hash.CREATE_MODEL_HIDE, -134.0374f, 1913.719f, 197.1885f, 1.0f, Game.GenerateHash("prop_pallet_pile_04"), true);
            Function.Call(Hash.CREATE_MODEL_HIDE, -134.4032f, 1911.055f, 196.3331f, 1.0f, Game.GenerateHash("prop_dumpster_02a"), true);
        }

        public override bool Update()
        {
            if (_interactionFlag == KillerFlags.None)
            {
                return false;
            }

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
                    DestroyClueImmersive(_interactionFlag);

                    SaveManager.AddFlag(_interactionFlag);
                    SaveManager.Save();

                    Util.StopSound(ref _soundId);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "clue_seen", "dlc_ch_hidden_collectibles_sk_sounds", false);

                    int numFound = GetNumCluesFound();
                    Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "SERIAL_KIL_COLLECT");
                    Function.Call(Hash.ADD_TEXT_COMPONENT_INTEGER, numFound);
                    Function.Call(Hash._DRAW_NOTIFICATION, false, true);

                    Game.Player.Money += SaveManager.ClueReward;

                    if (numFound == MaxClues)
                    {
                        return true;
                    }

                    _interactionFlag = KillerFlags.None;
                }
                else
                {
                    CameraData cameraData = CameraManager.GetClueCamera(FlagToIndex(_interactionFlag));

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

            return false;
        }

        public override void Destroy(bool scriptExit)
        {
            CameraManager.Disable();

            // Remove clue props (and areas)
            for (int i = 0; i < MaxClues; i++)
            {
                if (_props[i] != null)
                {
                    if (scriptExit)
                    {
                        _props[i].Delete();
                    }
                    else
                    {
                        DestroyClueImmersive(i);
                    }

                    _props[i] = null;
                }
            }

            // Remove the clue camera
            if (_cam != null)
            {
                _cam.Destroy();
                _cam = null;
            }

            // Stop the clue sound
            Util.StopSound(ref _soundId);
        }
        #endregion

        #region Events
        private void EnterAudioArea(AreaBase area)
        {
            if (_soundId == -1 && area.GetData("skMod_AudioName", out string audioName))
            {
                _soundId = Util.PlaySoundFromCoord((area as Sphere).Center, "dlc_ch_hidden_collectibles_sk_sounds", audioName);
            }
        }

        private void LeaveAudioArea(AreaBase area)
        {
            Util.StopSound(ref _soundId);
        }

        private void EnterInteractionArea(AreaBase area)
        {
            if (area.GetData("skMod_AddsFlag", out KillerFlags flag))
            {
                _interactionFlag = flag;
            }
        }

        private void LeaveInteractionArea(AreaBase area)
        {
            CameraManager.Disable();
            _interactionFlag = KillerFlags.None;
        }
        #endregion
    }
}
