using UnityEngine;

namespace EmergencyRescue.Missions
{
    public struct MissionResultSummary
    {
        public bool victory;
        public int firesExtinguished;
        public int totalFires;
        public int peopleRescued;
        public int totalPeople;
        public float timeTaken;
        public float timeRemaining;
        public float healthRemaining;
        public float oxygenRemaining;
        public int totalScore;
        public string rating;
    }

    /// <summary>
    /// Computes mission performance score and letter grade based on fires extinguished,
    /// civilians rescued, time remaining, health, and oxygen.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Weights")]
        [SerializeField] private int pointsPerFire = 300;
        [SerializeField] private int pointsPerRescue = 500;
        [SerializeField] private float pointsPerSecondRemaining = 5f;
        [SerializeField] private float pointsPerHealth = 4f;
        [SerializeField] private float pointsPerOxygen = 3f;

        public MissionResultSummary LastResult { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public MissionResultSummary CalculateResult(
            bool victory,
            int firesExtinguished,
            int totalFires,
            int peopleRescued,
            int totalPeople,
            float timeTaken,
            float timeRemaining,
            float healthRemaining,
            float oxygenRemaining)
        {
            int fireScore = firesExtinguished * pointsPerFire;
            int rescueScore = peopleRescued * pointsPerRescue;
            int timeScore = victory ? Mathf.RoundToInt(Mathf.Max(0f, timeRemaining) * pointsPerSecondRemaining) : 0;
            int healthScore = victory ? Mathf.RoundToInt(Mathf.Max(0f, healthRemaining) * pointsPerHealth) : 0;
            int oxygenScore = victory ? Mathf.RoundToInt(Mathf.Max(0f, oxygenRemaining) * pointsPerOxygen) : 0;

            int total = fireScore + rescueScore + timeScore + healthScore + oxygenScore;

            string grade = "D";
            if (!victory)
            {
                grade = "F";
            }
            else if (total >= 3200)
            {
                grade = "S";
            }
            else if (total >= 2500)
            {
                grade = "A";
            }
            else if (total >= 1800)
            {
                grade = "B";
            }
            else if (total >= 1100)
            {
                grade = "C";
            }

            LastResult = new MissionResultSummary
            {
                victory = victory,
                firesExtinguished = firesExtinguished,
                totalFires = totalFires,
                peopleRescued = peopleRescued,
                totalPeople = totalPeople,
                timeTaken = timeTaken,
                timeRemaining = timeRemaining,
                healthRemaining = healthRemaining,
                oxygenRemaining = oxygenRemaining,
                totalScore = total,
                rating = grade
            };

            return LastResult;
        }
    }
}
