using System;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using EmergencyRescue.Interaction;
using EmergencyRescue.Audio;

namespace EmergencyRescue.NPC
{
    public enum NPCState
    {
        TrappedInjured,
        Evacuating,
        EvacuatedSafe
    }

    /// <summary>
    /// Controls a trapped civilian NPC: injured idle animation, rescue interaction,
    /// NavMesh navigation to the safe evacuation zone, and safe arrival state.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCRescueController : MonoBehaviour, IInteractable
    {
        [Header("NPC Identity & Targets")]
        [SerializeField] private string npcName = "Civilian";
        [SerializeField] private Transform evacuationTarget;
        [SerializeField] private float safeArrivalDistance = 1.8f;

        [Header("Procedural Animation Parts")]
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private Transform rightArmPivot;
        [SerializeField] private Transform leftArmPivot;
        [SerializeField] private TextMeshPro statusLabel;

        private NavMeshAgent agent;
        private NPCState currentState = NPCState.TrappedInjured;
        private float animTimer;
        private Vector3 bodyBaseLocalPos;

        public string NPCName => npcName;
        public NPCState CurrentState => currentState;
        public bool IsRescued => currentState != NPCState.TrappedInjured;
        public bool HasReachedSafeZone => currentState == NPCState.EvacuatedSafe;

        public event Action<NPCRescueController> OnNPCRescued;
        public event Action<NPCRescueController> OnNPCEvacuated;

        public string InteractionPrompt =>
            currentState == NPCState.TrappedInjured ? $"[E] Rescue {npcName}" : string.Empty;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (bodyRoot != null) bodyBaseLocalPos = bodyRoot.localPosition;
            animTimer = UnityEngine.Random.Range(0f, 10f);
        }

        private void Start()
        {
            if (agent != null)
            {
                agent.isStopped = true;
            }
            UpdateStatusLabel();
        }

        private void Update()
        {
            animTimer += Time.deltaTime;
            UpdateProceduralAnimation();
            UpdateLabelFacing();

            if (currentState == NPCState.Evacuating && agent != null && evacuationTarget != null)
            {
                if (!agent.pathPending && agent.remainingDistance <= safeArrivalDistance)
                {
                    ReachSafeZone();
                }
            }
        }

        public void Configure(string displayName, Transform evacTarget, Transform body, Transform rArm, Transform lArm, TextMeshPro label)
        {
            npcName = displayName;
            evacuationTarget = evacTarget;
            bodyRoot = body;
            rightArmPivot = rArm;
            leftArmPivot = lArm;
            statusLabel = label;
            if (bodyRoot != null) bodyBaseLocalPos = bodyRoot.localPosition;
        }

        public bool CanInteract(GameObject interactor)
        {
            return currentState == NPCState.TrappedInjured;
        }

        public void Interact(GameObject interactor)
        {
            if (currentState != NPCState.TrappedInjured) return;
            Rescue();
        }

        public void Rescue()
        {
            if (currentState != NPCState.TrappedInjured) return;

            currentState = NPCState.Evacuating;
            AudioManager.Instance?.PlayRescueChime();
            UpdateStatusLabel();

            if (agent != null && agent.isOnNavMesh && evacuationTarget != null)
            {
                agent.isStopped = false;
                agent.speed = 3.2f;
                agent.SetDestination(evacuationTarget.position);
            }

            OnNPCRescued?.Invoke(this);
        }

        private void ReachSafeZone()
        {
            currentState = NPCState.EvacuatedSafe;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            UpdateStatusLabel();
            OnNPCEvacuated?.Invoke(this);
        }

        private void UpdateProceduralAnimation()
        {
            if (bodyRoot == null) return;

            if (currentState == NPCState.TrappedInjured)
            {
                // Injured crouch/coughing bob + waving right arm for help
                float cough = Mathf.Abs(Mathf.Sin(animTimer * 3.5f)) * 0.05f;
                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, -0.15f + cough, 0f);
                bodyRoot.localRotation = Quaternion.Euler(12f + Mathf.Sin(animTimer * 3.5f) * 4f, 0f, 0f);

                if (rightArmPivot != null)
                {
                    float wave = Mathf.Sin(animTimer * 6f) * 28f;
                    rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, 140f + wave);
                }
                if (leftArmPivot != null)
                {
                    leftArmPivot.localRotation = Quaternion.Euler(20f, 0f, -15f);
                }
            }
            else if (currentState == NPCState.Evacuating)
            {
                // Walking bob & arm swing
                float walkBob = Mathf.Abs(Mathf.Sin(animTimer * 9f)) * 0.06f;
                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, walkBob, 0f);
                bodyRoot.localRotation = Quaternion.Euler(6f, 0f, 0f);

                float swing = Mathf.Sin(animTimer * 9f) * 32f;
                if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(swing, 0f, -8f);
                if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(-swing, 0f, 8f);
            }
            else // EvacuatedSafe
            {
                float breath = Mathf.Sin(animTimer * 2f) * 0.02f;
                bodyRoot.localPosition = bodyBaseLocalPos + new Vector3(0f, breath, 0f);
                bodyRoot.localRotation = Quaternion.identity;
                if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(0f, 0f, -10f);
                if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(0f, 0f, 10f);
            }
        }

        private void UpdateStatusLabel()
        {
            if (statusLabel == null) return;
            switch (currentState)
            {
                case NPCState.TrappedInjured:
                    statusLabel.text = $"{npcName}\n[HELP! PRESS E]";
                    statusLabel.color = new Color(1f, 0.35f, 0.25f);
                    break;
                case NPCState.Evacuating:
                    statusLabel.text = $"{npcName}\nEVACUATING...";
                    statusLabel.color = new Color(1f, 0.85f, 0.2f);
                    break;
                case NPCState.EvacuatedSafe:
                    statusLabel.text = $"{npcName}\nSAFE";
                    statusLabel.color = new Color(0.25f, 0.95f, 0.45f);
                    break;
            }
        }

        private void UpdateLabelFacing()
        {
            if (statusLabel == null) return;
            Camera cam = Camera.main;
            if (cam != null)
            {
                statusLabel.transform.LookAt(
                    statusLabel.transform.position + cam.transform.rotation * Vector3.forward,
                    cam.transform.rotation * Vector3.up);
            }
        }
    }
}
