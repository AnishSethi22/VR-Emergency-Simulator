using UnityEngine;
using EmergencyRescue.Core;
using EmergencyRescue.Missions;
using EmergencyRescue.UI;

namespace EmergencyRescue.Player
{
    /// <summary>
    /// Activates either the Desktop Player or the VR XR Origin based on GameManager.SelectedMode
    /// and binds Health, Oxygen, and HUD references.
    /// </summary>
    public class PlayerModeManager : MonoBehaviour
    {
        [SerializeField] private GameObject desktopPlayerRoot;
        [SerializeField] private GameObject vrPlayerRoot;
        [SerializeField] private UIManager uiManager;

        private void Start()
        {
            PlayModeType mode = GameManager.Instance != null
                ? GameManager.Instance.SelectedMode
                : PlayModeType.Desktop;

            bool useVR = (mode == PlayModeType.VR);

            if (desktopPlayerRoot != null) desktopPlayerRoot.SetActive(!useVR);
            if (vrPlayerRoot != null) vrPlayerRoot.SetActive(useVR);

            GameObject activeRoot = useVR ? vrPlayerRoot : desktopPlayerRoot;
            if (activeRoot != null)
            {
                HealthSystem health = activeRoot.GetComponent<HealthSystem>();
                OxygenSystem oxygen = activeRoot.GetComponent<OxygenSystem>();
                MissionManager.Instance?.RegisterActivePlayer(health, oxygen);

                if (uiManager != null)
                {
                    uiManager.BindPlayer(
                        health,
                        oxygen,
                        activeRoot.GetComponent<Interaction.InteractionManager>(),
                        activeRoot.GetComponent<PlayerController>(),
                        activeRoot.GetComponent<XRPlayerController>());
                }
            }

            GameManager.Instance?.UpdateCursorState(GameState.Playing);
        }

        public void Configure(GameObject desktopRoot, GameObject vrRoot, UIManager ui)
        {
            desktopPlayerRoot = desktopRoot;
            vrPlayerRoot = vrRoot;
            uiManager = ui;
        }
    }
}
