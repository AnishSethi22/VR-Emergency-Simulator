using UnityEngine;
using UnityEngine.InputSystem;
using EmergencyRescue.Core;
using EmergencyRescue.Interaction;
using EmergencyRescue.Fire;
using EmergencyRescue.Audio;

namespace EmergencyRescue.Player
{
    /// <summary>
    /// First-person Desktop player controller using Unity Input System.
    /// Supports WASD movement, mouse look, E-key interaction, G/Q drop, and LMB extinguisher spray.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform holdAnchor;
        [SerializeField] private InteractionManager interactionManager;

        [Header("Movement & Look")]
        [SerializeField] private float sprintMultiplier = 1.45f;
        [SerializeField] private float moveAcceleration = 18f;
        [SerializeField] private float lookSmoothSpeed = 28f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float footstepInterval = 0.45f;

        private CharacterController controller;
        private GrabbableObject heldObject;
        private FireExtinguisher heldExtinguisher;
        private float verticalVelocity;
        private Vector3 currentHorizontalVel;
        private float targetYaw;
        private float targetPitch;
        private float currentYaw;
        private float currentPitch;
        private int cursorLockGraceFrames;
        private float footstepTimer;

        public FireExtinguisher HeldExtinguisher => heldExtinguisher;
        public Camera PlayerCamera => playerCamera;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (interactionManager == null) interactionManager = GetComponent<InteractionManager>();
            if (interactionManager != null && playerCamera != null)
            {
                interactionManager.SetCamera(playerCamera);
            }
            targetYaw = transform.eulerAngles.y;
            currentYaw = targetYaw;
            cursorLockGraceFrames = 0;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ConfigureReferences(Camera cam, Transform anchor, InteractionManager interactMgr)
        {
            playerCamera = cam;
            holdAnchor = anchor;
            interactionManager = interactMgr;
            if (interactionManager != null && playerCamera != null)
            {
                interactionManager.SetCamera(playerCamera);
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null) return;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                GameManager.Instance?.TogglePause();
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                if (heldExtinguisher != null) heldExtinguisher.SetSpraying(false);
                return;
            }

            HandleLook(mouse);
            HandleMovement(kb);
            HandleInteraction(kb, mouse);
        }

        private void HandleLook(Mouse mouse)
        {
            if (mouse == null || playerCamera == null) return;

            // Auto-lock mouse if user clicks anywhere in game
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            Vector2 rawDelta = mouse.delta.ReadValue();
            // In Unity Input System, mouse delta can arrive even if cursor just became locked
            if (rawDelta.sqrMagnitude > 0.0001f)
            {
                float sens = GameManager.Instance != null ? GameManager.Instance.mouseSensitivity : 1.8f;
                rawDelta.x = Mathf.Clamp(rawDelta.x, -70f, 70f);
                rawDelta.y = Mathf.Clamp(rawDelta.y, -70f, 70f);

                float scale = sens * 0.12f;
                targetYaw += rawDelta.x * scale;
                targetPitch = Mathf.Clamp(targetPitch - rawDelta.y * scale, -80f, 80f);
            }

            float t = 1f - Mathf.Exp(-lookSmoothSpeed * Time.deltaTime);
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, t);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, t);

            transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }

        private void HandleMovement(Keyboard kb)
        {
            float moveX = 0f;
            float moveZ = 0f;
            if (kb.aKey.isPressed) moveX -= 1f;
            if (kb.dKey.isPressed) moveX += 1f;
            if (kb.wKey.isPressed) moveZ += 1f;
            if (kb.sKey.isPressed) moveZ -= 1f;

            Vector3 inputDir = (transform.right * moveX + transform.forward * moveZ);
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            float baseSpeed = GameManager.Instance != null ? GameManager.Instance.moveSpeed : 4.2f;
            bool sprinting = kb.leftShiftKey.isPressed;
            float speed = sprinting ? baseSpeed * sprintMultiplier : baseSpeed;

            Vector3 targetHoriz = inputDir * speed;
            currentHorizontalVel = Vector3.MoveTowards(currentHorizontalVel, targetHoriz, moveAcceleration * Time.deltaTime);

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = currentHorizontalVel + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            if (controller.isGrounded && inputDir.sqrMagnitude > 0.1f)
            {
                footstepTimer -= Time.deltaTime * (sprinting ? 1.35f : 1f);
                if (footstepTimer <= 0f)
                {
                    footstepTimer = footstepInterval;
                    AudioManager.Instance?.PlayFootstep(transform.position);
                }
            }
        }

        private void HandleInteraction(Keyboard kb, Mouse mouse)
        {
            if (kb.eKey.wasPressedThisFrame && interactionManager != null && interactionManager.CurrentTarget != null)
            {
                IInteractable target = interactionManager.CurrentTarget;
                if (target is GrabbableObject grabbable)
                {
                    if (heldObject != null) DropHeldObject(false);
                    heldObject = grabbable;
                    heldExtinguisher = grabbable as FireExtinguisher;
                    grabbable.PickUpDesktop(holdAnchor);
                }
                else if (target.CanInteract(gameObject))
                {
                    target.Interact(gameObject);
                }
            }

            if ((kb.gKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) && heldObject != null)
            {
                DropHeldObject(true);
            }

            if (heldExtinguisher != null && mouse != null)
            {
                heldExtinguisher.SetSpraying(mouse.leftButton.isPressed);
            }
        }

        private void DropHeldObject(bool forwardToss)
        {
            if (heldObject == null) return;
            Vector3 vel = forwardToss && playerCamera != null
                ? playerCamera.transform.forward * 2.5f + Vector3.up * 0.8f
                : Vector3.zero;
            heldObject.DropDesktop(vel);
            heldObject = null;
            heldExtinguisher = null;
        }
    }
}
