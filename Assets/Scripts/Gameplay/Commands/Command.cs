namespace Assets.Scripts.Gameplay.Commands
{
    public abstract class Command
    {
        public double startOfFrame;
        public double endOfFrame
        {
            get {
                return startOfFrame + deltaTime;
            }
        }
        public double deltaTime;

        public Command(double startOfFrame, double deltaTime)
        {
            this.startOfFrame = startOfFrame;
            this.deltaTime = deltaTime;
        }
        public abstract void Execute();
        public abstract void Undo();
    }
}
