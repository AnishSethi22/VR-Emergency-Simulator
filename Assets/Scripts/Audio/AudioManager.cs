using UnityEngine;

namespace EmergencyRescue.Audio
{
    /// <summary>
    /// Centralized audio manager supporting both 2D UI/alert audio and 3D spatialized VR audio.
    /// Automatically synthesizes fallback clips via ProceduralAudioLibrary if no clips are assigned.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Optional Assigned Clips (Procedural fallbacks used if null)")]
        public AudioClip fireCrackleClip;
        public AudioClip extinguisherSprayClip;
        public AudioClip emergencyAlarmClip;
        public AudioClip footstepClip;
        public AudioClip doorClip;
        public AudioClip rescueChimeClip;
        public AudioClip missionCompleteClip;
        public AudioClip warningBeepClip;
        public AudioClip pickupClip;

        private AudioSource uiSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.spatialBlend = 0f;

            EnsureClipsLoaded();
        }

        public void EnsureClipsLoaded()
        {
            if (fireCrackleClip == null) fireCrackleClip = ProceduralAudioLibrary.CreateFireCrackleLoop();
            if (extinguisherSprayClip == null) extinguisherSprayClip = ProceduralAudioLibrary.CreateExtinguisherSprayLoop();
            if (emergencyAlarmClip == null) emergencyAlarmClip = ProceduralAudioLibrary.CreateEmergencyAlarmLoop();
            if (footstepClip == null) footstepClip = ProceduralAudioLibrary.CreateFootstepClip();
            if (doorClip == null) doorClip = ProceduralAudioLibrary.CreateDoorClip();
            if (rescueChimeClip == null) rescueChimeClip = ProceduralAudioLibrary.CreateRescueChimeClip();
            if (missionCompleteClip == null) missionCompleteClip = ProceduralAudioLibrary.CreateMissionCompleteClip();
            if (warningBeepClip == null) warningBeepClip = ProceduralAudioLibrary.CreateWarningBeepClip();
            if (pickupClip == null) pickupClip = ProceduralAudioLibrary.CreatePickupClip();
        }

        public void Play2D(AudioClip clip, float volume = 0.8f)
        {
            if (clip == null || uiSource == null) return;
            uiSource.PlayOneShot(clip, volume);
        }

        public void Play3D(AudioClip clip, Vector3 position, float volume = 0.85f, float minDistance = 1.5f, float maxDistance = 18f)
        {
            if (clip == null) return;
            GameObject temp = new GameObject("TempAudio3D");
            temp.transform.position = position;
            AudioSource src = temp.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.Play();
            Destroy(temp, clip.length + 0.1f);
        }

        public void PlayFootstep(Vector3 position, float volume = 0.45f) => Play3D(footstepClip, position, volume, 0.5f, 8f);
        public void PlayDoorSound(Vector3 position) => Play3D(doorClip, position, 0.75f, 1f, 14f);
        public void PlayPickupSound(Vector3 position) => Play3D(pickupClip, position, 0.65f, 0.5f, 10f);
        public void PlayRescueChime() => Play2D(rescueChimeClip, 0.85f);
        public void PlayMissionComplete() => Play2D(missionCompleteClip, 0.9f);
        public void PlayWarningBeep() => Play2D(warningBeepClip, 0.5f);
    }
}
