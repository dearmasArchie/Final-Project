using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        const string StingerPrefabPath = "Assets/_Project/Prefabs/Enemies/ReconStinger.prefab";

        // Corridor interior (Wing A): x from -8 (hub) to -30 (breaker room door), z from 18.15 (south) to 21.85 (north)
        const float CorridorSouth = 18.15f, CorridorNorth = 21.85f;

        /// <summary>One laser gate: emitter on a wall, a free-standing receiver post, and the fan of beams between them.</summary>
        struct LaserGate
        {
            public string name;
            public bool emitterSouth;                 // emitter wall (the post is toward the other side, so the gap is on the far wall)
            public Vector2 emitterXZ;                 // wall position x (z follows from the wall)
            public Vector2 postXZ;                    // receiver post x, z
            public float[] startY, endY;              // beam heights at the emitter and at the post (differ for slanted beams)
        }

        /// <summary>
        /// Fixed layout (no randomness): six gates the player must weave through, gap on alternating sides, mixing straight
        /// beams, beams slanted across the corridor, beams tilted in height, and a gate that does both. Shooting an
        /// emitter switches off its beams.
        /// </summary>
        static LaserGate[] LaserGates()
        {
            float[] levels = { 0.3f, 0.75f, 1.2f, 1.65f, 2.1f };
            float[] Rising(float amount) { var r = new float[levels.Length]; for (int i = 0; i < r.Length; i++) r[i] = Mathf.Clamp(levels[i] + (i % 2 == 0 ? amount : -amount), 0.2f, 2.3f); return r; }
            float[] Fan(float spread) { var r = new float[levels.Length]; for (int i = 0; i < r.Length; i++) r[i] = Mathf.Lerp(levels[i], 1.25f, spread); return r; }
            return new[]
            {
                new LaserGate { name = "A", emitterSouth = true,  emitterXZ = new Vector2(-12.5f, CorridorSouth), postXZ = new Vector2(-12.5f, 20.3f), startY = levels, endY = levels },
                new LaserGate { name = "B", emitterSouth = false, emitterXZ = new Vector2(-14.2f, CorridorNorth), postXZ = new Vector2(-14.2f, 19.55f), startY = levels, endY = Fan(0.5f) },
                new LaserGate { name = "C", emitterSouth = true,  emitterXZ = new Vector2(-17.0f, CorridorSouth), postXZ = new Vector2(-19.6f, 20.2f), startY = levels, endY = levels },
                new LaserGate { name = "D", emitterSouth = false, emitterXZ = new Vector2(-20.8f, CorridorNorth), postXZ = new Vector2(-20.8f, 19.7f), startY = levels, endY = Rising(0.7f) },
                new LaserGate { name = "E", emitterSouth = true,  emitterXZ = new Vector2(-23.6f, CorridorSouth), postXZ = new Vector2(-25.8f, 20.5f), startY = levels, endY = Rising(0.5f) },
                new LaserGate { name = "F", emitterSouth = false, emitterXZ = new Vector2(-27.6f, CorridorNorth), postXZ = new Vector2(-27.6f, 19.6f), startY = Fan(0.4f), endY = levels },
            };
        }

        /// <summary>Rebuilds the Wing A laser corridor in the open UndergroundFacility scene. Bridge command: lasers.</summary>
        public static void RebuildLasers()
        {
            var scene = EditorSceneManager.OpenScene(BunkerScene);
            var alarm = Object.FindAnyObjectByType<AlarmSpawner>();
            if (alarm == null) { EditorCommandBridge.Log("lasers: AlarmSpawner (Wing A) not found"); return; }
            var wing = alarm.transform.parent.gameObject;
            var stinger = AssetDatabase.LoadAssetAtPath<GameObject>(StingerPrefabPath);
            string report = BuildLaserCorridor(wing.transform, alarm, stinger);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorCommandBridge.Log("lasers: " + report);
        }

        /// <summary>Builds the laser gates under wing (replacing any earlier emitters or gates) and wires them to the alarm.</summary>
        internal static string BuildLaserCorridor(Transform wing, AlarmSpawner alarm, GameObject alarmEnemy)
        {
            EnsureFolder();
            var doomed = new List<GameObject>();
            foreach (Transform child in wing)
                if (child.name.StartsWith("LaserEmitter_") || child.name == "LaserGates") doomed.Add(child.gameObject);
            foreach (var go in doomed) Object.DestroyImmediate(go);

            var steel = Steel("Laser_Housing", new Color(0.32f, 0.34f, 0.37f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var yellow = Lit("Lift_Yellow", new Color(0.88f, 0.66f, 0.06f), 0.3f, 0.45f);
            var lens = Lit("Laser_Lens", new Color(0.3f, 0.02f, 0.02f), 0f, 0.9f, new Color(5f, 0.2f, 0.12f));
            var core = Lit("Laser_Core", new Color(1f, 0.25f, 0.2f), 0f, 0.5f, new Color(9f, 0.35f, 0.2f));
            var halo = Lit("Laser_HaloSolid", new Color(1f, 0.1f, 0.05f, 0.12f), 0f, 0f, new Color(1.2f, 0.06f, 0.03f));
            halo.SetFloat("_Surface", 1f);
            halo.SetFloat("_Blend", 0f);
            halo.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            halo.SetFloat("_DstBlend", (float)BlendMode.One);
            halo.SetFloat("_ZWrite", 0f);
            halo.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            halo.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(halo);
            var hazard = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Greybox_Hazard.mat") ?? yellow;

            var root = Group(wing, "LaserGates");
            int beamCount = 0;
            foreach (var gate in LaserGates())
            {
                var g = Group(root, $"Gate_{gate.name}");
                float wallZ = gate.emitterXZ.y;
                float side = gate.emitterSouth ? 1f : -1f;                         // direction from the wall into the corridor
                float ex = gate.emitterXZ.x;
                var postPos = new Vector3(gate.postXZ.x, 0f, gate.postXZ.y);

                // Emitter column on the wall: steel housing with a lens at each beam height; it takes the shot
                var emitter = Part(g, $"LaserEmitter_{gate.name}", PrimitiveType.Cube, new Vector3(ex, 1.2f, wallZ + side * 0.12f), new Vector3(0.4f, 2.5f, 0.24f), steel, collider: true);
                var housingRenderer = emitter.GetComponent<Renderer>();
                var glows = new List<Renderer>();
                Part(g, "Cap", PrimitiveType.Cube, new Vector3(ex, 2.5f, wallZ + side * 0.12f), new Vector3(0.46f, 0.08f, 0.3f), dark);
                Part(g, "Base", PrimitiveType.Cube, new Vector3(ex, 0.08f, wallZ + side * 0.14f), new Vector3(0.5f, 0.16f, 0.3f), hazard);
                var beams = new List<GameObject>();
                for (int i = 0; i < gate.startY.Length; i++)
                {
                    Vector3 a = new Vector3(ex, gate.startY[i], wallZ + side * 0.26f);
                    Vector3 b = new Vector3(postPos.x, gate.endY[i], postPos.z - side * 0.14f);
                    var port = Part(g, "Lens", PrimitiveType.Cylinder, new Vector3(ex, gate.startY[i], wallZ + side * 0.25f), new Vector3(0.09f, 0.02f, 0.09f), lens, Quaternion.Euler(90, 0, 0), shadows: false);
                    glows.Add(port.GetComponent<Renderer>());
                    var rport = Part(g, "ReceiverPort", PrimitiveType.Cylinder, new Vector3(postPos.x, gate.endY[i], postPos.z - side * 0.15f), new Vector3(0.08f, 0.02f, 0.08f), lens, Quaternion.Euler(90, 0, 0), shadows: false);
                    glows.Add(rport.GetComponent<Renderer>());
                    beams.Add(LaserBeamObject(g, $"Beam_{gate.name}{i}", a, b, core, halo));
                    beamCount++;
                }

                // Receiver post in the corridor: floor to ceiling, solid
                var post = Part(g, $"ReceiverPost_{gate.name}", PrimitiveType.Cube, new Vector3(postPos.x, 1.95f, postPos.z), new Vector3(0.3f, 3.9f, 0.3f), steel, collider: true);
                Part(g, "PostBase", PrimitiveType.Cube, new Vector3(postPos.x, 0.08f, postPos.z), new Vector3(0.42f, 0.16f, 0.42f), hazard);
                Part(g, "PostCap", PrimitiveType.Cube, new Vector3(postPos.x, 2.55f, postPos.z), new Vector3(0.36f, 0.06f, 0.36f), dark);
                // Floor hazard strip on the blocked side, so the safe gap reads
                Vector3 stripA = new Vector3(ex, 0.012f, wallZ + side * 0.5f), stripB = new Vector3(postPos.x, 0.012f, postPos.z - side * 0.3f);
                Bar(g, "FloorMark", stripA, stripB, 0.05f, hazard).transform.localScale = new Vector3(0.06f, 0.012f, Vector3.Distance(stripA, stripB));

                // A soft red glow that breathes a little
                var light = PointLight(g, "Glow", new Vector3((ex + postPos.x) / 2f, 1.3f, (wallZ + postPos.z) / 2f), new Color(1f, 0.1f, 0.05f), 1.4f, 4.5f);
                var flicker = light.gameObject.AddComponent<LightFlicker>();
                var fso = new SerializedObject(flicker);
                fso.FindProperty("mode").enumValueIndex = (int)LightFlicker.Mode.Pulse;
                fso.FindProperty("speed").floatValue = 1.2f;
                fso.FindProperty("minimum").floatValue = 0.6f;
                fso.ApplyModifiedPropertiesWithoutUndo();

                var trip = emitter.AddComponent<LaserTripwire>();
                var so = new SerializedObject(trip);
                SetArray(so.FindProperty("beams"), beams.ToArray());
                so.FindProperty("emitterLight").objectReferenceValue = glows[0];
                SetArray(so.FindProperty("glowRenderers"), glows.ToArray());
                so.FindProperty("alarm").objectReferenceValue = alarm;
                so.FindProperty("alarmEnemyPrefab").objectReferenceValue = alarmEnemy;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return $"{LaserGates().Length} gates, {beamCount} beams";
        }

        /// <summary>
        /// One beam: a root (kinematic Rigidbody and LaserBeam are added by LaserTripwire at runtime) holding a bright core
        /// cylinder, a soft additive halo, and a thicker trigger hitbox along the line from a to b.
        /// </summary>
        static GameObject LaserBeamObject(Transform parent, string name, Vector3 a, Vector3 b, Material core, Material halo)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Vector3 d = b - a;
            root.transform.position = (a + b) * 0.5f;
            root.transform.rotation = Quaternion.LookRotation(d.normalized, Mathf.Abs(Vector3.Dot(d.normalized, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up);
            float len = d.magnitude;

            void Cyl(string n, float radius, Material m)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = n;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(root.transform, false);
                c.transform.localRotation = Quaternion.Euler(90, 0, 0);
                c.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
                var r = c.GetComponent<Renderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            Cyl("Core", 0.012f, core);
            Cyl("Halo", 0.045f, halo);

            var hit = new GameObject("Hitbox");
            hit.transform.SetParent(root.transform, false);
            var box = hit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.12f, 0.12f, len);
            return root;
        }
    }
}
