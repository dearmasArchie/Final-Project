using UnityEngine;

namespace SignalLost
{
    /// <summary>Footsteps by distance walked: gravel on the island's terrain, stone on concrete and metal.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class FootstepPlayer : MonoBehaviour
    {
        [SerializeField] float strideWalk = 2.0f;
        [SerializeField] float strideSprint = 2.6f;

        CharacterController controller;
        FirstPersonController fpc;
        Vector3 lastPosition;
        float travelled;
        bool wasGrounded = true;
        Renderer[] puddles;
        ElectrifiedWater[] floods;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            fpc = GetComponent<FirstPersonController>();
        }

        void Start()
        {
            var root = GameObject.Find("ArtPass_Puddles");
            puddles = root != null ? root.GetComponentsInChildren<Renderer>() : new Renderer[0];
            floods = FindObjectsByType<ElectrifiedWater>(FindObjectsSortMode.None);
        }

        void OnEnable() => lastPosition = transform.position;

        void Update()
        {
            if (Time.timeScale == 0f) return;

            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            bool grounded = controller.isGrounded;

            if (grounded && !wasGrounded) Step(1f); // landing
            wasGrounded = grounded;
            if (!grounded) return;

            delta.y = 0f;
            if (delta.magnitude > 2f) return; // teleported (spawn point, elevator)
            travelled += delta.magnitude;
            bool sprinting = fpc != null && fpc.IsSprinting;
            if (travelled >= (sprinting ? strideSprint : strideWalk))
            {
                travelled = 0f;
                Step(sprinting ? 1f : 0.7f);
            }
        }

        void Step(float volumeScale)
        {
            Sfx.PlayScaled(SurfaceSound(), transform.position, volumeScale);
        }

        string SurfaceSound()
        {
            Vector3 p = transform.position;
            if (InPuddle(p)) return "step_puddle";

            if (!Physics.Raycast(p + Vector3.up * 0.3f, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore)
                || !(hit.collider is TerrainCollider)) return "step_stone"; // concrete and metal

            var terrain = hit.collider.GetComponent<Terrain>();
            if (terrain == null) return "step_dirt";
            string layer = DominantLayer(terrain, hit.point);
            if (layer.Contains("grass")) return "step_grass";
            if (layer.Contains("sand")) return "step_sand";
            if (layer.Contains("rock")) return "step_rock";
            return "step_dirt"; // dirt roads and paths
        }

        /// <summary>Inside a rain puddle (an oval inside the puddle mesh's bounds) or standing in flood water.</summary>
        bool InPuddle(Vector3 p)
        {
            foreach (var r in puddles)
            {
                if (r == null) continue;
                var b = r.bounds;
                float dx = (p.x - b.center.x) / (b.extents.x * 0.85f);
                float dz = (p.z - b.center.z) / (b.extents.z * 0.85f);
                if (dx * dx + dz * dz < 1f && Mathf.Abs(p.y - b.center.y) < 1f) return true;
            }
            foreach (var f in floods)
                if (f != null && !f.Drained && f.GetComponent<Collider>().bounds.Contains(p + Vector3.up * 0.3f)) return true;
            return false;
        }

        static string DominantLayer(Terrain terrain, Vector3 world)
        {
            var data = terrain.terrainData;
            Vector3 local = world - terrain.transform.position;
            int x = Mathf.Clamp((int)(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp((int)(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
            var weights = data.GetAlphamaps(x, z, 1, 1);
            int best = 0;
            for (int i = 1; i < weights.GetLength(2); i++)
                if (weights[0, 0, i] > weights[0, 0, best]) best = i;
            var layers = data.terrainLayers;
            return best < layers.Length && layers[best] != null ? layers[best].name.ToLowerInvariant() : "dirt";
        }
    }
}
