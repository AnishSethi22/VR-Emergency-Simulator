using UnityEngine;

namespace EmergencyRescue.Audio
{
    /// <summary>
    /// Generates procedural AudioClips at runtime so all sound effects work out-of-the-box
    /// without relying on external audio files.
    /// </summary>
    public static class ProceduralAudioLibrary
    {
        private const int SampleRate = 22050;

        public static AudioClip CreateFireCrackleLoop(float duration = 3f)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(101);

            float lowPass = 0f;
            float crackleEnv = 0f;

            for (int i = 0; i < samples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Warm rumble via simple low-pass
                lowPass = lowPass * 0.92f + white * 0.08f;

                // Random sharp crackles
                if (rng.NextDouble() < 0.0025)
                {
                    crackleEnv = 0.8f + (float)rng.NextDouble() * 0.2f;
                }
                crackleEnv *= 0.985f;

                float sample = lowPass * 0.55f + white * crackleEnv * 0.5f;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("FireCrackleLoop", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateExtinguisherSprayLoop(float duration = 2f)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(202);

            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                // High-pass / hiss emphasis
                float hiss = white - prev * 0.45f;
                prev = white;
                float t = (float)i / SampleRate;
                float mod = 0.9f + 0.1f * Mathf.Sin(t * 25f);
                data[i] = Mathf.Clamp(hiss * 0.35f * mod, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("ExtinguisherSprayLoop", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateEmergencyAlarmLoop(float duration = 2f)
        {
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float cycle = t % 1.0f;
                // Two-tone klaxon pulse (0.0-0.35s active, 0.35-0.5s pause, 0.5-0.85s active)
                bool active = (cycle < 0.35f) || (cycle >= 0.5f && cycle < 0.85f);
                float env = active ? 0.35f : 0f;
                float freq = Mathf.Lerp(620f, 880f, Mathf.PingPong(t * 4f, 1f));
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.35f * Mathf.Sin(4f * Mathf.PI * freq * t);
                data[i] = Mathf.Clamp(wave * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("EmergencyAlarmLoop", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateFootstepClip()
        {
            float duration = 0.14f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(303);

            float lp = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float env = Mathf.Exp(-t * 18f);
                float thud = Mathf.Sin(2f * Mathf.PI * 95f * ((float)i / SampleRate)) * 0.6f;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp = lp * 0.8f + noise * 0.2f;
                data[i] = Mathf.Clamp((thud + lp * 0.7f) * env * 0.65f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("Footstep", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateDoorClip()
        {
            float duration = 0.45f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            System.Random rng = new System.Random(404);

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration));
                float creak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(180f, 120f, t / duration) * t);
                float latch = (t < 0.08f) ? (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f : 0f;
                data[i] = Mathf.Clamp((creak * 0.28f + latch) * env, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create("DoorOpen", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateRescueChimeClip()
        {
            float duration = 0.6f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float f1 = t < 0.2f ? 523.25f : (t < 0.4f ? 659.25f : 783.99f); // C5 - E5 - G5
                float env = Mathf.Exp(-((t % 0.2f) * 6f)) * 0.45f;
                data[i] = Mathf.Sin(2f * Mathf.PI * f1 * t) * env;
            }

            AudioClip clip = AudioClip.Create("RescueChime", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateMissionCompleteClip()
        {
            float duration = 1.1f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                int idx = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / 0.22f));
                float localT = t - idx * 0.22f;
                float env = Mathf.Exp(-localT * 4.5f) * 0.4f;
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env;
            }

            AudioClip clip = AudioClip.Create("MissionComplete", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateWarningBeepClip()
        {
            float duration = 0.22f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = (t < 0.16f) ? 0.35f : 0f;
                data[i] = Mathf.Sin(2f * Mathf.PI * 980f * t) * env;
            }

            AudioClip clip = AudioClip.Create("WarningBeep", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreatePickupClip()
        {
            float duration = 0.12f;
            int samples = Mathf.RoundToInt(SampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 25f) * 0.45f;
                float freq = Mathf.Lerp(240f, 440f, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }

            AudioClip clip = AudioClip.Create("Pickup", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
