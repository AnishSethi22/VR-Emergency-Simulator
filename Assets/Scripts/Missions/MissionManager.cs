using System;
using System.Collections.Generic;
using UnityEngine;
using EmergencyRescue.Core;
using EmergencyRescue.Fire;
using EmergencyRescue.NPC;
using EmergencyRescue.Player;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Missions
{
    public enum MissionPhase
    {
        Mission1_FireControl = 1,
        Mission2_HumanRescue = 2,
        Mission3_FinalEscape = 3,
        Completed = 4
    }

    /// <summary>
    /// Tracks progressive 3-stage rescue objectives, countdown timer,
    /// victory/game-over conditions, and feeds ScoreManager.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [Header("Timer")]
        [SerializeField] private float missionTimeLimit = 300f; // 5 minutes

        [Header("Scene References (Auto-discovered if left empty)")]
        [SerializeField] private List<FireController> trackedFires = new List<FireController>();
        [SerializeField] private List<NPCRescueController> trackedNPCs = new List<NPCRescueController>();

        private float timeRemaining;
        private float elapsedTime;
        private MissionPhase currentPhase = MissionPhase.Mission1_FireControl;

        // Mission 1 sub-steps
        private bool enteredBuilding;
        private bool pickedUpExtinguisher;
        private bool extinguishedFirstFire;
        private bool reachedMission1Checkpoint;

        // Counters
        private int firesExtinguishedCount;
        private int totalObjectiveFires;
        private int peopleRescuedCount;
        private int peopleEvacuatedCount;
        private int totalPeopleCount;

        private bool missionEnded;
        private HealthSystem activePlayerHealth;
        private OxygenSystem activePlayerOxygen;

        public MissionPhase CurrentPhase => currentPhase;
        public float TimeRemaining => timeRemaining;
        public float ElapsedTime => elapsedTime;
        public int FiresExtinguishedCount => firesExtinguishedCount;
        public int TotalObjectiveFires => totalObjectiveFires;
        public int PeopleRescuedCount => peopleRescuedCount;
        public int PeopleEvacuatedCount => peopleEvacuatedCount;
        public int TotalPeopleCount => totalPeopleCount;

        public event Action OnMissionStateUpdated;
        public event Action<MissionPhase, string> OnMissionPhaseChanged;
        public event Action<MissionResultSummary> OnMissionEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            timeRemaining = missionTimeLimit;
            elapsedTime = 0f;
            missionEnded = false;

            DiscoverSceneEntities();
            SubscribeEvents();
            OnMissionStateUpdated?.Invoke();
        }

        private void Update()
        {
            if (missionEnded) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

            elapsedTime += Time.deltaTime;
            timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);

            if (timeRemaining <= 0f)
            {
                EndMission(false);
            }
        }

        public void RegisterActivePlayer(HealthSystem health, OxygenSystem oxygen)
        {
            if (activePlayerHealth != null)
            {
                activePlayerHealth.OnDied -= HandlePlayerDied;
            }
            activePlayerHealth = health;
            activePlayerOxygen = oxygen;
            if (activePlayerHealth != null)
            {
                activePlayerHealth.OnDied += HandlePlayerDied;
            }
        }

        private void DiscoverSceneEntities()
        {
            if (trackedFires.Count == 0)
            {
                var fires = FindObjectsByType<FireController>(FindObjectsSortMode.None);
                foreach (var f in fires)
                {
                    if (f.CountsTowardObjective) trackedFires.Add(f);
                }
            }
            totalObjectiveFires = Mathf.Max(1, trackedFires.Count);

            if (trackedNPCs.Count == 0)
            {
                trackedNPCs.AddRange(FindObjectsByType<NPCRescueController>(FindObjectsSortMode.None));
            }
            totalPeopleCount = Mathf.Max(1, trackedNPCs.Count);
        }

        private void SubscribeEvents()
        {
            foreach (var fire in trackedFires)
            {
                if (fire != null)
                {
                    fire.OnFireExtinguished += HandleFireExtinguished;
                }
            }

            foreach (var npc in trackedNPCs)
            {
                if (npc != null)
                {
                    npc.OnNPCRescued += HandleNPCRescued;
                    npc.OnNPCEvacuated += HandleNPCEvacuated;
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var fire in trackedFires)
            {
                if (fire != null) fire.OnFireExtinguished -= HandleFireExtinguished;
            }
            foreach (var npc in trackedNPCs)
            {
                if (npc != null)
                {
                    npc.OnNPCRescued -= HandleNPCRescued;
                    npc.OnNPCEvacuated -= HandleNPCEvacuated;
                }
            }
            if (activePlayerHealth != null)
            {
                activePlayerHealth.OnDied -= HandlePlayerDied;
            }
        }

        public void NotifyExtinguisherPickedUp()
        {
            if (!pickedUpExtinguisher)
            {
                pickedUpExtinguisher = true;
                EvaluateProgression();
            }
        }

        public void NotifyCheckpointReached(CheckpointType type)
        {
            if (missionEnded) return;

            switch (type)
            {
                case CheckpointType.BuildingEntrance:
                    if (!enteredBuilding)
                    {
                        enteredBuilding = true;
                        EvaluateProgression();
                    }
                    break;

                case CheckpointType.Mission1Checkpoint:
                    if (extinguishedFirstFire && !reachedMission1Checkpoint)
                    {
                        reachedMission1Checkpoint = true;
                        EvaluateProgression();
                    }
                    break;

                case CheckpointType.EmergencyExit:
                    if (currentPhase == MissionPhase.Mission3_FinalEscape)
                    {
                        EndMission(true);
                    }
                    break;
            }
        }

        private void HandleFireExtinguished(FireController fire)
        {
            firesExtinguishedCount++;
            if (!extinguishedFirstFire)
            {
                extinguishedFirstFire = true;
            }
            EvaluateProgression();
        }

        private void HandleNPCRescued(NPCRescueController npc)
        {
            peopleRescuedCount++;
            EvaluateProgression();
        }

        private void HandleNPCEvacuated(NPCRescueController npc)
        {
            peopleEvacuatedCount++;
            EvaluateProgression();
        }

        private void HandlePlayerDied()
        {
            EndMission(false);
        }

        private void EvaluateProgression()
        {
            if (missionEnded) return;

            if (currentPhase == MissionPhase.Mission1_FireControl)
            {
                if (enteredBuilding && pickedUpExtinguisher && extinguishedFirstFire && reachedMission1Checkpoint)
                {
                    currentPhase = MissionPhase.Mission2_HumanRescue;
                    AudioManager.Instance?.PlayRescueChime();
                    OnMissionPhaseChanged?.Invoke(currentPhase, "MISSION 1 PASSED! NEW MISSION: RESCUE 2 TRAPPED CIVILIANS!");
                }
            }

            if (currentPhase == MissionPhase.Mission2_HumanRescue)
            {
                if (peopleRescuedCount >= totalPeopleCount && peopleEvacuatedCount >= totalPeopleCount)
                {
                    currentPhase = MissionPhase.Mission3_FinalEscape;
                    AudioManager.Instance?.PlayRescueChime();
                    OnMissionPhaseChanged?.Invoke(currentPhase, "CIVILIANS SAFE! NEW MISSION: ESCAPE TO EMERGENCY EXIT!");
                }
            }

            OnMissionStateUpdated?.Invoke();
        }

        public string GetCurrentMissionTitle()
        {
            switch (currentPhase)
            {
                case MissionPhase.Mission1_FireControl: return "MISSION 1: FIRE CONTROL";
                case MissionPhase.Mission2_HumanRescue: return "MISSION 2: HUMAN RESCUE";
                case MissionPhase.Mission3_FinalEscape: return "MISSION 3: FINAL ESCAPE";
                default: return "MISSION COMPLETE";
            }
        }

        public string GetCurrentObjectiveText()
        {
            switch (currentPhase)
            {
                case MissionPhase.Mission1_FireControl:
                    return $"{(enteredBuilding ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 1. Enter burning building (Andar jayein)\n" +
                           $"{(pickedUpExtinguisher ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 2. Pick up Extinguisher (Press E)\n" +
                           $"{(extinguishedFirstFire ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 3. Spray & Extinguish Lobby Fire (Click & Hold)\n" +
                           $"{(reachedMission1Checkpoint ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 4. Reach Office Hallway checkpoint";

                case MissionPhase.Mission2_HumanRescue:
                    return $"{(peopleRescuedCount >= totalPeopleCount ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 1. Find & Rescue 2 Trapped Civilians ({peopleRescuedCount}/{totalPeopleCount})\n" +
                           $"   (Office A me Alex aur Office B me Maya ke paas jaakar E dabayein)\n" +
                           $"{(peopleEvacuatedCount >= totalPeopleCount ? "<color=#55FF77>[DONE]</color>" : "<color=#FFCC00>[TODO]</color>")} 2. Ensure they reach Outside Safe Zone ({peopleEvacuatedCount}/{totalPeopleCount})";

                case MissionPhase.Mission3_FinalEscape:
                    return "<color=#FF4433>[FINAL ESCAPE]</color>\n" +
                           "- Run through the smoke corridor!\n" +
                           "- Watch Oxygen! Move crates or avoid fire!\n" +
                           "- Reach the GREEN EMERGENCY EXIT sign to WIN!";

                default:
                    return "<color=#55FF77>ALL MISSIONS COMPLETED! YOU WON!</color>";
            }
        }

        private void EndMission(bool victory)
        {
            if (missionEnded) return;
            missionEnded = true;
            if (victory)
            {
                currentPhase = MissionPhase.Completed;
                AudioManager.Instance?.PlayMissionComplete();
            }

            float hp = activePlayerHealth != null ? activePlayerHealth.CurrentHealth : 0f;
            float ox = activePlayerOxygen != null ? activePlayerOxygen.CurrentOxygen : 0f;

            MissionResultSummary summary = ScoreManager.Instance != null
                ? ScoreManager.Instance.CalculateResult(
                    victory,
                    firesExtinguishedCount,
                    totalObjectiveFires,
                    peopleRescuedCount,
                    totalPeopleCount,
                    elapsedTime,
                    timeRemaining,
                    hp,
                    ox)
                : new MissionResultSummary { victory = victory };

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(victory ? GameState.Victory : GameState.GameOver);
            }

            OnMissionEnded?.Invoke(summary);
        }
    }
}
