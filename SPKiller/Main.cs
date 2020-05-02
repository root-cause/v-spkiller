using System;
using GTA;
using GTA.Native;
using SPKiller.Enums;
using SPKiller.Classes;
using SPKiller.Managers;

namespace SPKiller
{
    public class Main : Script
    {
        public StageBase CurrentStageHandler = null;
        public bool GameReady = false;

        #region Methods
        public void MakeNewHandler(KillerStage newStage, bool runInit)
        {
            if (CurrentStageHandler != null)
            {
                CurrentStageHandler.Destroy(false);
                CurrentStageHandler = null;
            }

            switch (newStage)
            {
                case KillerStage.SearchingClues:
                    CurrentStageHandler = new SearchingCluesStage(); 
                    break;

                case KillerStage.SearchingVan:
                    CurrentStageHandler = new SearchingVanStage();
                    break;

                case KillerStage.SearchingKiller:
                    CurrentStageHandler = new SearchingKillerStage();
                    break;

                case KillerStage.Complete:
                    CurrentStageHandler = new CompleteStage();
                    break;

                default:
                    throw new NotImplementedException("Not implemented stage used with MakeNewHandler.");
            }

            if (runInit)
            {
                CurrentStageHandler?.Init(false);
            }
        }
        #endregion

        #region Constructor
        public Main()
        {
            MakeNewHandler(SaveManager.Load(), false);

            Tick += Main_Tick;
            Aborted += Main_Aborted;
        }
        #endregion

        #region Events
        public void Main_Tick(object sender, EventArgs e)
        {
            if (!GameReady && !Game.IsLoading && Game.Player.CanControlCharacter)
            {
                Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "DLC_HEIST3/DLC_CH_HIDDEN_COLLECTIBLES_SK", false, -1);

                GameReady = true;
                CurrentStageHandler?.Init(true);
            }

            if (CurrentStageHandler != null && CurrentStageHandler.Update())
            {
                MakeNewHandler(CurrentStageHandler.NextStage, true);
            }
        }

        public void Main_Aborted(object sender, EventArgs e)
        {
            if (CurrentStageHandler != null)
            {
                CurrentStageHandler.Destroy(true);
                CurrentStageHandler = null;
            }
        }
        #endregion
    }
}
