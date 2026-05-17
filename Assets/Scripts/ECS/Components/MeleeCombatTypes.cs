namespace Chronocaust.Ecs.Components
{
    public enum MeleeMotionType : byte
    {
        Arc = 0,
        Thrust,
        GroundSlam,
        Spin
    }

    public enum MeleeAttackPhase : byte
    {
        None = 0,
        Startup,
        Active,
        Recovery
    }
}
