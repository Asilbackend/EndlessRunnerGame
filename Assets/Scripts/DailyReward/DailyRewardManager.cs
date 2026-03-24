using System;
using System.Collections.Generic;
using UnityEngine;

namespace DailyReward
{
    public enum DailyObjectiveType
    {
        PlayGames,
        TravelTotalDistance,
        CollectCoins,
        ReachScore,
        CollectCoinMagnet,
        CollectExtraHealth,
        CollectDoubleLaneChange,
        CollectDoubleCoin,
        CollectInvincibility,
    }

    [Serializable]
    public class DailyObjective
    {
        public DailyObjectiveType type;
        public int target;
        public int progress;
        public bool IsComplete => progress >= target;
    }

    [Serializable]
    public class DailyObjectivesSaveData
    {
        public string date;
        public List<DailyObjective> objectives = new();
        public bool rewardClaimed;
    }

    /// <summary>
    /// Generates 3 random daily objectives, tracks progress, persists via PlayerPrefs JSON.
    /// Attach to the GameManager GameObject or any persistent object.
    /// </summary>
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        [Tooltip("Number of gems awarded when all daily objectives are completed.")]
        [SerializeField] private int rewardGems = 1;

        private const string PrefsKey = "DailyObjectives";
        private const int ObjectiveCount = 3;

        public DailyObjectivesSaveData Data { get; private set; }

        public bool AllComplete
        {
            get
            {
                if (Data?.objectives == null) return false;
                foreach (var obj in Data.objectives)
                    if (!obj.IsComplete) return false;
                return Data.objectives.Count > 0;
            }
        }

