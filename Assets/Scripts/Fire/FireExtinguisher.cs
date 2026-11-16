using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using EmergencyRescue.Interaction;
using EmergencyRescue.Missions;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Fire
{
    /// <summary>
    /// Interactive Fire Extinguisher supporting Desktop & VR grabbing, trigger-based foam/CO2 spray,
    /// limited pressure with 3D gauge indicator, and physics/spherecast fire extinguishing.
    /// </summary>
    public class FireExtinguisher : GrabbableObject
    {
        [Header("Extinguisher Components")]
        [SerializeField] private Transform nozzleTransform;
        [SerializeField] private ParticleSystem sprayParticles;
        [SerializeField] private AudioSource sprayAudioSource;
        [SerializeField] private Transform pressureBarIndicator;

        [Header("Spray & Pressure")]
        [SerializeField] private float maxPressure = 100f;
        [SerializeField] private float currentPressure = 100f;
        [SerializeField] private float pressureDrainPerSecond = 8.5f;
        [SerializeField] private float sprayRange = 6.5f;
        [SerializeField] private float sprayRadius = 0.55f;
        [SerializeField] private float extinguishRatePerSecond = 42f;

        private bool isSpraying;

        public float CurrentPressure => currentPressure;
        public float MaxPressure => maxPressure;
        public float PressureNormalized => maxPressure > 0f ? Mathf.Clamp01(currentPressure / maxPressure) : 0f;
        public bool IsSpraying => isSpraying && currentPressure > 0f;

        public event Action<float, float> OnPressureChanged;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (xrGrab != null)
            {
                xrGrab.activated.AddListener(OnVRActivated);
                xrGrab.deactivated.AddListener(OnVRDeactivated);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (xrGrab != null)
            {
                xrGrab.activated.RemoveListener(OnVRActivated);
                xrGrab.deactivated.RemoveListener(OnVRDeactivated);
            }
        }

        private void Start()
        {
            currentPressure = maxPressure;
            if (sprayAudioSource != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.EnsureClipsLoaded();
                if (sprayAudioSource.clip == null)
                {
                    sprayAudioSource.clip = AudioManager.Instance.extinguisherSprayClip;
                }
                sprayAudioSource.loop = true;
                sprayAudioSource.spatialBlend = 1f;
            }
            UpdatePressureVisual();
        }

        public void ConfigureReferences(Transform nozzle, ParticleSystem particles, AudioSource audioSrc, Transform gaugeBar)
        {
            nozzleTransform = nozzle;
            sprayParticles = particles;
            sprayAudioSource = audioSrc;
            pressureBarIndicator = gaugeBar;
        }

        public override void PickUpDesktop(Transform holdAnchor)
        {
            base.PickUpDesktop(holdAnchor);
            MissionManager.Instance?.NotifyExtinguisherPickedUp();
        }

        public override void DropDesktop(Vector3 releaseVelocity)
        {
            SetSpraying(false);
            base.DropDesktop(releaseVelocity);
        }

        protected override void OnVRGrabbed(SelectEnterEventArgs args)
        {
            base.OnVRGrabbed(args);
            MissionManager.Instance?.NotifyExtinguisherPickedUp();
        }

        protected override void OnVRReleased(SelectExitEventArgs args)
        {
            SetSpraying(false);
            base.OnVRReleased(args);
        }

        private void OnVRActivated(ActivateEventArgs args) => SetSpraying(true);
        private void OnVRDeactivated(DeactivateEventArgs args) => SetSpraying(false);

        public void SetSpraying(bool active)
        {
            if (active && currentPressure <= 0f)
            {
                active = false;
            }

            if (isSpraying == active) return;
            isSpraying = active;

            if (sprayParticles != null)
            {
                if (isSpraying) sprayParticles.Play();
                else sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (sprayAudioSource != null)
            {
                if (isSpraying && !sprayAudioSource.isPlaying) sprayAudioSource.Play();
                else if (!isSpraying && sprayAudioSource.isPlaying) sprayAudioSource.Stop();
            }
        }

        public void Refill()
        {
            currentPressure = maxPressure;
            UpdatePressureVisual();
            OnPressureChanged?.Invoke(currentPressure, maxPressure);
            AudioManager.Instance?.PlayPickupSound(transform.position);
        }

        private void Update()
        {
            if (!isSpraying) return;

            if (currentPressure <= 0f || !IsHeld)
            {
                SetSpraying(false);
                return;
            }

            currentPressure = Mathf.Max(0f, currentPressure - pressureDrainPerSecond * Time.deltaTime);
            UpdatePressureVisual();
            OnPressureChanged?.Invoke(currentPressure, maxPressure);

            PerformSprayHitDetection();

            if (currentPressure <= 0f)
            {
                SetSpraying(false);
            }
        }

        private void PerformSprayHitDetection()
        {
            Transform originTf = nozzleTransform != null ? nozzleTransform : transform;
            Vector3 origin = originTf.position;
            Vector3 dir = originTf.forward;

            RaycastHit[] hits = Physics.SphereCastAll(origin, sprayRadius, dir, sprayRange, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                FireController fire = hits[i].collider.GetComponentInParent<FireController>();
                if (fire == null || fire.IsExtinguished) continue;

                // Ensure no solid wall blocks the spray stream
                Vector3 toFire = fire.transform.position - origin;
                float dist = toFire.magnitude;
                if (Physics.Raycast(origin, toFire.normalized, out RaycastHit wallHit, dist, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (wallHit.collider.GetComponentInParent<FireController>() == null &&
                        wallHit.collider.GetComponentInParent<FireExtinguisher>() == null)
                    {
                        continue;
                    }
                }

                float distanceFactor = Mathf.Lerp(1.25f, 0.55f, Mathf.Clamp01(dist / sprayRange));
                fire.ApplyExtinguisherSpray(extinguishRatePerSecond * distanceFactor * Time.deltaTime);
            }
        }

        private void UpdatePressureVisual()
        {
            if (pressureBarIndicator != null)
            {
                Vector3 s = pressureBarIndicator.localScale;
                s.y = Mathf.Max(0.02f, PressureNormalized);
                pressureBarIndicator.localScale = s;
            }
        }
    }
}
