using System;
using UnityEngine;

namespace EmergencyRescue.Interaction
{
    /// <summary>
    /// Detects nearby/aimed IInteractable objects for Desktop mode and broadcasts
    /// the current interaction prompt to the HUD.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 3.2f;
        [SerializeField] private LayerMask interactLayerMask = ~0;

        private IInteractable currentTarget;
        private GameObject currentTargetObject;

        public IInteractable CurrentTarget => currentTarget;
        public GameObject CurrentTargetObject => currentTargetObject;

        public event Action<string> OnPromptChanged;

        private string lastPrompt = string.Empty;

        public void SetCamera(Camera cam)
        {
            playerCamera = cam;
        }

        private void Update()
        {
            if (playerCamera == null) return;

            IInteractable found = null;
            GameObject foundObj = null;
            string prompt = string.Empty;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
                if (found != null)
                {
                    foundObj = ((MonoBehaviour)found).gameObject;
                    prompt = found.InteractionPrompt;
                }
            }

            currentTarget = found;
            currentTargetObject = foundObj;

            if (prompt != lastPrompt)
            {
                lastPrompt = prompt;
                OnPromptChanged?.Invoke(prompt);
            }
        }
    }
}
