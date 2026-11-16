using System;
using UnityEngine;
using EmergencyRescue.Core;

namespace EmergencyRescue.Player
{
    /// <summary>
    /// Tracks player oxygen supply. Depletes slowly inside the burning building,
    /// accelerates in smoke-filled zones, and causes suffocation damage at 0%.
    /// </summary>
    public class OxygenSystem : MonoBehaviour
    {
        [Header("Oxygen Settings")]
        [SerializeField] private float maxOxygen = 100f;
        [SerializeField] private float currentOxygen = 100f;
        [SerializeField] private float ambientDepletionPerSecond = 0.45f;
        [SerializeField] private float smokeDepletionPerSecond = 4.5f;
        [SerializeField] private float safeZoneRegenPerSecond = 12f;
        [SerializeField] private float suffocationDamagePerSecond = 8f;
        [SerializeField] private float lowOxygenThreshold = 25f;

        private HealthSystem healthSystem;
        private int activeSmokeZones = 0;
        private int activeFreshAirZones = 0;
        private bool wasLowOxygen = false;

        public float MaxOxygen => maxOxygen;
        public float CurrentOxygen => currentOxygen;
        public float OxygenNormalized => maxOxygen > 0f ? Mathf.Clamp01(currentOxygen / maxOxygen) : 0f;
        public bool IsInSmoke => activeSmokeZones > 0;
        public bool IsLowOxygen => currentOxygen <= lowOxygenThreshold;

        public event Action<float, float> OnOxygenChanged;
        public event Action<bool> OnLowOxygenStateChanged;

        private void Awake()
        {
            healthSystem = GetComponent<HealthSystem>();
            currentOxygen = maxOxygen;
        }

        private void Start()
        {
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (healthSystem != null && healthSystem.IsDead) return;

            if (activeFreshAirZones > 0 && activeSmokeZones == 0)
            {
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + safeZoneRegenPerSecond * Time.deltaTime);
            }
            else
            {
                float drain = ambientDepletionPerSecond + (activeSmokeZones > 0 ? smokeDepletionPerSecond : 0f);
                currentOxygen = Mathf.Max(0f, currentOxygen - drain * Time.deltaTime);
            }

            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);

            bool lowNow = IsLowOxygen;
            if (lowNow != wasLowOxygen)
            {
                wasLowOxygen = lowNow;
                OnLowOxygenStateChanged?.Invoke(lowNow);
            }

            if (currentOxygen <= 0f && healthSystem != null)
            {
                healthSystem.TakeDamage(suffocationDamagePerSecond * Time.deltaTime, DamageSource.SmokeSuffocation);
            }
        }

        public void EnterSmokeZone() => activeSmokeZones++;
        public void ExitSmokeZone() => activeSmokeZones = Mathf.Max(0, activeSmokeZones - 1);

        public void EnterFreshAirZone() => activeFreshAirZones++;
        public void ExitFreshAirZone() => activeFreshAirZones = Mathf.Max(0, activeFreshAirZones - 1);

        public void RefillOxygen(float amount)
        {
            currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
            OnOxygenChanged?.Invoke(currentOxygen, maxOxygen);
        }
    }
}
