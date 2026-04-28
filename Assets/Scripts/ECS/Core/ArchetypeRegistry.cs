using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    /// <summary>
    /// Central map from ComponentSignature → Archetype.
    /// Notifies registered queries when a new archetype is created so they can update their matching list.
    /// </summary>
    public sealed class ArchetypeRegistry
    {
        private readonly Dictionary<ComponentSignature, Archetype> _map =
            new Dictionary<ComponentSignature, Archetype>(16);

        private readonly List<Archetype> _all = new List<Archetype>(16);

        /// <summary>Fired when a brand-new Archetype is registered (so queries can re-evaluate).</summary>
        public event Action<Archetype> OnArchetypeCreated;

        public Archetype GetOrCreate(ComponentSignature signature)
        {
            if (_map.TryGetValue(signature, out Archetype existing))
            {
                return existing;
            }

            var archetype = new Archetype(signature);
            _map[signature] = archetype;
            _all.Add(archetype);
            OnArchetypeCreated?.Invoke(archetype);
            return archetype;
        }

        /// <summary>Returns all archetypes whose signature contains every bit of <paramref name="querySignature"/>.</summary>
        public List<Archetype> GetMatching(ComponentSignature querySignature)
        {
            var result = new List<Archetype>(4);
            foreach (Archetype a in _all)
            {
                if (a.Signature.HasAll(querySignature))
                {
                    result.Add(a);
                }
            }

            return result;
        }

        public IReadOnlyList<Archetype> All => _all;
    }
}