        public event Action OnProgressChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushIfDirty();
        }

        private void OnApplicationQuit()
        {
            FlushIfDirty();
        }

        private void Start()
        {
            LoadOrGenerate();
        }

        public void LoadOrGenerate()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string json = PlayerPrefs.GetString(PrefsKey, "");

            if (!string.IsNullOrEmpty(json))
            {
                Data = JsonUtility.FromJson<DailyObjectivesSaveData>(json);
                if (Data != null && Data.date == today)
                    return; // Still valid for today
            }

            // New day — generate fresh objectives
            Data = new DailyObjectivesSaveData
            {
                date = today,
                objectives = GenerateObjectives(),
                rewardClaimed = false
            };
            Save();
        }

        /// <summary>
        /// Force-reloads progress from PlayerPrefs regardless of the in-memory cache.
        /// Call this when returning from a game session to pick up progress saved there.
        /// </summary>
        public void Reload()
        {
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var loaded = JsonUtility.FromJson<DailyObjectivesSaveData>(json);
                if (loaded != null)
                {
                    Data = loaded;
                    return;
                }
            }
            // Nothing in prefs yet — fall back to generate
            LoadOrGenerate();
        }

        private List<DailyObjective> GenerateObjectives()
        {
            var pool = BuildObjectivePool();

            // Shuffle and pick ObjectiveCount unique types
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var result = new List<DailyObjective>();
            for (int i = 0; i < Mathf.Min(ObjectiveCount, pool.Count); i++)
                result.Add(pool[i]);

            return result;
        }

        private List<DailyObjective> BuildObjectivePool()
        {
            return new List<DailyObjective>
            {
                new() { type = DailyObjectiveType.PlayGames,              target = RandomTarget(3, 7) },
                new() { type = DailyObjectiveType.TravelTotalDistance,     target = RandomTarget(500, 2000, 100) },
                new() { type = DailyObjectiveType.CollectCoins,           target = RandomTarget(50, 200, 10) },
                new() { type = DailyObjectiveType.ReachScore,             target = RandomTarget(100, 500, 50) },
                new() { type = DailyObjectiveType.CollectCoinMagnet,      target = RandomTarget(2, 5) },
                new() { type = DailyObjectiveType.CollectExtraHealth,     target = RandomTarget(2, 5) },
                new() { type = DailyObjectiveType.CollectDoubleLaneChange,target = RandomTarget(2, 5) },
                new() { type = DailyObjectiveType.CollectDoubleCoin,      target = RandomTarget(2, 5) },
                new() { type = DailyObjectiveType.CollectInvincibility,   target = RandomTarget(2, 5) },
            };
        }

        private int RandomTarget(int min, int max, int step = 1)
        {
            if (step <= 1) return UnityEngine.Random.Range(min, max + 1);
            int steps = (max - min) / step;
            return min + UnityEngine.Random.Range(0, steps + 1) * step;
        }

        private bool _dirty;

        private void Save()
        {
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
            _dirty = false;
        }

        /// <summary>Mark data as changed without writing to disk immediately.</summary>
        private void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>Flush pending changes to PlayerPrefs. Call after batched reports.</summary>
        public void FlushIfDirty()
        {
            if (_dirty) Save();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Progress Reporting (called from game systems)
        // ═══════════════════════════════════════════════════════════════

        public void ReportGamePlayed()
        {
            AddProgress(DailyObjectiveType.PlayGames, 1);
        }

        public void ReportDistance(float distance)
        {
            AddProgress(DailyObjectiveType.TravelTotalDistance, Mathf.RoundToInt(distance));
        }

        public void ReportCoinsCollected(int coins)
        {
            AddProgress(DailyObjectiveType.CollectCoins, coins);
        }

        public void ReportScoreReached(int score)
        {
            // For "reach score N" we set progress to the max score achieved
            SetProgressMax(DailyObjectiveType.ReachScore, score);
        }

        public void ReportPowerupCollected(PowerupType powerupType)
        {
            var objType = powerupType switch
            {
                PowerupType.CoinMagnet      => DailyObjectiveType.CollectCoinMagnet,
                PowerupType.ExtraHealth      => DailyObjectiveType.CollectExtraHealth,
                PowerupType.DoubleLaneChange => DailyObjectiveType.CollectDoubleLaneChange,
                PowerupType.DoubleCoin       => DailyObjectiveType.CollectDoubleCoin,
                PowerupType.Invincibility    => DailyObjectiveType.CollectInvincibility,
                _ => (DailyObjectiveType)(-1)
            };

            if ((int)objType >= 0)
                AddProgress(objType, 1);
        }

        private void AddProgress(DailyObjectiveType type, int amount)
        {
            if (Data?.objectives == null) return;
            foreach (var obj in Data.objectives)
            {
                if (obj.type == type && !obj.IsComplete)
                {
                    obj.progress = Mathf.Min(obj.progress + amount, obj.target);
                    MarkDirty();
                    OnProgressChanged?.Invoke();
                    return;
                }
            }
        }

        private void SetProgressMax(DailyObjectiveType type, int value)
        {
            if (Data?.objectives == null) return;
            foreach (var obj in Data.objectives)
            {
                if (obj.type == type && !obj.IsComplete)
                {
                    if (value > obj.progress)
                    {
                        obj.progress = Mathf.Min(value, obj.target);
                        MarkDirty();
                        OnProgressChanged?.Invoke();
                    }
                    return;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Reward Claim
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Claims the daily reward (1 gem). Returns true if successful.
        /// </summary>
        public bool TryClaimReward()
        {
            if (!AllComplete || Data.rewardClaimed) return false;

            Data.rewardClaimed = true;
            Save();

            GameManager.Instance?.AddGems(rewardGems);
            OnProgressChanged?.Invoke();
            return true;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Display Helpers
        // ═══════════════════════════════════════════════════════════════

        public static string GetObjectiveDescription(DailyObjective obj)
        {
            return obj.type switch
            {
                DailyObjectiveType.PlayGames              => $"Play {obj.target} games",
                DailyObjectiveType.TravelTotalDistance     => $"Travel {obj.target}m total distance",
                DailyObjectiveType.CollectCoins           => $"Collect {obj.target} coins",
                DailyObjectiveType.ReachScore             => $"Reach a score of {obj.target}",
                DailyObjectiveType.CollectCoinMagnet      => $"Collect Coin Magnet {obj.target} times",
                DailyObjectiveType.CollectExtraHealth     => $"Collect Extra Health {obj.target} times",
                DailyObjectiveType.CollectDoubleLaneChange=> $"Collect Double Lane {obj.target} times",
                DailyObjectiveType.CollectDoubleCoin      => $"Collect Double Coin {obj.target} times",
                DailyObjectiveType.CollectInvincibility   => $"Collect Invincibility {obj.target} times",
                _ => "Unknown objective"
            };
        }
    }
}
