using Chronocaust.Ecs.Components;
using Chronocaust.Ecs.Core;
using UnityEngine;

namespace Chronocaust.Ecs.Systems
{
    public sealed class PlayerInputSystem : IEcsUpdateSystem
    {
        private EcsQuery<PlayerTagComponent, InputStateComponent, TransformComponent> _query;

        public void Update(EcsWorld world, float deltaTime)
        {
            _query ??= world.CreateQuery<PlayerTagComponent, InputStateComponent, TransformComponent>();

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 rawMove = Vector2.ClampMagnitude(
                new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            bool fire = Input.GetMouseButton(0);
            bool interact = Input.GetKeyDown(KeyCode.E);
            bool drop = Input.GetKeyDown(KeyCode.G);
            bool slot0 = Input.GetKeyDown(KeyCode.Alpha1);
            bool slot1 = Input.GetKeyDown(KeyCode.Alpha2);

            _query.ForEach((EntityId id,
                ref PlayerTagComponent _,
                ref InputStateComponent input,
                ref TransformComponent transform) =>
            {
                if (transform.Transform == null) return;

                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = transform.Transform.position.z;

                input.MouseWorldPosition = mouseWorld;
                input.FirePressed = fire;
                input.InteractPressed = interact;
                input.DropPressed = drop;
                input.SelectSlot0Pressed = slot0;
                input.SelectSlot1Pressed = slot1;
                input.MoveInput = rawMove;
            });
        }
    }
}
