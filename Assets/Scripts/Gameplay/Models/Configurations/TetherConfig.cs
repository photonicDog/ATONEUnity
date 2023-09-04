using UnityEngine;

namespace Assets.Scripts.Gameplay.Models.Configurations
{
    public class TetherConfig
    {
        public bool TetherEnabled = true;
        public float CooldownOnBreak = 3.0f;
        public float FireSpeed = 100.0f;
        public float RetractTime = 0.2f;
        public float FireTimeMax = 3.0f;
        public float MaxHookDistance = 30.0f;
        public bool CooldownOnlyOnGround = true;

        public float TetherLeashMod = 0.95f;
        public float TetherLeashMaxRange = 10f;

        public string NoTetherTag = "Untetherable";
        public LayerMask TetherableLayerMasks = LayerMask.GetMask("Ground") | LayerMask.GetMask("Tetherable Entity");
        public LayerMask BreakTetherLayerMasks = LayerMask.GetMask("Ground");
    }
}
