using UnityEngine;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Interaction
{
    /// <summary>
    /// Interactive hinged door supporting both Desktop (E key) and VR (ray/direct interact)
    /// opening and closing with smooth rotation and 3D spatial audio.
    /// </summary>
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private Transform doorPivot;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 4.5f;
        [SerializeField] private bool startsOpen = false;
        [SerializeField] private bool isLocked = false;
        [SerializeField] private string lockedMessage = "Door is jammed / locked";

        private bool isOpen;
        private Quaternion closedRotation;
        private Quaternion openRotation;

        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        public string InteractionPrompt
        {
            get
            {
                if (isLocked) return lockedMessage;
                return isOpen ? "[E] Close Door" : "[E] Open Door";
            }
        }

        private void Awake()
        {
            if (doorPivot == null) doorPivot = transform;
            closedRotation = doorPivot.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            isOpen = startsOpen;
            if (isOpen) doorPivot.localRotation = openRotation;
        }

        private void Update()
        {
            Quaternion target = isOpen ? openRotation : closedRotation;
            if (Quaternion.Angle(doorPivot.localRotation, target) > 0.1f)
            {
                doorPivot.localRotation = Quaternion.Slerp(doorPivot.localRotation, target, Time.deltaTime * openSpeed);
            }
        }

        public bool CanInteract(GameObject interactor) => !isLocked;

        public void Interact(GameObject interactor)
        {
            if (isLocked) return;
            SetOpen(!isOpen);
        }

        public void SetOpen(bool open)
        {
            if (isOpen == open) return;
            isOpen = open;
            AudioManager.Instance?.PlayDoorSound(doorPivot.position);
        }

        public void Unlock()
        {
            isLocked = false;
        }
    }
}
