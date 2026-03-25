using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World
{
    public enum ParticleEffectType
    {
        ObstacleImpact,
        PlayerLanding,

        // Powerups
        PowerupCollect,
        PowerupMagnet,
        PowerupHealth,
        PowerupDoubleLane,
        PowerupDoubleCoin,
        PowerupInvincibility,

        // Challenge
        ChallengeTapBurst,
    }

    [System.Serializable]
    public class ParticleEffectData
    {
        [Header("Effect Properties")]
        public ParticleEffectType effectType;
        public GameObject particlePrefab;
        [Tooltip("Optional: If true, will randomly select from multiple prefabs of the same type")]
        public bool useRandomSelection = false;
    }

    [CreateAssetMenu(fileName = "ParticleEffects", menuName = "World/Particle Effects", order = 3)]
    public class ParticleEffectsSO : ScriptableObject
    {
        [Header("Particle Effect Configurations")]
        [Tooltip("List of all particle effects. Each effect type can have multiple prefabs for variety.")]
        public List<ParticleEffectData> particleEffects = new List<ParticleEffectData>();

        /// <summary>
        /// Gets a particle effect prefab by type. Returns the first matching prefab.
        /// </summary>
        public GameObject GetParticleEffect(ParticleEffectType effectType)
        {
            if (particleEffects == null || particleEffects.Count == 0)
            {
                Debug.LogWarning($"ParticleEffectsSO '{name}' has no particle effect configurations.");
                return null;
            }

            var matchingEffects = particleEffects.Where(effect => effect.effectType == effectType && effect.particlePrefab != null).ToList();

            if (matchingEffects.Count == 0)
            {
                Debug.LogWarning($"ParticleEffectsSO '{name}' has no particle effect configuration for type '{effectType}'.");
                return null;
            }

            // If there's only one or random selection is disabled, return the first one
            if (matchingEffects.Count == 1 || !matchingEffects[0].useRandomSelection)
            {
                return matchingEffects[0].particlePrefab;
            }

            // Random selection from multiple prefabs of the same type
            int randomIndex = Random.Range(0, matchingEffects.Count);
            return matchingEffects[randomIndex].particlePrefab;
        }

        /// <summary>
        /// Gets all particle effect prefabs of a specific type (useful for random selection).
        /// </summary>
        public List<GameObject> GetParticleEffects(ParticleEffectType effectType)
        {
            if (particleEffects == null || particleEffects.Count == 0)
            {
                return new List<GameObject>();
            }

            return particleEffects
                .Where(effect => effect.effectType == effectType && effect.particlePrefab != null)
                .Select(effect => effect.particlePrefab)
                .ToList();
        }

        /// <summary>
        /// Gets a random particle effect prefab from all effects of the specified type.
        /// </summary>
        public GameObject GetRandomParticleEffect(ParticleEffectType effectType)
        {
            var effects = GetParticleEffects(effectType);
            
            if (effects.Count == 0)
            {
                Debug.LogWarning($"ParticleEffectsSO '{name}' has no particle effect configuration for type '{effectType}'.");
                return null;
            }

            int randomIndex = Random.Range(0, effects.Count);
            return effects[randomIndex];
        }
    }
}
