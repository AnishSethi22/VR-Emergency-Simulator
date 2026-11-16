using UnityEngine;
using EmergencyRescue.Player;

namespace EmergencyRescue.Missions
{
    public enum CheckpointType
    {
        BuildingEntrance,
        Mission1Checkpoint,
        EmergencyExit
    }

    /// <summary>
    /// Trigger zone that notifies MissionManager when the player reaches a key location.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private CheckpointType checkpointType;

        public CheckpointType Type => checkpointType;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        public void SetType(CheckpointType type)
        {
            checkpointType = type;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<HealthSystem>() == null) return;
            MissionManager.Instance?.NotifyCheckpointReached(checkpointType);
        }
    }
}
