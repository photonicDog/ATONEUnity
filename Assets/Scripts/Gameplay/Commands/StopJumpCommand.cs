using System;

namespace Assets.Scripts.Gameplay.Commands
{
    public class StopJumpCommand : Command
    {
        public StopJumpCommand(double startOfFrame, double deltaTime) : base(startOfFrame, deltaTime){}

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
