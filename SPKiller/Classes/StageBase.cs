using SPKiller.Enums;

namespace SPKiller.Classes
{
    public abstract class StageBase
    {
        public abstract KillerStage NextStage { get; }

        public abstract void Init(bool scriptStart);
        public abstract bool Update();
        public abstract void Destroy(bool scriptExit);
    }
}