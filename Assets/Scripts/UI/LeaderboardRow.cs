using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI
{
    /// <summary>
    /// A single row in the leaderboard scroll list.
    /// Assign this component to the LeaderboardRow prefab.
    ///
    /// Required child objects (by SerializeField):
    ///   rankText   — TMP_Text  — shows "1", "2", etc.
    ///   nameText   — TMP_Text  — player display name
    ///   valueText  — TMP_Text  — formatted score / distance / coins
    ///   background — Image     — tinted to highlight the current player
    /// </summary>
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image    background;

        [Header("Highlight Colors")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.1f, 0.35f);
        [SerializeField] private Color normalColor    = new Color(1f, 1f,    1f,  0.05f);

        public void Setup(LeaderboardEntry entry, LeaderboardCategory category)
        {
            if (rankText)  rankText.text  = entry.Rank.ToString();
            if (nameText)  nameText.text  = entry.DisplayName;
            if (valueText) valueText.text = FormatValue(entry.Value, category);

            if (background)
                background.color = entry.IsCurrentPlayer ? highlightColor : normalColor;

            // Make the name bold for the current player so they stand out
            if (nameText)
                nameText.fontStyle = entry.IsCurrentPlayer ? FontStyles.Bold : FontStyles.Normal;
        }

        private static string FormatValue(double value, LeaderboardCategory category)
        {
            return category switch
            {
                LeaderboardCategory.LongestRun => NumberFormatter.FormatDistance((float)value),
                _                              => NumberFormatter.Format((long)System.Math.Round(value))
            };
        }
    }
}
