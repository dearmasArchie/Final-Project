using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Jagged, flickering electric line between two points (Overload Beam, Titan pylon tethers).
    /// Either follow two transforms, or call Show(start, end) for a one-off beam.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningLine : MonoBehaviour
    {
        [SerializeField] Transform from;
        [SerializeField] Transform to;
        [SerializeField] int segments = 12;
        [SerializeField] float jitter = 0.18f;
        [SerializeField] float refreshInterval = 0.04f;

        LineRenderer line;
        Vector3 start, end;
        float nextRefresh;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
        }

        /// <summary>Follow two transforms every frame (tethers).</summary>
        public void Connect(Transform a, Transform b)
        {
            from = a;
            to = b;
            line.enabled = true;
        }

        /// <summary>Draw between two fixed points (a beam shot) until hidden.</summary>
        public void Show(Vector3 a, Vector3 b)
        {
            from = to = null;
            start = a;
            end = b;
            line.enabled = true;
            Rebuild();
        }

        public void Hide() => line.enabled = false;

        void Update()
        {
            if (!line.enabled) return;
            if (from != null && to != null)
            {
                start = from.position;
                end = to.position;
            }
            if (Time.time >= nextRefresh) Rebuild();
        }

        void Rebuild()
        {
            nextRefresh = Time.time + refreshInterval;
            Vector3 direction = end - start;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            Vector3 up = Vector3.Cross(side, direction).normalized;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = start + direction * t;
                // No jitter at the ends, most in the middle
                float strength = jitter * Mathf.Sin(t * Mathf.PI);
                point += side * Random.Range(-strength, strength) + up * Random.Range(-strength, strength);
                line.SetPosition(i, point);
            }
        }
    }
}
