using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SignalLost.EditorTools
{
    public static partial class DetailArtBuilder
    {
        const string PaintFolder = MaterialFolder + "/Paint";

        struct PaintColor
        {
            public string name;
            public Color digit;   // the painted number
            public Color smear;   // the wet smudge behind and around it
            public PaintColor(string name, Color digit, Color smear) { this.name = name; this.digit = digit; this.smear = smear; }
        }

        // Code colors in the order GameManager.ArmoryPuzzleData.ColorNames uses
        static readonly PaintColor[] CodePaints =
        {
            new PaintColor("Blue", new Color(0.06f, 0.22f, 0.92f), new Color(0.25f, 0.45f, 1f, 0.7f)),
            new PaintColor("Red", new Color(0.82f, 0.06f, 0.05f), new Color(0.95f, 0.3f, 0.25f, 0.7f)),
            new PaintColor("Green", new Color(0.06f, 0.62f, 0.14f), new Color(0.3f, 0.85f, 0.35f, 0.7f)),
            new PaintColor("Yellow", new Color(0.98f, 0.82f, 0.05f), new Color(1f, 0.92f, 0.35f, 0.75f)),
        };

        // Decoy colors: none of them is a code color, so the wall message always picks the right numbers
        static readonly PaintColor[] DecoyPaints =
        {
            new PaintColor("White", new Color(0.97f, 0.97f, 0.93f), new Color(0.12f, 0.12f, 0.12f, 0.55f)),
            new PaintColor("Orange", new Color(0.98f, 0.45f, 0.05f), new Color(1f, 0.65f, 0.3f, 0.65f)),
            new PaintColor("Purple", new Color(0.45f, 0.12f, 0.7f), new Color(0.65f, 0.4f, 0.85f, 0.65f)),
            new PaintColor("Black", new Color(0.03f, 0.03f, 0.03f), new Color(0.75f, 0.75f, 0.72f, 0.6f)),
            new PaintColor("Brown", new Color(0.42f, 0.24f, 0.09f), new Color(0.65f, 0.45f, 0.25f, 0.65f)),
        };

        // ------------------------------------------------------------------ paint textures and materials

        /// <summary>
        /// A white paint smudge with alpha: a wobbly blob, dry-brush streaks across it, ragged edges and a few drips from
        /// the bottom. Tinted per material. Saved as a PNG once per variant.
        /// </summary>
        static Texture2D SmearTexture(int variant)
        {
            if (!AssetDatabase.IsValidFolder(PaintFolder)) AssetDatabase.CreateFolder(MaterialFolder, "Paint");
            string path = $"{PaintFolder}/Smear_{variant}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int N = 256;
            var rng = new System.Random(variant * 101 + 7);
            float seed = variant * 13.7f;
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N * 2f - 1f, v = y / (float)N * 2f - 1f;
                    float r = Mathf.Sqrt(u * u * 0.8f + v * v * 1.15f);          // slightly wide blob
                    float a = Mathf.Atan2(v, u);
                    float edge = 0.55f + 0.16f * (Mathf.PerlinNoise(seed + Mathf.Cos(a) * 1.8f, seed + Mathf.Sin(a) * 1.8f) - 0.5f) * 2f;
                    float blob = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(edge - 0.12f, edge, r));
                    float streak = 0.7f + 0.3f * Mathf.PerlinNoise(seed + 3f + v * 11f, u * 0.9f);           // horizontal brush hairs
                    float ragged = r > 0.35f ? Mathf.Clamp01((Mathf.PerlinNoise(seed + u * 16f, v * 16f + seed) - 0.3f) * 4f) : 1f;
                    float alpha = blob * streak * Mathf.Lerp(1f, ragged, Mathf.InverseLerp(0.35f, edge, r));
                    px[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }

            // Drips run down from the lower edge
            int drips = 3 + rng.Next(3);
            for (int d = 0; d < drips; d++)
            {
                float dx = (float)(rng.NextDouble() * 0.9 - 0.45), length = 0.15f + (float)rng.NextDouble() * 0.3f, width = 0.018f + (float)rng.NextDouble() * 0.018f;
                float top = -0.35f;
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float u = x / (float)N * 2f - 1f, v = y / (float)N * 2f - 1f;
                        float t = (top - v) / length;
                        if (t < 0f || t > 1f) continue;
                        float w = width * (1f - 0.5f * t) + (t > 0.85f ? 0.012f : 0f);        // bulb at the tip
                        float edgeSoft = Mathf.Clamp01((w - Mathf.Abs(u - dx)) / (w * 0.5f));        // soft sides
                        if (edgeSoft <= 0f) continue;
                        var c = px[y * N + x];
                        px[y * N + x] = new Color32(255, 255, 255, (byte)Mathf.Max(c.a, (int)(235f * edgeSoft)));
                    }
            }

            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path.Replace("Assets/", Application.dataPath + "/"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Unlit transparent paint material (a decal feel that does not depend on the room light).</summary>
        static Material PaintMaterial(string name, Color color, int variant, bool additive = false)
        {
            if (!AssetDatabase.IsValidFolder(PaintFolder)) AssetDatabase.CreateFolder(MaterialFolder, "Paint");
            string path = $"{PaintFolder}/Paint_{name}_{variant}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", SmearTexture(variant));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", additive ? 2f : 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ------------------------------------------------------------------ the bunker

        /// <summary>
        /// Maintenance bunker clues without the colored lockers: the four code digits are painted smudges on the walls in
        /// the code colors, with many decoy smudges (other colors, random numbers) around the room. Digits and decoys get
        /// hand-painted variance (tilt, size, drift). Also swaps the switch box for a realistic toggle switch.
        /// </summary>
        internal static string DressCodeBunker(Transform root)
        {
            var puzzle = Object.FindAnyObjectByType<ArmoryCodePuzzle>();
            if (puzzle == null) return "no maintenance bunker in this scene";
            var group = puzzle.transform;
            var floor = group.Find("Floor");
            if (floor == null) return "bunker floor missing";
            Vector3 origin = new Vector3(floor.position.x, floor.GetComponent<Renderer>().bounds.max.y, floor.position.z);   // top of the floor slab          // ground level
            Quaternion rot = group.rotation;
            Vector3 P(float x, float y, float z) => origin + rot * new Vector3(x, y, z);
            Vector3 N(float x, float z) => rot * new Vector3(x, 0, z);

            // Remove the old colored lockers, stenciled digits and decoy labels (and earlier runs of this pass)
            var doomed = new List<GameObject>();
            foreach (Transform child in group)
                if (child.name.StartsWith("Locker_") || child.name.StartsWith("Digit_") || child.name.StartsWith("GlowDigit_")
                    || child.name.StartsWith("Decoy_") || child.name == "PaintClues") doomed.Add(child.gameObject);
            foreach (var go in doomed) Object.DestroyImmediate(go);

            var clues = Group(group, "PaintClues");
            clues.position = origin;                                       // so size checks see the group at the bunker, not at the world origin
            var smudgeRenderers = new List<Renderer>();
            var fonts = new[]
            {
                (TMP_FontAsset)null,
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Rajdhani-Bold SDF.asset"),
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/UI/Fonts/Rajdhani-SemiBold SDF.asset"),
            };
            var rng = new System.Random(4242);

            // Wall slots: position along the wall, height, and which wall (normal pointing into the room)
            Vector3 wallN = N(0, -1), wallS = N(0, 1), wallW = N(1, 0), wallE = N(-1, 0);
            Vector3 Wall(Vector3 normal, float along, float y)
            {
                if (normal == wallN) return P(along, y, 3.0f);
                if (normal == wallS) return P(along, y, -3.0f);
                if (normal == wallW) return P(-4.0f, y, along);
                return P(4.0f, y, along);
            }

            GameObject Smudge(string name, Vector3 pos, Vector3 normal, float size, float roll, Color color, int variant, Transform parent)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = name;
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(parent, false);
                q.transform.position = pos + normal * 0.006f;
                q.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up) * Quaternion.Euler(0, 0, roll);
                q.transform.localScale = new Vector3(size * (0.9f + 0.4f * (float)rng.NextDouble()), size, 1f);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = PaintMaterial($"{name.Split('_')[0]}{ColorKey(color)}", color, variant);
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                return q;
            }

            TMP_Text Paint(string name, Vector3 pos, Vector3 normal, Color color, float roll, float size, TMP_FontAsset font, Transform parent)
            {
                var t = Label(parent, name, "0", pos + normal * 0.012f, normal, size, color, 1.4f);
                t.fontStyle = FontStyles.Bold | FontStyles.Italic;
                if (font != null) t.font = font;
                t.enableWordWrapping = false;
                t.transform.Rotate(0f, 0f, roll, Space.Self);
                return t;
            }

            var digits = new TMP_Text[4];
            var glows = new TMP_Text[4];
            // The four code digits: smudge + digit on the north and south walls, each in its code color
            var codeSlots = new (Vector3 normal, float along, float y)[]
            {
                (wallN, -2.5f, 1.55f), (wallS, -1.5f, 1.15f), (wallN, 1.5f, 1.75f), (wallS, 2.5f, 1.35f)
            };
            for (int i = 0; i < 4; i++)
            {
                var (normal, along, y) = codeSlots[i];
                Vector3 pos = Wall(normal, along, y);
                float roll = -12f + 24f * (float)rng.NextDouble();
                var codeSmudge = Smudge($"Smudge_{CodePaints[i].name}", pos, normal, 1.15f, roll * 0.5f, CodePaints[i].smear, i % 4, clues);
                smudgeRenderers.Add(codeSmudge.GetComponent<Renderer>());
                digits[i] = Paint($"Digit_{CodePaints[i].name}", pos, normal, CodePaints[i].digit, roll, 9f, fonts[i % fonts.Length], clues);

                // Glow-paint twin below it: only turned on for the hidden digit, in the dark
                Vector3 glowPos = pos + Vector3.down * 0.7f;
                var glowRoot = Paint($"GlowDigit_{CodePaints[i].name}", glowPos, normal, new Color(0.5f, 1f, 0.45f), roll * -0.6f, 6f, fonts[(i + 1) % fonts.Length], clues);
                var halo = Smudge("GlowHalo", glowPos, normal, 0.9f, roll, new Color(0.3f, 1f, 0.3f, 0.5f), (i + 2) % 4, glowRoot.transform);
                halo.GetComponent<Renderer>().sharedMaterial = PaintMaterial("GlowHalo", new Color(0.3f, 1f, 0.3f, 0.5f), (i + 2) % 4, additive: true);
                glows[i] = glowRoot;
            }

            // Decoys: other colors, mixed sizes and walls; texts are random each run (ArmoryCodePuzzle.Start)
            var decoySlots = new (Vector3 normal, float along, float y, float size)[]
            {
                (wallN, -0.4f, 2.15f, 7f), (wallN, 3.2f, 1.2f, 6f), (wallN, -3.5f, 0.95f, 5f),
                (wallS, -3.4f, 1.7f, 6.5f), (wallS, 0.5f, 0.95f, 7f), (wallS, 3.6f, 2.05f, 5.5f),
                (wallN, 0.1f, 1.3f, 5.5f), (wallS, 1.5f, 1.95f, 6f),
                (wallW, -2.4f, 0.6f, 6f), (wallW, 2.4f, 0.75f, 5f),
            };
            var decoys = new List<TMP_Text>();
            for (int i = 0; i < decoySlots.Length; i++)
            {
                var (normal, along, y, size) = decoySlots[i];
                Vector3 pos = Wall(normal, along, y);
                var paint = DecoyPaints[i % DecoyPaints.Length];
                float roll = -18f + 36f * (float)rng.NextDouble();
                var smudge = Smudge($"Decoy_Smudge{i}", pos, normal, 0.8f + size * 0.06f, roll * 0.6f, paint.smear, (i + 1) % 4, clues);
                smudgeRenderers.Add(smudge.GetComponent<Renderer>());
                decoys.Add(Paint($"Decoy_{paint.name}{i}", pos, normal, paint.digit, roll, size * 1.1f, fonts[i % fonts.Length], clues));
            }
            // A few blank smears and streaks, so not every smudge carries a number
            var blanks = new (Vector3 normal, float along, float y)[] { (wallN, 1.0f, 0.5f), (wallS, -0.4f, 2.3f), (wallE, 0.0f, 2.8f), (wallW, 0.2f, 1.1f) };
            for (int i = 0; i < blanks.Length; i++)
            {
                var (normal, along, y) = blanks[i];
                var paint = (i % 2 == 0 ? DecoyPaints : CodePaints)[(i * 2 + 1) % 4];
                var smudge = Smudge($"Streak_{i}", Wall(normal, along, y), normal, 0.9f, -25f + 50f * (float)rng.NextDouble(), new Color(paint.smear.r, paint.smear.g, paint.smear.b, 0.45f), (i + 3) % 4, clues);
                smudgeRenderers.Add(smudge.GetComponent<Renderer>());
            }

            // The wall message is hand-painted too: slanted, red, marker-like
            var message = puzzle.GetComponentInChildren<TMP_Text>(true);
            foreach (var t in puzzle.GetComponentsInChildren<TMP_Text>(true)) if (t.name == "WallMessage") message = t;
            if (message != null)
            {
                message.fontStyle = FontStyles.Bold | FontStyles.Italic;
                message.transform.localRotation = Quaternion.Euler(0f, -90f, -3f);
                var rajdhani = fonts[1];
                if (rajdhani != null) message.font = rajdhani;
            }

            // Wire the puzzle
            var pso = new SerializedObject(puzzle);
            SetArray(pso.FindProperty("lockerDigits"), digits);
            SetArray(pso.FindProperty("glowDigits"), glows);
            SetArray(pso.FindProperty("decoys"), decoys.ToArray());
            SetArray(pso.FindProperty("smudges"), smudgeRenderers.ToArray());
            pso.ApplyModifiedPropertiesWithoutUndo();

            string furniture = DressCodeBunkerFurniture(group, origin, rot);
            return $"{DressLightSwitch(group, rot)}; {furniture}; {digits.Length} painted digits, {decoys.Count} decoys, {blanks.Length} blank streaks";
        }

        static string ColorKey(Color c) => $"{Mathf.RoundToInt(c.r * 9)}{Mathf.RoundToInt(c.g * 9)}{Mathf.RoundToInt(c.b * 9)}{Mathf.RoundToInt(c.a * 9)}";

        static void SetArray<T>(SerializedProperty prop, T[] items) where T : Object
        {
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // ------------------------------------------------------------------ light switch

        /// <summary>
        /// A heavy-duty surface-mounted toggle switch: steel box with a cover plate, four screws, a chrome collar, and a
        /// metal toggle with a rubber tip that flips up (lights on) and down (off). A conduit runs up to the ceiling.
        /// The old plate keeps its collider and LightSwitch script; the lever pivot is reused and now swings around the
        /// mount's horizontal axis.
        /// </summary>
        static string DressLightSwitch(Transform group, Quaternion rot)
        {
            var plate = group.Find("LightSwitch");
            if (plate == null) return "no light switch";
            var old = group.Find("LightSwitchModel");
            var pivot = plate.Find("LeverPivot");
            if (pivot == null && old != null) pivot = old.Find("Mount/LeverPivot");
            if (pivot == null) return "no lever pivot";
            pivot.SetParent(plate, true);                                   // keep it alive while the old model goes
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var pr = plate.GetComponent<Renderer>();
            if (pr != null) pr.enabled = false;
            var oldLever = pivot.Find("Lever");
            if (oldLever != null) Object.DestroyImmediate(oldLever.gameObject);

            Vector3 normal = rot * Vector3.left;                             // wall faces west, into the room
            Vector3 wallPoint = plate.position + rot * new Vector3(0.05f, 0, 0);
            var model = Group(group, "LightSwitchModel");
            model.position = plate.position;
            var mount = Group(model, "Mount");
            mount.SetPositionAndRotation(wallPoint, Quaternion.LookRotation(normal, Vector3.up));

            var steel = Steel("Switch_Steel", new Color(0.45f, 0.48f, 0.5f));
            var dark = Lit("Console_Dark", new Color(0.04f, 0.045f, 0.05f), 0.5f, 0.5f);
            var chrome = Lit("Switch_Chrome", new Color(0.85f, 0.86f, 0.88f), 1f, 0.85f);
            var rubber = Lit("Switch_Rubber", new Color(0.02f, 0.02f, 0.02f), 0f, 0.3f);
            var cover = Lit("Switch_Cover", new Color(0.62f, 0.64f, 0.62f), 0.6f, 0.45f);

            Part(mount, "Box", PrimitiveType.Cube, new Vector3(0, 0, 0.035f), new Vector3(0.15f, 0.24f, 0.07f), steel).transform.localRotation = Quaternion.identity;
            Part(mount, "Cover", PrimitiveType.Cube, new Vector3(0, 0, 0.074f), new Vector3(0.13f, 0.21f, 0.012f), cover);
            foreach (float sx in new[] { -0.05f, 0.05f })
                foreach (float sy in new[] { -0.085f, 0.085f })
                    Part(mount, "Screw", PrimitiveType.Cylinder, new Vector3(sx, sy, 0.082f), new Vector3(0.014f, 0.004f, 0.014f), chrome, Quaternion.Euler(90, 0, 0));
            Part(mount, "Collar", PrimitiveType.Cylinder, new Vector3(0, 0, 0.086f), new Vector3(0.05f, 0.008f, 0.05f), chrome, Quaternion.Euler(90, 0, 0));
            Part(mount, "Slot", PrimitiveType.Cube, new Vector3(0, 0, 0.081f), new Vector3(0.016f, 0.07f, 0.004f), dark);
            Label(mount, "OnLabel", "ON", mount.TransformPoint(new Vector3(0, 0.088f, 0.081f)), normal, 0.32f, new Color(0.1f, 0.1f, 0.1f), 0.15f);
            Label(mount, "OffLabel", "OFF", mount.TransformPoint(new Vector3(0, -0.088f, 0.081f)), normal, 0.32f, new Color(0.1f, 0.1f, 0.1f), 0.15f);
            // Conduit up to the ceiling with a clamp
            Vector3 top = mount.TransformPoint(new Vector3(0, 0.12f, 0.035f));
            Vector3 ceiling = new Vector3(top.x, plate.position.y + 3.2f - 1.3f, top.z);
            Tube(model, "Conduit", top, ceiling, 0.016f, steel);
            Part(model, "ConduitClamp", PrimitiveType.Cube, Vector3.Lerp(top, ceiling, 0.5f), new Vector3(0.05f, 0.03f, 0.05f), chrome);

            // Toggle: pivot (reused, so LightSwitch keeps its reference) with a chrome stem and a rubber tip
            pivot.SetParent(mount, false);
            pivot.localPosition = new Vector3(0, 0, 0.088f);
            pivot.localRotation = Quaternion.identity;
            pivot.localScale = Vector3.one;
            foreach (Transform c in pivot) Object.DestroyImmediate(c.gameObject);
            Part(pivot, "Stem", PrimitiveType.Cylinder, new Vector3(0, 0, 0.04f), new Vector3(0.014f, 0.04f, 0.014f), chrome, Quaternion.Euler(90, 0, 0));
            Part(pivot, "Tip", PrimitiveType.Sphere, new Vector3(0, 0, 0.085f), new Vector3(0.03f, 0.03f, 0.03f), rubber);

            var ls = plate.GetComponent<LightSwitch>();
            var so = new SerializedObject(ls);
            so.FindProperty("leverAxis").vector3Value = Vector3.right;
            so.FindProperty("onAngle").floatValue = -32f;
            so.FindProperty("offAngle").floatValue = 32f;
            so.FindProperty("lever").objectReferenceValue = pivot;
            so.ApplyModifiedPropertiesWithoutUndo();
            return "toggle switch";
        }
    }
}
