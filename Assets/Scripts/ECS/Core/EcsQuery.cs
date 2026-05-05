using System;
using System.Collections.Generic;

namespace Chronocaust.Ecs.Core
{
    // ---------------------------------------------------------------------------
    // Delegate signatures
    // ---------------------------------------------------------------------------

    public delegate void ForEach2<T1, T2>(EntityId id, ref T1 c1, ref T2 c2)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent;

    public delegate void ForEach3<T1, T2, T3>(EntityId id, ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent;

    public delegate void ForEach4<T1, T2, T3, T4>(EntityId id, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent;

    public delegate void ForEach5<T1, T2, T3, T4, T5>(
        EntityId id, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent;

    public delegate void ForEach6<T1, T2, T3, T4, T5, T6>(
        EntityId id, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent;

    public delegate void ForEach7<T1, T2, T3, T4, T5, T6, T7>(
        EntityId id, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent
        where T7 : struct, IEcsComponent;

    public delegate void ForEach8<T1, T2, T3, T4, T5, T6, T7, T8>(
        EntityId id, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7, ref T8 c8)
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent
        where T7 : struct, IEcsComponent
        where T8 : struct, IEcsComponent;

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2>
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Precomputed list of archetypes whose signatures contain all required component types
    /// and none of the excluded types.
    /// ForEach iterates directly over chunk arrays — no Has() checks, no entity-centric dispatch.
    /// Automatically stays up to date via ArchetypeRegistry.OnArchetypeCreated.
    /// Call .Excluding&lt;T&gt;() after construction to add archetype-level exclusion filters.
    /// </summary>
    public sealed class EcsQuery<T1, T2>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty.With<T1>().With<T2>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        /// <summary>
        /// Excludes archetypes that contain TX. Must be called immediately after construction
        /// (before the first ForEach) so the exclusion is baked into archetype matching.
        /// </summary>
        public EcsQuery<T1, T2> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach2<T1, T2> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i], ref chunk.GetRef<T1>(i), ref chunk.GetRef<T2>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty.With<T1>().With<T2>().With<T3>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach3<T1, T2, T3> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3, T4>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3, T4>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty.With<T1>().With<T2>().With<T3>().With<T4>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3, T4> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach4<T1, T2, T3, T4> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i),
                            ref chunk.GetRef<T4>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3, T4, T5>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3, T4, T5>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty
                .With<T1>().With<T2>().With<T3>().With<T4>().With<T5>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3, T4, T5> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach5<T1, T2, T3, T4, T5> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i),
                            ref chunk.GetRef<T4>(i),
                            ref chunk.GetRef<T5>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3, T4, T5, T6>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3, T4, T5, T6>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty
                .With<T1>().With<T2>().With<T3>().With<T4>().With<T5>().With<T6>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach6<T1, T2, T3, T4, T5, T6> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i),
                            ref chunk.GetRef<T4>(i),
                            ref chunk.GetRef<T5>(i),
                            ref chunk.GetRef<T6>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3, T4, T5, T6, T7>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3, T4, T5, T6, T7>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent
        where T7 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty
                .With<T1>().With<T2>().With<T3>().With<T4>().With<T5>().With<T6>().With<T7>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach7<T1, T2, T3, T4, T5, T6, T7> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i),
                            ref chunk.GetRef<T4>(i),
                            ref chunk.GetRef<T5>(i),
                            ref chunk.GetRef<T6>(i),
                            ref chunk.GetRef<T7>(i));
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------------------
    // EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8>
    // ---------------------------------------------------------------------------

    public sealed class EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8>
        where T1 : struct, IEcsComponent
        where T2 : struct, IEcsComponent
        where T3 : struct, IEcsComponent
        where T4 : struct, IEcsComponent
        where T5 : struct, IEcsComponent
        where T6 : struct, IEcsComponent
        where T7 : struct, IEcsComponent
        where T8 : struct, IEcsComponent
    {
        private readonly List<Archetype> _matched;
        private readonly ComponentSignature _required;
        private ComponentSignature _excluded;

        public EcsQuery(ArchetypeRegistry registry)
        {
            _required = ComponentSignature.Empty
                .With<T1>().With<T2>().With<T3>().With<T4>()
                .With<T5>().With<T6>().With<T7>().With<T8>();
            _matched = registry.GetMatching(_required);
            registry.OnArchetypeCreated += OnNewArchetype;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8> Excluding<TX>() where TX : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8> Excluding<TX1, TX2>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        public EcsQuery<T1, T2, T3, T4, T5, T6, T7, T8> Excluding<TX1, TX2, TX3>()
            where TX1 : struct, IEcsComponent
            where TX2 : struct, IEcsComponent
            where TX3 : struct, IEcsComponent
        {
            _excluded = _excluded.With<TX1>().With<TX2>().With<TX3>();
            _matched.RemoveAll(a => a.Signature.HasAny(_excluded));
            return this;
        }

        private void OnNewArchetype(Archetype a)
        {
            if (a.Signature.HasAll(_required) && !a.Signature.HasAny(_excluded))
                _matched.Add(a);
        }

        public void ForEach(ForEach8<T1, T2, T3, T4, T5, T6, T7, T8> action)
        {
            foreach (Archetype archetype in _matched)
            {
                IReadOnlyList<ArchetypeChunk> chunks = archetype.Chunks;
                int chunkCount = chunks.Count;
                for (int c = 0; c < chunkCount; c++)
                {
                    ArchetypeChunk chunk = chunks[c];
                    int count = chunk.Count;
                    for (int i = 0; i < count; i++)
                    {
                        action(chunk.Entities[i],
                            ref chunk.GetRef<T1>(i),
                            ref chunk.GetRef<T2>(i),
                            ref chunk.GetRef<T3>(i),
                            ref chunk.GetRef<T4>(i),
                            ref chunk.GetRef<T5>(i),
                            ref chunk.GetRef<T6>(i),
                            ref chunk.GetRef<T7>(i),
                            ref chunk.GetRef<T8>(i));
                    }
                }
            }
        }
    }
}
