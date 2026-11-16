using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmergencyRescue.Core;

namespace EmergencyRescue.UI
{
    /// <summary>
    /// Controls the Main Menu UI: Play, VR Mode, Desktop Mode, Instructions, Settings, and Exit.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Sub-Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject instructionsPanel;

        private void Start()
        {
            ShowMainPanel();
        }

        public void ShowMainPanel()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (instructionsPanel != null) instructionsPanel.SetActive(false);
        }

        public void ShowInstructions()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (instructionsPanel != null) instructionsPanel.SetActive(true);
        }

        public void OnPlayClicked()
        {
            GameManager.Instance?.StartGame();
        }

        public void OnExitClicked()
        {
            GameManager.Instance?.QuitApplication();
        }
    }
}
