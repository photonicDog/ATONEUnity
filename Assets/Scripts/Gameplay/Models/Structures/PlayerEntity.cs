using Assets.Scripts.Gameplay.Interfaces;

namespace Assets.Scripts.Gameplay.Structures
{
    internal struct PlayerEntity : IEntity
    {
        private PlayerMovement _movement;
        public IMovement Movement => _movement;

        public PlayerEntity(PlayerMovement movement)
        {
            _movement = movement;
        }
    }
}
