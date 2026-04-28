using System.Collections.Generic;
using Chronocaust.Ecs.Components;
using UnityEngine;

namespace Chronocaust.Ecs.Core
{
    public sealed class EcsWorld
    {
        private readonly List<EcsEntity> _entities = new List<EcsEntity>();
        private readonly List<EcsEntity> _destroyQueue = new List<EcsEntity>();
        private readonly List<IEcsUpdateSystem> _updateSystems = new List<IEcsUpdateSystem>();
        private readonly List<IEcsFixedUpdateSystem> _fixedUpdateSystems = new List<IEcsFixedUpdateSystem>();
        private int _nextEntityId = 1;

        public IReadOnlyList<EcsEntity> Entities => _entities;

        public EcsEntity CreateEntity()
        {
            EcsEntity entity = new EcsEntity(_nextEntityId++);
            _entities.Add(entity);
            return entity;
        }

        public void DestroyEntity(EcsEntity entity)
        {
            if (!_destroyQueue.Contains(entity))
            {
                _destroyQueue.Add(entity);
            }
        }

        public void AddSystem(IEcsUpdateSystem system)
        {
            _updateSystems.Add(system);
        }

        public void AddSystem(IEcsFixedUpdateSystem system)
        {
            _fixedUpdateSystems.Add(system);
        }

        public void Update(float deltaTime)
        {
            foreach (IEcsUpdateSystem system in _updateSystems)
            {
                system.Update(this, deltaTime);
            }

            FlushDestroyedEntities();
        }

        public void FixedUpdate(float deltaTime)
        {
            foreach (IEcsFixedUpdateSystem system in _fixedUpdateSystems)
            {
                system.FixedUpdate(this, deltaTime);
            }

            FlushDestroyedEntities();
        }

        private void FlushDestroyedEntities()
        {
            if (_destroyQueue.Count == 0)
            {
                return;
            }

            foreach (EcsEntity entity in _destroyQueue)
            {
                if (entity.TryGet(out TransformComponent transformComponent) &&
                    transformComponent.Transform != null)
                {
                    Object.Destroy(transformComponent.Transform.gameObject);
                }

                _entities.Remove(entity);
            }

            _destroyQueue.Clear();
        }
    }
}
