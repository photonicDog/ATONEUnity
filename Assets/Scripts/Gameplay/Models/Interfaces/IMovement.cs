using UnityEngine;

namespace Assets.Scripts.Gameplay.Interfaces
{
    public interface IMovement
    {
        Vector3 Velocity { get; set; }
        float Speed { get; }
        Vector3 WishVelocity { get; set; }
        Vector3 WishDirection { get; }
        float WishSpeed { get; }
        bool IsGrounded { get; set; }
    }
}
