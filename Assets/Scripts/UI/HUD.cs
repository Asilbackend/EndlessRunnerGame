using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text runPointsText;
        [SerializeField] private TMP_Text mapName;
        [SerializeField] private TMP_Text distanceMeterText;

        private void OnEnable()
        {
            RefreshFromGameController();
        }

        public void SetHealth(int health)
        {
            if (healthText != null)
            {
                healthText.text = health.ToString();
            }
        }

        public void SetMapName(string name)
        {
            if (mapName != null)
            {
                mapName.text = name;
            }
        }

        public void SetRunPoints(int points)
        {
            if (runPointsText != null)
            {
                runPointsText.text = points.ToString();
            }
        }

        public void AddRunPoints(int amount)
        {
            if (runPointsText == null) return;

            if (int.TryParse(runPointsText.text, out int current))
            {
                current += amount;
                runPointsText.text = current.ToString();
            }
            else
            {
                runPointsText.text = amount.ToString();
            }
        }

        public void SetDistanceMeter(float distance)
        {
            if (distanceMeterText != null)
            {
                // Convert distance to meters (assuming world units = meters)
                distanceMeterText.text = $"{distance:F1}m";
            }
        }

        public void RefreshFromGameController()
        {
            if (GameController.Instance == null) return;

            SetHealth(GameController.Instance.GameHealth);
            SetRunPoints(GameController.Instance.GamePoints);
        }

        public void ForceRefresh()
        {
            RefreshFromGameController();
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }   
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
