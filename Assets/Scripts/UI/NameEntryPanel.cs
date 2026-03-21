using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Shown the first time a player opens the leaderboard (no display name set yet).
    /// The player types a name and confirms. The panel then invokes OnNameConfirmed
    /// so the LeaderboardPanel can proceed.
    /// </summary>
    public class NameEntryPanel : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private Button         confirmButton;

        [Header("Validation")]
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private int      minLength = 3;
        [SerializeField] private int      maxLength = 16;

        /// <summary>Invoked with the trimmed display name when the player confirms.</summary>
        public event Action<string> OnNameConfirmed;

        private void Awake()
        {
            if (confirmButton)   confirmButton.onClick.AddListener(OnConfirmClicked);
            if (nameInputField)
            {
                nameInputField.characterLimit = maxLength;
                nameInputField.onValueChanged.AddListener(_ => HideError());
            }

            if (errorText) errorText.gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (nameInputField)
            {
                nameInputField.text = "";
                nameInputField.ActivateInputField();
            }
            HideError();
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuOpen);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            AudioManager.Instance?.PlaySFX(AudioEventSFX.MenuClose);
        }

        private void OnConfirmClicked()
        {
            AudioManager.Instance?.PlaySFX(AudioEventSFX.ButtonClick);

            string name = nameInputField != null ? nameInputField.text.Trim() : "";

            if (name.Length < minLength)
            {
                ShowError($"Name must be at least {minLength} characters.");
                return;
            }

            FirebaseLeaderboardService.Instance?.SetDisplayName(name);
            Hide();
            OnNameConfirmed?.Invoke(name);
        }

        private void ShowError(string message)
        {
            if (errorText == null) return;
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        private void HideError()
        {
            if (errorText) errorText.gameObject.SetActive(false);
        }
    }
}
