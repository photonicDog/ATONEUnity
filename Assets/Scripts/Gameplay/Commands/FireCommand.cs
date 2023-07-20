using System;

namespace Assets.Scripts.Gameplay.Commands
{
    public class FireCommand : Command
    {
        public FireCommand(double startOfFrame, double deltaTime) : base(startOfFrame, deltaTime){}

        public override void Execute()
        {
            throw new NotImplementedException();
        }

        public override void Undo()
        {
            throw new NotImplementedException();
        }
    }
}
