using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Interaction
{
    /// <summary>
    /// Reusable physics-based grabbable object supporting both Desktop pickup/drop
    /// and VR XRGrabInteractable grabbing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableObject : MonoBehaviour, IInteractable
    {
        [Header("Grabbable Settings")]
        [SerializeField] private string objectDisplayName = "Object";
        [SerializeField] private Vector3 desktopHoldOffset = Vector3.zero;
        [SerializeField] private Vector3 desktopHoldEuler = Vector3.zero;

        protected Rigidbody rb;
        protected Collider[] colliders;
        protected XRGrabInteractable xrGrab;
        protected bool isHeldDesktop;
        protected bool isHeldVR;
        protected Transform desktopHoldAnchor;

        public bool IsHeld => isHeldDesktop || isHeldVR;
        public string ObjectDisplayName => objectDisplayName;
        public virtual string InteractionPrompt => IsHeld ? string.Empty : $"[E] Pick Up {objectDisplayName}";

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
            xrGrab = GetComponent<XRGrabInteractable>();
            if (xrGrab == null)
            {
                xrGrab = gameObject.AddComponent<XRGrabInteractable>();
            }
            xrGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            xrGrab.throwOnDetach = true;
        }

        protected virtual void OnEnable()
        {
            if (xrGrab != null)
            {
                xrGrab.selectEntered.AddListener(OnVRGrabbed);
                xrGrab.selectExited.AddListener(OnVRReleased);
            }
        }

        protected virtual void OnDisable()
        {
            if (xrGrab != null)
            {
                xrGrab.selectEntered.RemoveListener(OnVRGrabbed);
                xrGrab.selectExited.RemoveListener(OnVRReleased);
            }
        }

        public void ConfigureHoldOffset(Vector3 offset, Vector3 euler)
        {
            desktopHoldOffset = offset;
            desktopHoldEuler = euler;
        }

        public virtual bool CanInteract(GameObject interactor) => !IsHeld;

        public virtual void Interact(GameObject interactor)
        {
            // Desktop pickup is handled by PlayerController via PickUpDesktop
        }

        public virtual void PickUpDesktop(Transform holdAnchor)
        {
            if (IsHeld) return;
            isHeldDesktop = true;
            desktopHoldAnchor = holdAnchor;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            SetCollidersEnabled(false);

            transform.SetParent(holdAnchor, false);
            transform.localPosition = desktopHoldOffset;
            transform.localRotation = Quaternion.Euler(desktopHoldEuler);
            AudioManager.Instance?.PlayPickupSound(transform.position);
        }

        public virtual void DropDesktop(Vector3 releaseVelocity)
        {
            if (!isHeldDesktop) return;
            isHeldDesktop = false;
            desktopHoldAnchor = null;

            transform.SetParent(null, true);
            // Keep dropped object upright on Y axis
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            SetCollidersEnabled(true);

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = releaseVelocity;
            }
            AudioManager.Instance?.PlayPickupSound(transform.position);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = enabled;
            }
        }

        protected virtual void OnVRGrabbed(SelectEnterEventArgs args)
        {
            isHeldVR = true;
            AudioManager.Instance?.PlayPickupSound(transform.position);
        }

        protected virtual void OnVRReleased(SelectExitEventArgs args)
        {
            isHeldVR = false;
            AudioManager.Instance?.PlayPickupSound(transform.position);
        }
    }
}
