using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Allocates versioned entity slots. Each slot has an integer Index and a Generation
    /// that increments on recycle, making stale EntityIds detectable.
    /// </summary>
    public sealed class EntityPool
    {
        private int[] _generations;
        private bool[] _alive;
        private readonly Stack<int> _free = new Stack<int>(64);
        private int _capacity;
        private int _highWaterMark;

        public EntityPool(int initialCapacity = 256)
        {
            _capacity = Math.Max(16, initialCapacity);
            _generations = new int[_capacity];
            _alive = new bool[_capacity];
        }

        public EntityId Create()
        {
            int index;
            if (_free.Count > 0)
            {
                index = _free.Pop();
            }
            else
            {
                if (_highWaterMark >= _capacity)
                {
                    Grow();
                }

                index = _highWaterMark++;
            }

            _alive[index] = true;
            return new EntityId(index, _generations[index]);
        }

        public void Recycle(EntityId id)
        {
            if (!IsAlive(id)) return;
            _alive[id.Index] = false;
            _generations[id.Index]++;
            _free.Push(id.Index);
        }

        public bool IsAlive(EntityId id)
        {
            if (!id.IsValid || id.Index >= _capacity) return false;
            return _alive[id.Index] && _generations[id.Index] == id.Generation;
        }

        private void Grow()
        {
            int newCap = _capacity * 2;
            Array.Resize(ref _generations, newCap);
            Array.Resize(ref _alive, newCap);
            _capacity = newCap;
        }
    }
}
