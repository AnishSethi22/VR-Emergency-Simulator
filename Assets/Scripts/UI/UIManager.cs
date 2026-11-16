using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EmergencyRescue.Core;
using EmergencyRescue.Player;
using EmergencyRescue.Missions;
using EmergencyRescue.Interaction;
using EmergencyRescue.Fire;

namespace EmergencyRescue.UI
{
    /// <summary>
    /// Manages the In-Game HUD, damage/smoke vignette, Pause Menu, and Mission Result Screen.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Slider oxygenBar;
        [SerializeField] private TextMeshProUGUI oxygenText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private TextMeshProUGUI missionObjectivesText;
        [SerializeField] private TextMeshProUGUI rescueCounterText;
        [SerializeField] private TextMeshProUGUI fireStatusText;
        [SerializeField] private GameObject extinguisherPanel;
        [SerializeField] private Slider extinguisherPressureBar;
        [SerializeField] private TextMeshProUGUI extinguisherPressureText;
        [SerializeField] private GameObject interactionHintPanel;
        [SerializeField] private TextMeshProUGUI interactionHintText;
        [SerializeField] private Image damageVignette;
        [SerializeField] private TextMeshProUGUI warningBannerText;

        [Header("Mission Announcement Banner")]
        [SerializeField] private GameObject missionBannerPanel;
        [SerializeField] private TextMeshProUGUI missionBannerTitleText;
        [SerializeField] private TextMeshProUGUI missionBannerSubtitleText;

