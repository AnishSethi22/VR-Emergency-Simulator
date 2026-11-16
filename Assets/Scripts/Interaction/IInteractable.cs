using UnityEngine;

namespace EmergencyRescue.Interaction
{
    /// <summary>
    /// Common interface for all interactive objects in both Desktop and VR modes
    /// (doors, NPCs, refill stations, switches, grabbable items).
    /// </summary>
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
