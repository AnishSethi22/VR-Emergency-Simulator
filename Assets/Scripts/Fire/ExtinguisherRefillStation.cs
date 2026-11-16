using UnityEngine;
using EmergencyRescue.Interaction;

namespace EmergencyRescue.Fire
{
    /// <summary>
    /// Wall-mounted refill station that restores FireExtinguisher pressure to 100%.
    /// </summary>
    public class ExtinguisherRefillStation : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => "[E] Refill Fire Extinguisher";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            if (interactor == null) return;
            FireExtinguisher held = interactor.GetComponentInChildren<FireExtinguisher>();
            if (held != null)
            {
                held.Refill();
                return;
            }

            // Fallback: refill nearest extinguisher within 2.5m
            var all = FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None);
            foreach (var ext in all)
            {
                if (Vector3.Distance(transform.position, ext.transform.position) <= 2.5f)
                {
                    ext.Refill();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            FireExtinguisher ext = other.GetComponentInParent<FireExtinguisher>();
            if (ext != null && ext.CurrentPressure < ext.MaxPressure * 0.98f)
            {
                ext.Refill();
            }
        }
    }
}
