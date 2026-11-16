using System;
using UnityEngine;
using EmergencyRescue.Player;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Fire
{
    /// <summary>
    /// Controls an individual fire hazard: animated flame/smoke/spark particles,
    /// flickering orange/red lighting, spatial crackling audio, heat proximity damage,
    /// and gradual extinguishing via physical spray collision or raycast.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class FireController : MonoBehaviour
    {
        [Header("Fire Properties")]
        [SerializeField] private float fireIntensity = 100f;
        [SerializeField] private float maxIntensity = 100f;
        [SerializeField] private float spreadRadius = 4.5f;
        [SerializeField] private float damageValue = 18f; // HP/sec at core
        [SerializeField] private bool startLit = true;
        [SerializeField] private bool countsTowardObjective = true;

        [Header("Effects & Audio")]
        [SerializeField] private ParticleSystem flameParticles;
        [SerializeField] private ParticleSystem smokeParticles;
        [SerializeField] private ParticleSystem sparkParticles;
        [SerializeField] private Light fireLight;
        [SerializeField] private AudioSource fireAudioSource;

        [Header("Light Flicker")]
        [SerializeField] private float baseLightIntensity = 6f;
        [SerializeField] private float flickerAmount = 1.8f;
        [SerializeField] private float flickerSpeed = 12f;

        private float baseFlameRate = 45f;
        private float baseSmokeRate = 18f;
        private float baseSparkRate = 15f;
        private float flickerSeed;
        private SphereCollider heatTrigger;

        public float FireIntensity => fireIntensity;
        public float MaxIntensity => maxIntensity;
        public float IntensityNormalized => maxIntensity > 0f ? Mathf.Clamp01(fireIntensity / maxIntensity) : 0f;
        public float SpreadRadius => spreadRadius;
        public float DamageValue => damageValue;
        public bool IsExtinguished => fireIntensity <= 0.01f;
        public bool CountsTowardObjective => countsTowardObjective;

        public event Action<FireController> OnFireExtinguished;
        public event Action<FireController> OnFireIgnited;

        private void Awake()
        {
            flickerSeed = UnityEngine.Random.Range(0f, 100f);
            heatTrigger = GetComponent<SphereCollider>();
            if (heatTrigger != null)
            {
                heatTrigger.isTrigger = true;
            }

            CacheBaseParticleRates();
        }

        private void Start()
        {
            if (fireAudioSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.EnsureClipsLoaded();
                if (fireAudioSource.clip == null)
                {
                    fireAudioSource.clip = AudioManager.Instance.fireCrackleClip;
                }
                fireAudioSource.loop = true;
                fireAudioSource.spatialBlend = 1f;
            }

            if (startLit)
            {
                fireIntensity = Mathf.Clamp(fireIntensity, 10f, maxIntensity);
                if (fireAudioSource != null && !fireAudioSource.isPlaying)
                {
                    fireAudioSource.Play();
                }
            }
            else
            {
                fireIntensity = 0f;
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (IsExtinguished || fireLight == null) return;

            float noise = Mathf.PerlinNoise(flickerSeed, Time.time * flickerSpeed);
            float ratio = IntensityNormalized;
            fireLight.intensity = (baseLightIntensity + (noise - 0.5f) * 2f * flickerAmount) * ratio;
        }

        private void CacheBaseParticleRates()
        {
            if (flameParticles != null)
            {
                baseFlameRate = Mathf.Max(10f, flameParticles.emission.rateOverTime.constant);
            }
            if (smokeParticles != null)
            {
                baseSmokeRate = Mathf.Max(5f, smokeParticles.emission.rateOverTime.constant);
            }
            if (sparkParticles != null)
            {
                baseSparkRate = Mathf.Max(5f, sparkParticles.emission.rateOverTime.constant);
            }
        }

        public void ConfigureReferences(
            ParticleSystem flames,
            ParticleSystem smoke,
            ParticleSystem sparks,
            Light pointLight,
            AudioSource audioSrc,
            bool litOnStart = true,
            bool objectiveFire = true)
        {
            flameParticles = flames;
            smokeParticles = smoke;
            sparkParticles = sparks;
            fireLight = pointLight;
            fireAudioSource = audioSrc;
            startLit = litOnStart;
            countsTowardObjective = objectiveFire;
            CacheBaseParticleRates();
        }

        /// <summary>
        /// Called by FireExtinguisher raycast/spherecast or particle collision.
        /// Gradually lowers fire intensity and scales particle emission until extinguished.
        /// </summary>
        public void ApplyExtinguisherSpray(float extinguishAmount)
        {
            if (IsExtinguished || extinguishAmount <= 0f) return;

            fireIntensity = Mathf.Max(0f, fireIntensity - extinguishAmount);
            UpdateVisuals();

            if (IsExtinguished)
            {
                ExtinguishCompletely();
            }
        }

        public void Ignite(float initialIntensity = 55f)
        {
            if (!IsExtinguished) return;

            fireIntensity = Mathf.Clamp(initialIntensity, 15f, maxIntensity);
            if (flameParticles != null && !flameParticles.isPlaying) flameParticles.Play();
            if (smokeParticles != null && !smokeParticles.isPlaying) smokeParticles.Play();
            if (sparkParticles != null && !sparkParticles.isPlaying) sparkParticles.Play();
            if (fireAudioSource != null && !fireAudioSource.isPlaying) fireAudioSource.Play();

            UpdateVisuals();
            OnFireIgnited?.Invoke(this);
        }

        private void ExtinguishCompletely()
        {
            fireIntensity = 0f;
            if (flameParticles != null) flameParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (smokeParticles != null) smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (sparkParticles != null) sparkParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (fireLight != null) fireLight.enabled = false;
            if (fireAudioSource != null) fireAudioSource.Stop();

            OnFireExtinguished?.Invoke(this);
        }

        private void UpdateVisuals()
        {
            float ratio = IntensityNormalized;

            if (flameParticles != null)
            {
                var em = flameParticles.emission;
                em.rateOverTime = baseFlameRate * ratio;
                var main = flameParticles.main;
                main.startSizeMultiplier = Mathf.Lerp(0.25f, 1f, ratio);
                if (ratio > 0f && !flameParticles.isPlaying) flameParticles.Play();
            }

            if (smokeParticles != null)
            {
                var em = smokeParticles.emission;
                em.rateOverTime = baseSmokeRate * Mathf.Lerp(0.15f, 1f, ratio);
                if (ratio > 0f && !smokeParticles.isPlaying) smokeParticles.Play();
            }

            if (sparkParticles != null)
            {
                var em = sparkParticles.emission;
                em.rateOverTime = baseSparkRate * ratio;
                if (ratio > 0f && !sparkParticles.isPlaying) sparkParticles.Play();
            }

            if (fireLight != null)
            {
                fireLight.enabled = ratio > 0.01f;
                fireLight.intensity = baseLightIntensity * ratio;
            }

            if (fireAudioSource != null)
            {
                fireAudioSource.volume = 0.7f * ratio;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (IsExtinguished) return;

            HealthSystem health = other.GetComponentInParent<HealthSystem>();
            if (health == null) return;

            float radius = heatTrigger != null ? heatTrigger.radius * transform.lossyScale.x : 2.5f;
            float dist = Vector3.Distance(transform.position, other.transform.position);
            float proximity = Mathf.Clamp01(1f - (dist / Mathf.Max(0.5f, radius)));
            float dps = damageValue * IntensityNormalized * Mathf.Max(0.25f, proximity);

            health.TakeDamage(dps * Time.deltaTime, DamageSource.FireHeat);
        }

        private void OnParticleCollision(GameObject other)
        {
            if (IsExtinguished) return;
            if (other.CompareTag("ExtinguisherSpray") || other.GetComponentInParent<FireExtinguisher>() != null)
            {
                ApplyExtinguisherSpray(6f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, spreadRadius);
        }
    }
}
