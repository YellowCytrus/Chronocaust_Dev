using UnityEngine;

namespace Chronocaust.Ecs
{
    public sealed class GroundWeaponAuthoring : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;

        public WeaponDefinition Definition => definition;

        private void OnValidate()
        {
            if (definition == null)
            {
                Debug.LogWarning("GroundWeaponAuthoring: WeaponDefinition is not assigned.", this);
            }
        }
    }
}