        [Header("Overlay Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultStatsText;
        [SerializeField] private TextMeshProUGUI resultRatingText;

        private HealthSystem playerHealth;
        private OxygenSystem playerOxygen;
        private InteractionManager interactionManager;
        private PlayerController desktopController;
        private XRPlayerController vrController;

        private float damageFlashAlpha;
        private float bannerDismissTime;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (interactionHintPanel != null) interactionHintPanel.SetActive(false);
            if (extinguisherPanel != null) extinguisherPanel.SetActive(false);
            if (warningBannerText != null) warningBannerText.gameObject.SetActive(false);
            if (missionBannerPanel != null) missionBannerPanel.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStateUpdated += RefreshMissionHUD;
                MissionManager.Instance.OnMissionPhaseChanged += ShowMissionBanner;
                MissionManager.Instance.OnMissionEnded += ShowResultScreen;
            }

            RefreshMissionHUD();
            // Show initial mission instruction popup for 8 seconds
            ShowMissionBanner(MissionPhase.Mission1_FireControl, "MISSION 1: Enter building, pick up extinguisher [E], and spray the lobby fire!");
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStateUpdated -= RefreshMissionHUD;
                MissionManager.Instance.OnMissionPhaseChanged -= ShowMissionBanner;
                MissionManager.Instance.OnMissionEnded -= ShowResultScreen;
            }
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthUI;
                playerHealth.OnDamaged -= TriggerDamageFlash;
            }
            if (playerOxygen != null)
            {
                playerOxygen.OnOxygenChanged -= UpdateOxygenUI;
                playerOxygen.OnLowOxygenStateChanged -= UpdateLowOxygenWarning;
            }
            if (interactionManager != null)
            {
                interactionManager.OnPromptChanged -= UpdateInteractionPrompt;
            }
        }

        public void BindPlayer(
            HealthSystem health,
            OxygenSystem oxygen,
            InteractionManager interactMgr,
            PlayerController desktop,
            XRPlayerController vr)
        {
            playerHealth = health;
            playerOxygen = oxygen;
            interactionManager = interactMgr;
            desktopController = desktop;
            vrController = vr;

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
                playerHealth.OnDamaged += TriggerDamageFlash;
                UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
            if (playerOxygen != null)
            {
                playerOxygen.OnOxygenChanged += UpdateOxygenUI;
                playerOxygen.OnLowOxygenStateChanged += UpdateLowOxygenWarning;
                UpdateOxygenUI(playerOxygen.CurrentOxygen, playerOxygen.MaxOxygen);
            }
            if (interactionManager != null)
            {
                interactionManager.OnPromptChanged += UpdateInteractionPrompt;
            }
        }

        private void Update()
        {
            UpdateTimer();
            UpdateExtinguisherUI();
            UpdateDamageVignette();

            if (missionBannerPanel != null && missionBannerPanel.activeSelf)
            {
                if (Time.time >= bannerDismissTime)
                {
                    missionBannerPanel.SetActive(false);
                }
            }
        }

        public void ShowMissionBanner(MissionPhase phase, string instruction)
        {
            if (missionBannerPanel == null) return;
            missionBannerPanel.SetActive(true);
            bannerDismissTime = Time.time + 8f; // Stay visible for 8 seconds

            if (missionBannerTitleText != null)
            {
                switch (phase)
                {
                    case MissionPhase.Mission1_FireControl:
                        missionBannerTitleText.text = "MISSION 1: FIRE CONTROL (AAG BUJHAYEIN)";
                        missionBannerTitleText.color = new Color(1f, 0.72f, 0.25f);
                        break;
                    case MissionPhase.Mission2_HumanRescue:
                        missionBannerTitleText.text = "MISSION 2: RESCUE CIVILIANS (LOGON KO BACHAYEIN)";
                        missionBannerTitleText.color = new Color(0.35f, 1f, 0.65f);
                        break;
                    case MissionPhase.Mission3_FinalEscape:
                        missionBannerTitleText.text = "MISSION 3: ESCAPE TO EMERGENCY EXIT!";
                        missionBannerTitleText.color = new Color(1f, 0.35f, 0.3f);
                        break;
                }
            }

            if (missionBannerSubtitleText != null)
            {
                missionBannerSubtitleText.text = instruction;
            }
        }

        private void UpdateTimer()
        {
            if (timerText == null || MissionManager.Instance == null) return;
            float t = MissionManager.Instance.TimeRemaining;
            int mins = Mathf.FloorToInt(t / 60f);
            int secs = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{mins:00}:{secs:00}";
            timerText.color = t <= 45f ? new Color(1f, 0.35f, 0.25f) : Color.white;
        }

        private void UpdateHealthUI(float current, float max)
        {
            float norm = max > 0f ? current / max : 0f;
            if (healthBar != null) healthBar.value = norm;
            if (healthText != null) healthText.text = $"{Mathf.CeilToInt(current)}%";
        }

        private void UpdateOxygenUI(float current, float max)
        {
            float norm = max > 0f ? current / max : 0f;
            if (oxygenBar != null) oxygenBar.value = norm;
            if (oxygenText != null) oxygenText.text = $"{Mathf.CeilToInt(current)}%";
        }

        private void UpdateLowOxygenWarning(bool isLow)
        {
            if (warningBannerText != null)
            {
                warningBannerText.gameObject.SetActive(isLow);
                warningBannerText.text = "WARNING: LOW OXYGEN - EXIT SMOKE ZONE!";
            }
        }

        private void TriggerDamageFlash(float amount, DamageSource source)
        {
            damageFlashAlpha = Mathf.Min(0.65f, damageFlashAlpha + 0.35f);
        }

        private void UpdateDamageVignette()
        {
            if (damageVignette == null) return;
            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, Time.deltaTime * 1.1f);
            float lowOxPulse = (playerOxygen != null && playerOxygen.IsLowOxygen)
                ? (0.18f + 0.12f * Mathf.Sin(Time.time * 5f))
                : 0f;
            Color c = damageVignette.color;
            c.a = Mathf.Max(damageFlashAlpha, lowOxPulse);
            damageVignette.color = c;
        }

        private void UpdateExtinguisherUI()
        {
            FireExtinguisher ext = desktopController != null
                ? desktopController.HeldExtinguisher
                : (vrController != null ? vrController.ActiveHeldExtinguisher : null);

            if (extinguisherPanel != null) extinguisherPanel.SetActive(ext != null);
            if (ext == null) return;

            if (extinguisherPressureBar != null) extinguisherPressureBar.value = ext.PressureNormalized;
            if (extinguisherPressureText != null) extinguisherPressureText.text = $"{Mathf.CeilToInt(ext.CurrentPressure)}%";
        }

        private void UpdateInteractionPrompt(string prompt)
        {
            bool hasPrompt = !string.IsNullOrEmpty(prompt);
            if (interactionHintPanel != null) interactionHintPanel.SetActive(hasPrompt);
            if (interactionHintText != null && hasPrompt) interactionHintText.text = prompt;
        }

        private void RefreshMissionHUD()
        {
            if (MissionManager.Instance == null) return;
            if (missionTitleText != null) missionTitleText.text = MissionManager.Instance.GetCurrentMissionTitle();
            if (missionObjectivesText != null) missionObjectivesText.text = MissionManager.Instance.GetCurrentObjectiveText();
            if (rescueCounterText != null)
            {
                rescueCounterText.text = $"Rescued: {MissionManager.Instance.PeopleEvacuatedCount} / {MissionManager.Instance.TotalPeopleCount}";
            }
            if (fireStatusText != null)
            {
                fireStatusText.text = $"Fires Out: {MissionManager.Instance.FiresExtinguishedCount} / {MissionManager.Instance.TotalObjectiveFires}";
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (pausePanel != null) pausePanel.SetActive(state == GameState.Paused);
        }

        private void ShowResultScreen(MissionResultSummary summary)
        {
            if (resultPanel == null) return;
            resultPanel.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = summary.victory ? "MISSION ACCOMPLISHED" : "MISSION FAILED";
                resultTitleText.color = summary.victory ? new Color(0.25f, 0.95f, 0.45f) : new Color(1f, 0.3f, 0.25f);
            }

            if (resultStatsText != null)
            {
                int mins = Mathf.FloorToInt(summary.timeTaken / 60f);
                int secs = Mathf.FloorToInt(summary.timeTaken % 60f);
                resultStatsText.text =
                    $"Total Score: {summary.totalScore}\n" +
                    $"Civilians Rescued: {summary.peopleRescued} / {summary.totalPeople}\n" +
                    $"Fires Extinguished: {summary.firesExtinguished} / {summary.totalFires}\n" +
                    $"Time Taken: {mins:00}:{secs:00}\n" +
                    $"Health Remaining: {Mathf.CeilToInt(summary.healthRemaining)}%\n" +
                    $"Oxygen Remaining: {Mathf.CeilToInt(summary.oxygenRemaining)}%";
            }

            if (resultRatingText != null)
            {
                resultRatingText.text = $"RATING: {summary.rating}";
            }
        }

        // Button Callbacks
        public void OnResumeClicked() => GameManager.Instance?.ResumeGame();
        public void OnRestartClicked() => GameManager.Instance?.RestartLevel();
        public void OnMainMenuClicked() => GameManager.Instance?.ReturnToMainMenu();
    }
}
