using UnityEngine;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Interaction
{
    /// <summary>
    /// Flickers emergency ceiling/wall lights and optionally pulses an emissive beacon
    /// and spatialized fire alarm klaxon.
    /// </summary>
    public class EmergencyLightFlicker : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float minIntensity = 0.25f;
        [SerializeField] private float maxIntensity = 2.4f;
        [SerializeField] private float pulseSpeed = 4.5f;
        [SerializeField] private bool erraticFlicker = false;
        [SerializeField] private bool playAlarmAudio = false;

        private float seed;

        private void Awake()
        {
            if (targetLight == null) targetLight = GetComponent<Light>();
            seed = Random.Range(0f, 50f);
        }

        private void Start()
        {
            if (playAlarmAudio && AudioManager.Instance != null)
            {
                AudioManager.Instance.EnsureClipsLoaded();
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = AudioManager.Instance.emergencyAlarmClip;
                src.loop = true;
                src.spatialBlend = 1f;
                src.volume = 0.35f;
                src.minDistance = 2f;
                src.maxDistance = 24f;
                src.Play();
            }
        }

        private void Update()
        {
            if (targetLight == null) return;

            if (erraticFlicker)
            {
                float n = Mathf.PerlinNoise(seed, Time.time * pulseSpeed * 3f);
                targetLight.intensity = n > 0.3f ? Mathf.Lerp(minIntensity, maxIntensity, n) : minIntensity * 0.2f;
            }
            else
            {
                float t = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + seed);
                targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            }
        }
    }
}
