using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using EmergencyRescue.Core;
using EmergencyRescue.Fire;

namespace EmergencyRescue.Player
{
    /// <summary>
    /// Configures and manages the XR Origin VR player rig, applying comfort settings
    /// (move speed, snap/smooth turn) and exposing held extinguisher state to the HUD.
    /// </summary>
    public class XRPlayerController : MonoBehaviour
    {
        [Header("Locomotion Providers")]
        [SerializeField] private ContinuousMoveProvider moveProvider;
        [SerializeField] private SnapTurnProvider snapTurnProvider;
        [SerializeField] private ContinuousTurnProvider continuousTurnProvider;

        public FireExtinguisher ActiveHeldExtinguisher
        {
            get
            {
                var extinguishers = FindObjectsByType<FireExtinguisher>(FindObjectsSortMode.None);
                foreach (var ext in extinguishers)
                {
                    if (ext.IsHeld) return ext;
                }
                return null;
            }
        }

        private void Start()
        {
            ApplyComfortSettings();
        }

        public void ApplyComfortSettings()
        {
            if (GameManager.Instance == null) return;

            if (moveProvider != null)
            {
                moveProvider.moveSpeed = GameManager.Instance.moveSpeed;
            }

            bool useSnap = GameManager.Instance.useSnapTurn;
            if (snapTurnProvider != null)
            {
                snapTurnProvider.enabled = useSnap;
                snapTurnProvider.turnAmount = GameManager.Instance.snapTurnAngle;
            }
            if (continuousTurnProvider != null)
            {
                continuousTurnProvider.enabled = !useSnap;
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                GameManager.Instance?.TogglePause();
            }
        }
    }
}
