using System;
using UnityEngine;
using EmergencyRescue.Core;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Player
{
    public enum DamageSource
    {
        FireHeat,
        SmokeSuffocation,
        Hazard
    }

    /// <summary>
    /// Tracks player health, heat/smoke damage, and death state.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float warningCooldown = 1.2f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float HealthNormalized => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        public bool IsDead { get; private set; }

        public event Action<float, float> OnHealthChanged;
        public event Action<float, DamageSource> OnDamaged;
        public event Action OnDied;

        private float lastWarningTime = -10f;

        private void Awake()
        {
            currentHealth = maxHealth;
            IsDead = false;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(float amount, DamageSource source)
        {
            if (IsDead || amount <= 0f) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke(amount, source);

            if (Time.time - lastWarningTime >= warningCooldown)
            {
                lastWarningTime = Time.time;
                AudioManager.Instance?.PlayWarningBeep();
            }

            if (currentHealth <= 0f)
            {
                IsDead = true;
                OnDied?.Invoke();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetGameState(GameState.GameOver);
                }
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            IsDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
