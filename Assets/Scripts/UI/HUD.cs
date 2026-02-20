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

        private void OnEnable()
        {
            RefreshFromGameManager();
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
            if (healthText != null)
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

        public void RefreshFromGameManager()
        {
            if (GameController.Instance == null) return;

            SetHealth(GameController.Instance.GameHealth);
            SetRunPoints(GameController.Instance.GamePoints);
        }

        public void ForceRefresh()
        {
            RefreshFromGameManager();
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
