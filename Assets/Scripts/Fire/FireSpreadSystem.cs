using System.Collections.Generic;
using UnityEngine;
using EmergencyRescue.Core;

namespace EmergencyRescue.Fire
{
    /// <summary>
    /// Periodically evaluates active FireController instances and spreads fire to nearby
    /// dormant fire nodes within SpreadRadius if unobstructed.
    /// </summary>
    public class FireSpreadSystem : MonoBehaviour
    {
        [Header("Spread Settings")]
        [SerializeField] private float checkInterval = 3.5f;
        [SerializeField] private float spreadChancePerCheck = 0.35f;
        [SerializeField] private float minIntensityToSpread = 45f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        private readonly List<FireController> allFires = new List<FireController>();
        private float nextCheckTime;

        private void Start()
        {
            RefreshFireList();
            nextCheckTime = Time.time + checkInterval;
        }

        public void RefreshFireList()
        {
            allFires.Clear();
            allFires.AddRange(FindObjectsByType<FireController>(FindObjectsSortMode.None));
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;
            if (Time.time < nextCheckTime) return;

            nextCheckTime = Time.time + checkInterval;
            EvaluateSpread();
        }

        private void EvaluateSpread()
        {
            for (int i = 0; i < allFires.Count; i++)
            {
                FireController source = allFires[i];
                if (source == null || source.IsExtinguished) continue;
                if (source.FireIntensity < minIntensityToSpread) continue;

                for (int j = 0; j < allFires.Count; j++)
                {
                    if (i == j) continue;
                    FireController target = allFires[j];
                    if (target == null || !target.IsExtinguished) continue;

                    float dist = Vector3.Distance(source.transform.position, target.transform.position);
                    if (dist <= source.SpreadRadius && UnityEngine.Random.value <= spreadChancePerCheck)
                    {
                        Vector3 origin = source.transform.position + Vector3.up * 0.4f;
                        Vector3 dest = target.transform.position + Vector3.up * 0.4f;
                        Vector3 dir = (dest - origin).normalized;

                        // Spread only if not blocked by a solid wall
                        if (!Physics.Raycast(origin, dir, dist - 0.3f, obstructionMask, QueryTriggerInteraction.Ignore))
                        {
                            target.Ignite(55f);
                        }
                    }
                }
            }
        }
    }
}
