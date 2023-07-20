namespace Assets.Scripts.Gameplay.Commands
{
    public abstract class Command
    {
        private double _startOfFrame;
        private double _deltaTime;
        public double StartOfFrame { get { return _startOfFrame; }  set { _startOfFrame = value; } }
        public double EndOfFrame { get { return StartOfFrame + DeltaTime; } }
        public double DeltaTime { get { return _deltaTime; } set { _deltaTime = value; } }

        public Command(double startOfFrame, double deltaTime)
        {
            this.StartOfFrame = startOfFrame;
            this.DeltaTime = deltaTime;
        }
        public abstract void Execute();
        public abstract void Undo();
    }
}
