using UnityEngine;

namespace Chronocaust.Ecs
{
    /// <summary>
    /// Scene authoring for the player entity. All per-character parameters live here,
    /// not in EcsCombatBootstrap, so the bootstrap stays a pure wiring/orchestration component.
    /// </summary>
    public sealed class PlayerAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [Min(0.01f)] public float BaseMovementSpeed = 5f;
        public bool UseIsometricAxes = true;
        public Vector2 IsometricRightAxis = new Vector2(1f, 0.5f);
        public Vector2 IsometricUpAxis = new Vector2(-1f, 0.5f);
    }
}
