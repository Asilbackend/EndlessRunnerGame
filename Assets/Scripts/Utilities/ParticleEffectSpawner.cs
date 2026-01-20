using UnityEngine;
using World;

namespace Utilities
{
    public static class ParticleEffectSpawner
    {
        public static GameObject SpawnParticleEffect(ParticleEffectType effectType, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (GameController.Instance == null)
            {
                Debug.LogWarning("ParticleEffectSpawner: GameController.Instance is null. Cannot spawn particle effect.");
                return null;
            }

            var particleEffectsSO = GameController.Instance.ParticleEffectsSO;
            if (particleEffectsSO == null)
            {
                Debug.LogWarning("ParticleEffectSpawner: ParticleEffectsSO is not assigned in GameManager. Cannot spawn particle effect.");
                return null;
            }

            GameObject particlePrefab = particleEffectsSO.GetParticleEffect(effectType);
            if (particlePrefab == null)
            {
                Debug.LogWarning($"ParticleEffectSpawner: No particle effect found for type '{effectType}'.");
                return null;
            }

            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            GameObject particleInstance = Object.Instantiate(particlePrefab, position, rotation, parent);

            // Auto-destroy the particle effect after it finishes playing
            ParticleSystem particleSystem = particleInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Object.Destroy(particleInstance, lifetime);
            }
            else
            {
                // Check for ParticleSystem in children
                particleSystem = particleInstance.GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null)
                {
                    float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                    Object.Destroy(particleInstance, lifetime);
                }
                else
                {
                    // If no ParticleSystem found, destroy after a default time
                    Object.Destroy(particleInstance, 5f);
                }
            }

            return particleInstance;
        }

        /// <summary>
        /// Spawns a random particle effect from all available effects of the specified type.
        /// </summary>
        public static GameObject SpawnRandomParticleEffect(ParticleEffectType effectType, Vector3 position, Quaternion rotation = default, Transform parent = null)
        {
            if (GameController.Instance == null)
            {
                Debug.LogWarning("ParticleEffectSpawner: GameController.Instance is null. Cannot spawn particle effect.");
                return null;
            }

            var particleEffectsSO = GameController.Instance.ParticleEffectsSO;
            if (particleEffectsSO == null)
            {
                Debug.LogWarning("ParticleEffectSpawner: ParticleEffectsSO is not assigned in GameManager. Cannot spawn particle effect.");
                return null;
            }

            GameObject particlePrefab = particleEffectsSO.GetRandomParticleEffect(effectType);
            if (particlePrefab == null)
            {
                Debug.LogWarning($"ParticleEffectSpawner: No particle effect found for type '{effectType}'.");
                return null;
            }

            if (rotation == default)
            {
                rotation = Quaternion.identity;
            }

            GameObject particleInstance = Object.Instantiate(particlePrefab, position, rotation, parent);

            // Auto-destroy the particle effect after it finishes playing
            ParticleSystem particleSystem = particleInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                Object.Destroy(particleInstance, lifetime);
            }
            else
            {
                // Check for ParticleSystem in children
                particleSystem = particleInstance.GetComponentInChildren<ParticleSystem>();
                if (particleSystem != null)
                {
                    float lifetime = particleSystem.main.duration + particleSystem.main.startLifetime.constantMax;
                    Object.Destroy(particleInstance, lifetime);
                }
                else
                {
                    // If no ParticleSystem found, destroy after a default time
                    Object.Destroy(particleInstance, 5f);
                }
            }

            return particleInstance;
        }
    }
}
