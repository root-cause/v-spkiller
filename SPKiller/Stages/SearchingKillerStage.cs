using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using SPKiller.Enums;
using SPKiller.Managers;

namespace SPKiller.Classes
{
    public class SearchingKillerStage : StageBase
    {
        #region Constants
        private const float SpawnDistance = 8.0f;

        private const int MessageDelay = 7000;
        private const int SpawnLogicDelay = 120000;
        private const int SpawnAttemptDelay = 5000;
        private const int FightLogicDelay = 100;

        private const int HelpTextTime = 7000;
        private const int BlipFlashInterval = 250;
        private const int BlipFlashTime = 7000;

        private const float CancelDistanceSquared = 100.0f * 100.0f;

        private readonly int RelationshipHash = Game.GenerateHash("HATES_PLAYER");
        private readonly int CountrysideHash = Game.GenerateHash("countryside");
        private readonly WeaponHash NavyRevolverHash = (WeaponHash)Game.GenerateHash("WEAPON_NAVYREVOLVER");

        private readonly Vector3 GameplayCameraOffset = new Vector3(0.0f, -15.0f, 0.0f);

        private readonly Dictionary<int, Tuple<int, int>> FreemodeClothes = new Dictionary<int, Tuple<int, int>>()
        {
            // component, Tuple<drawable, texture>
            { 1, Tuple.Create(69, 1) },
            { 3, Tuple.Create(53, 0) },
            { 4, Tuple.Create(7, 1) },
            { 6, Tuple.Create(12, 6) },
            { 7, Tuple.Create(39, 0) },
            { 8, Tuple.Create(63, 1) },
            { 11, Tuple.Create(59, 1) }
        };

        private readonly Dictionary<int, Tuple<int, int>> RedneckClothes = new Dictionary<int, Tuple<int, int>>()
        {
            // component, Tuple<drawable, texture>
            { 0, Tuple.Create(0, 1) },
            { 2, Tuple.Create(1, 0) },
            { 3, Tuple.Create(0, 0) },
            { 4, Tuple.Create(0, 0) }
        };
        #endregion

        private KillerFightState _state = KillerFightState.None;
        private int _nextActionTime = 0;
        private int _helpTextHideAt = 0;

        private Ped _killer = null;
        private bool _killerSeen = false;

        #region Properties
        public override KillerStage NextStage => KillerStage.Complete;
        #endregion

        #region Private methods
        private void ChangeState(KillerFightState newState, int actionDelay)
        {
            _state = newState;
            _nextActionTime = Game.GameTime + actionDelay;
        }

        private void SendKillerMessage()
        {
            Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "SERIAL_KIL_TXT");
            Function.Call(Hash._SET_NOTIFICATION_MESSAGE, "CHAR_DEFAULT", "CHAR_DEFAULT", false, 1, "CELL_195", "");

            switch ((PedHash)Game.Player.Character.Model.Hash)
            {
                case PedHash.Michael:
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Michael", false);
                    break;

                case PedHash.Franklin:
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Franklin", false);
                    break;

                case PedHash.Trevor:
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Trevor", false);
                    break;

                default:
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", false);
                    break;
            }

            SaveManager.AddFlag(KillerFlags.ReceivedText);
            SaveManager.Save();

            ChangeState(KillerFightState.Spawning, SpawnLogicDelay);
        }

        private void SpawnKiller(int gameTime)
        {
            _nextActionTime = gameTime + SpawnAttemptDelay;

            int hour = Function.Call<int>(Hash.GET_CLOCK_HOURS);
            if (hour < 19 && hour >= 5)
            {
                return;
            }

            if (!GameplayCamera.IsRendering || Game.MissionFlag || Game.Player.WantedLevel > 0 || Game.Player.Character.IsInVehicle())
            {
                return;
            }

            Vector3 spawnBase = GameplayCamera.GetOffsetInWorldCoords(GameplayCameraOffset);
            if (Function.Call<int>(Hash._0x7EE64D51E8498728, spawnBase.X, spawnBase.Y, spawnBase.Z) != CountrysideHash) // GET_HASH_OF_MAP_AREA_AT_COORDS
            {
                return;
            }

            if (Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, spawnBase.X, spawnBase.Y, spawnBase.Z) != 0)
            {
                return;
            }

