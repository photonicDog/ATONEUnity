using System;

namespace Assets.Scripts.Gameplay.Commands
{
    public class AltReleaseCommand : Command
    {
        public AltReleaseCommand(double startOfFrame, double deltaTime) : base(startOfFrame, deltaTime){}

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
