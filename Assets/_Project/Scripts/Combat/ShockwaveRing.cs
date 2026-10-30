using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// Titan stomp: a glowing ring that expands along the ground. Jump over it; touching it while grounded
    /// deals damage once.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ShockwaveRing : MonoBehaviour
    {
        const int Points = 64;

        [SerializeField] float maxRadius = 18f;
        [SerializeField] float duration = 2.5f;
        [SerializeField] float damage = 20f;
        [SerializeField] float thickness = 0.7f;
        [SerializeField] float jumpClearance = 0.6f;

        LineRenderer line;
        Transform player;
        float age;
        bool hit;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Points;
        }

        void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }

        void Update()
        {
            age += Time.deltaTime;
            float radius = Mathf.Lerp(0.5f, maxRadius, age / duration);
            for (int i = 0; i < Points; i++)
            {
                float a = i / (float)Points * Mathf.PI * 2f;
                line.SetPosition(i, transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius + Vector3.up * 0.15f);
            }

            if (!hit && player != null)
            {
                Vector3 offset = player.position - transform.position;
                float flat = new Vector2(offset.x, offset.z).magnitude;
                // Feet below the clearance height = not jumping over it
                if (Mathf.Abs(flat - radius) <= thickness && offset.y < jumpClearance)
                {
                    hit = true;
                    player.GetComponent<PlayerHealth>().TakeDamage(damage, DamageType.Hazard, transform.position);
                }
            }

            if (age >= duration) Destroy(gameObject);
        }
    }
}
