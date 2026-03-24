using DailyReward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DailyRewardCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private GameObject checkmark;

        public void Populate(DailyObjective obj)
        {
            gameObject.SetActive(true);

            if (descriptionText)
                descriptionText.text = DailyRewardManager.GetObjectiveDescription(obj);

            if (progressBar)
            {
                progressBar.maxValue = obj.target;
                progressBar.value = obj.progress;
            }

            if (progressText)
                progressText.text = $"{obj.progress}/{obj.target}";

            if (checkmark)
                checkmark.SetActive(obj.IsComplete);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
