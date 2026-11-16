using UnityEngine;
using EmergencyRescue.Player;

namespace EmergencyRescue.Missions
{
    /// <summary>
    /// Trigger volume representing either a smoke-filled hazard zone (accelerates oxygen depletion)
    /// or a fresh-air safe zone (regenerates oxygen).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SmokeZone : MonoBehaviour
    {
        [SerializeField] private bool isFreshAirZone = false;

        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
            if (oxygen == null) return;

            if (isFreshAirZone)
                oxygen.EnterFreshAirZone();
            else
                oxygen.EnterSmokeZone();
        }

        private void OnTriggerExit(Collider other)
        {
            OxygenSystem oxygen = other.GetComponentInParent<OxygenSystem>();
            if (oxygen == null) return;

            if (isFreshAirZone)
                oxygen.ExitFreshAirZone();
            else
                oxygen.ExitSmokeZone();
        }
    }
}