            Vector3 spawnPos = World.GetSafeCoordForPed(spawnBase.Around(SpawnDistance), false, 16);
            if (spawnPos == Vector3.Zero)
            {
                return;
            }

            _killerSeen = false;

            // Load model
            Model model = new Model(SaveManager.UseFMM ? "mp_m_freemode_01" : "a_m_y_salton_01");
            if (!model.IsLoaded)
            {
                model.Request(1000);
            }

            // World.CreatePed caused a small stutter, ruining the surprise...
            _killer = Function.Call<Ped>(Hash.CREATE_PED, 26, model.Hash, spawnPos.X, spawnPos.Y, spawnPos.Z, 0.0f, false, false);
            _killer.SetDefaultClothes();

            // Clothes
            if (SaveManager.UseFMM)
            {
                foreach (var item in FreemodeClothes)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, _killer.Handle, item.Key, item.Value.Item1, item.Value.Item2, 0);
                }
            }
            else
            {
                foreach (var item in RedneckClothes)
                {
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, _killer.Handle, item.Key, item.Value.Item1, item.Value.Item2, 0);
                }
            }

            // Behavior etc.
            Function.Call(Hash._0x52D59AB61DDC05DD, _killer.Handle, true); // SET_PED_HIGHLY_PERCEPTIVE
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, _killer.Handle, 46, true);
            Function.Call(Hash._SET_PED_RAGDOLL_BLOCKING_FLAGS, _killer.Handle, 8209);

            _killer.Health = 600;
            _killer.MaxHealth = 600;
            _killer.Money = 0;
            _killer.RelationshipGroup = RelationshipHash;
            _killer.IsEnemy = true;
            _killer.IsOnlyDamagedByPlayer = true;

            Function.Call(Hash.SET_PED_SEEING_RANGE, _killer.Handle, 200.0f);
            Function.Call(Hash.SET_PED_HEARING_RANGE, _killer.Handle, 200.0f);
            Function.Call(Hash.SET_PED_CONFIG_FLAG, _killer.Handle, 281, true);
            Function.Call(Hash.SET_PED_CONFIG_FLAG, _killer.Handle, 434, true);

            _killer.AlwaysDiesOnLowHealth = false;

            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, _killer.Handle, 512, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, _killer.Handle, 43, true);
            Function.Call(Hash.SET_PED_TARGET_LOSS_RESPONSE, _killer.Handle, 1);

            _killer.CanWearHelmet = false;
            _killer.CanSufferCriticalHits = true;
            _killer.Weapons.Give(WeaponHash.Machete, 1, true, true);

            Function.Call(Hash.STOP_PED_SPEAKING, _killer.Handle, true);
            Function.Call(Hash.TASK_GO_TO_ENTITY, _killer.Handle, Game.Player.Character.Handle, 20000, 0.5f, 2.0f, 2.0f, 0);

            model.MarkAsNoLongerNeeded();
            ChangeState(KillerFightState.Fighting, FightLogicDelay);
        }

        private void HandleFight(int gameTime)
        {
            _nextActionTime = gameTime + FightLogicDelay;

            // Can this even happen normally???
            if (_killer == null || !_killer.Exists())
            {
                _helpTextHideAt = 0;

                ChangeState(KillerFightState.Spawning, SpawnLogicDelay);
                return;
            }

            Ped playerPed = Game.Player.Character;
            if (playerPed.IsDead || Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, 0, false) || playerPed.Position.DistanceToSquared(_killer.Position) >= CancelDistanceSquared)
            {
                _helpTextHideAt = 0;

                _killer.MarkAsNoLongerNeeded();
                _killer = null;

                ChangeState(KillerFightState.Spawning, SpawnLogicDelay * 2);
                return;
            }

            if (_killer.IsDead)
            {
                ChangeState(KillerFightState.KillerDead, 0);
                return;
            }

            if (!_killerSeen && (_killer.HasBeenDamagedBy(playerPed) || playerPed.HasBeenDamagedBy(_killer) || _killer.IsOnScreen))
            {
                _killerSeen = true;
                _helpTextHideAt = gameTime + HelpTextTime;

                // Behavior stuff again
                _killer.ResetConfigFlag(394);
                _killer.ResetConfigFlag(240);

                Function.Call(Hash.TASK_COMBAT_PED, _killer.Handle, playerPed.Handle, 0, 16);
                Function.Call(Hash.SET_PED_HEARING_RANGE, _killer.Handle, 100.0f);
                Function.Call(Hash.SET_PED_SEEING_RANGE, _killer.Handle, 100.0f);
                Function.Call(Hash.STOP_PED_SPEAKING, _killer.Handle, false);

                // Blip
                Blip blip = _killer.AddBlip();
                blip.Sprite = BlipSprite.Rampage;
                blip.Color = BlipColor.Red;
                blip.IsShortRange = true;
                blip.IsFlashing = true;

                Function.Call(Hash.SET_BLIP_NAME_FROM_TEXT_FILE, blip.Handle, "SERIALKILLBLIP");
                Function.Call(Hash.SET_BLIP_FLASH_INTERVAL, blip.Handle, BlipFlashInterval);
                Function.Call(Hash.SET_BLIP_FLASH_TIMER, blip.Handle, BlipFlashTime);
                Function.Call(Hash.FLASH_MINIMAP_DISPLAY);
            }
        }
        #endregion

        #region Public methods
        public override void Init(bool scriptStart)
        {
            if (!SaveManager.HasFlag(KillerFlags.ReceivedText))
            {
                ChangeState(KillerFightState.Messaging, MessageDelay);
            }
            else
            {
                ChangeState(KillerFightState.Spawning, SpawnLogicDelay);
            }
        }

        public override bool Update()
        {
            int gameTime = Game.GameTime;
            if (_helpTextHideAt > 0 && gameTime < _helpTextHideAt)
            {
                Util.DisplayHelpTextThisFrame("SERIALKILLHELP");
            }

            if (_nextActionTime > gameTime)
            {
                return false;
            }

            switch (_state)
            {
                case KillerFightState.Messaging:
                    SendKillerMessage();
                    break;

                case KillerFightState.Spawning:
                    SpawnKiller(gameTime);
                    break;

                case KillerFightState.Fighting:
                    HandleFight(gameTime);
                    break;

                case KillerFightState.KillerDead:
                    _helpTextHideAt = 0;
                    _killer.CurrentBlip?.Remove();

                    Game.Player.Money += SaveManager.KillReward;
                    Game.Player.Character.Weapons.Give(NavyRevolverHash, 9999, true, true);

                    SaveManager.AddFlag(KillerFlags.KilledKiller);
                    SaveManager.Save();

                    Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "killer_down", "dlc_ch_hidden_collectibles_sk_sounds", false);

                    Function.Call(Hash._SET_NOTIFICATION_TEXT_ENTRY, "NAVREV_UNLOCK");
                    Function.Call(Hash._0xC8F3AAF93D0600BF, "WEAPON_UNLOCK", 2, "NAVREV_UNLOCK", 1);
                    return true;
            }

            return false;
        }

        public override void Destroy(bool scriptExit)
        {
            if (_killer != null)
            {
                if (scriptExit)
                {
                    _killer.Delete();
                }
                else
                {
                    _killer.MarkAsNoLongerNeeded();
                }

                _killer = null;
            }
        }
        #endregion
    }
}
