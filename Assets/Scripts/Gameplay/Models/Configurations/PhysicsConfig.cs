namespace Assets.Scripts.Gameplay.Models.Configurations
{
    public class PhysicsConfig
    {
        public float Gravity = -15.0f;
        public float GroundAccelerate = 36.0f;
        public float AirAccelerate = 150.0f;
        public float StopSpeed = 100.0f;
        public float GroundFriction = 1.0f;
        public float AirFriction = 0.4f;
        public float MaxSpeed = 50.0f;
        public float MaxAirWishSpeed = 12.0f;
        public float StepOffset = 1.0f;

        public float BounceMod = 1.0f;
        public float GravityMod = 1.0f;
        public float FrictionMod = 5.0f;

        public float MaxSlideSpeed = 18f;
        public float MinSlideSpeed = 9f;
        public float SlideFriction = 14f;
        public float SlideSpeedMod = 1.75f;
        public float DownSlideSpeedMod = 2.5f;
        public float SlideDelay = 0.5f;
        public float SlopeYNormalLimit = 0.7f;
        public float SlopeAngleLimit = 45f;

        public uint MaxCollisions = 128;
        public uint MaxClipPlanes = 6;
        public uint NumBumps = 1;
    }
}
