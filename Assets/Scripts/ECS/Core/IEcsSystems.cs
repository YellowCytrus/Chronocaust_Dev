namespace Chronocaust.Ecs.Core
{
    public interface IEcsUpdateSystem
    {
        void Update(EcsWorld world, float deltaTime);
    }

    public interface IEcsFixedUpdateSystem
    {
        void FixedUpdate(EcsWorld world, float deltaTime);
    }
}
