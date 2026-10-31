using System.Collections;
using UnityEngine;

namespace SignalLost
{
    /// <summary>
    /// The storm for the final stretch. Once the signal upload completes (the Titan wakes at the pier) the island fades into rain:
    /// the sun, ambient light and sky get darker, the fog closes in and turns grey, the sea picks up, rain streaks fall around the player
    /// (they stop on roofs and ground), a rain loop plays and lightning flashes now and then with a rumble of thunder. If the scene
    /// loads with the upload already done (a checkpoint retry, coming back up the elevator) the storm is on from the first frame.
    /// All sounds are synthesized here, so there are no extra audio assets. Placed in IslandExterior by DetailArtBuilder (art-details).
    /// </summary>
    public class StormController : MonoBehaviour
    {
        [SerializeField] Material rainMaterial;
        [SerializeField, Tooltip("Seconds to go from clear to full storm")] float fadeSeconds = 8f;
        [SerializeField, Range(0.1f, 1f), Tooltip("Sun brightness at full storm, as a fraction of the clear value")] float sunDimming = 0.7f;
        [SerializeField] int rainRate = 1800;

        // Clear-weather values, captured on start
        Light sun;
        float clearSun;
        Color clearSunColor, clearSky, clearEquator, clearGround, clearFog;
        float clearFogStart, clearFogEnd, clearExposure;
        Color clearTint = Color.gray;
        Material skyInstance;
        Renderer ocean;
        Material oceanInstance;
        float clearWaveSpeed, clearRipple;

        ParticleSystem rain;
        AudioSource rainSource, thunderSource;
        AudioClip thunderClip;
        Transform player;
        float intensity, lastGiUpdate, nextLightning;
        bool first = true;
        Coroutine flash;

        static readonly Color StormSun = new Color(0.62f, 0.7f, 0.82f);
        static readonly Color StormSky = new Color(0.34f, 0.4f, 0.48f);
        static readonly Color StormFog = new Color(0.24f, 0.27f, 0.31f);

        public float Intensity => intensity;
        public float SunIntensity => sun != null ? sun.intensity : 0f;
        public int RainParticles => rain != null ? rain.particleCount : 0;

        void Start()
        {
            sun = RenderSettings.sun;
            if (sun != null) { clearSun = sun.intensity; clearSunColor = sun.color; }
            clearSky = RenderSettings.ambientSkyColor;
            clearEquator = RenderSettings.ambientEquatorColor;
            clearGround = RenderSettings.ambientGroundColor;
            clearFog = RenderSettings.fogColor;
            clearFogStart = RenderSettings.fogStartDistance;
            clearFogEnd = RenderSettings.fogEndDistance;
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
            {
                skyInstance = new Material(RenderSettings.skybox);   // an instance, so the skybox asset itself is never changed
                RenderSettings.skybox = skyInstance;
                clearExposure = skyInstance.GetFloat("_Exposure");
                if (skyInstance.HasProperty("_Tint")) clearTint = skyInstance.GetColor("_Tint");
            }
            var oceanGo = GameObject.Find("Ocean");
            if (oceanGo != null && oceanGo.TryGetComponent(out ocean))
            {
                oceanInstance = ocean.material;                      // per-renderer copy
                if (oceanInstance.HasProperty("_WaveSpeed")) { clearWaveSpeed = oceanInstance.GetFloat("_WaveSpeed"); clearRipple = oceanInstance.GetFloat("_NormalStrength"); }
            }
            BuildRain();
            BuildAudio();
            nextLightning = Time.time + 6f;
        }

        void BuildRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            rain = go.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1.15f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.04f);
            main.startColor = new Color(0.8f, 0.88f, 0.96f, 0.5f);
            main.maxParticles = 4000;
            main.gravityModifier = 0f;
            var emission = rain.emission;
            emission.rateOverTime = 0f;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(44f, 0.5f, 44f);
            shape.position = new Vector3(0f, 15f, 0f);
            var velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 2.5f;
            velocity.y = -24f;
            velocity.z = 1f;
            var collision = rain.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 1f;
            collision.lifetimeLoss = 1f;                              // drops die on roofs, trees, rocks and the ground
            collision.radiusScale = 0.2f;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.maxCollisionShapes = 64;
            collision.collidesWith = ~(1 << LayerMask.NameToLayer("Player"));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.4f;
            renderer.velocityScale = 0.025f;
            renderer.cameraVelocityScale = 0f;
            renderer.sharedMaterial = rainMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void BuildAudio()
        {
            var group = Sfx.Library != null ? Sfx.Library.sfxGroup : null;
            rainSource = gameObject.AddComponent<AudioSource>();
            rainSource.clip = MakeRainClip();
            rainSource.loop = true;
            rainSource.spatialBlend = 0f;
            rainSource.volume = 0f;
            rainSource.playOnAwake = false;
            rainSource.outputAudioMixerGroup = group;
            thunderSource = gameObject.AddComponent<AudioSource>();
            thunderSource.spatialBlend = 0f;
            thunderSource.playOnAwake = false;
            thunderSource.outputAudioMixerGroup = group;
            thunderClip = MakeThunderClip();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            bool fight = gm != null && gm.IsCleared(WaveSpawner.CompletedId);
            float target = fight ? 1f : 0f;
            if (first) { intensity = target; first = false; }       // already past the upload: no fade
            float before = intensity;
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime / Mathf.Max(0.1f, fadeSeconds));
            if (intensity != before || (intensity > 0f && lastGiUpdate == 0f)) Apply(intensity);

            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }
            if (rain != null)
            {
                if (player != null) rain.transform.position = new Vector3(player.position.x, player.position.y, player.position.z);
                var emission = rain.emission;
                emission.rateOverTime = rainRate * intensity;
                if (intensity > 0.01f && !rain.isPlaying) rain.Play();
                else if (intensity <= 0.01f && rain.isPlaying) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (rainSource != null)
            {
                rainSource.volume = 0.3f * intensity;
                if (intensity > 0.01f && !rainSource.isPlaying) rainSource.Play();
                else if (intensity <= 0.01f && rainSource.isPlaying) rainSource.Stop();
            }

            if (intensity > 0.95f && Time.time >= nextLightning)
            {
                nextLightning = Time.time + Random.Range(9f, 22f);
                if (flash != null) StopCoroutine(flash);
                flash = StartCoroutine(Lightning());
            }
        }

        void Apply(float t)
        {
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(clearSun, clearSun * sunDimming, t);
                sun.color = Color.Lerp(clearSunColor, StormSun, t * 0.8f);
            }
            float ambient = Mathf.Lerp(1f, 0.8f, t);
            var tint = Color.Lerp(Color.white, new Color(0.8f, 0.88f, 1f), t);
            RenderSettings.ambientSkyColor = clearSky * ambient * tint;
            RenderSettings.ambientEquatorColor = clearEquator * ambient * tint;
            RenderSettings.ambientGroundColor = clearGround * ambient;
            RenderSettings.fogColor = Color.Lerp(clearFog, StormFog, t);
            RenderSettings.fogStartDistance = Mathf.Lerp(clearFogStart, 35f, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(clearFogEnd, 300f, t);
            if (skyInstance != null && skyInstance.HasProperty("_Tint")) skyInstance.SetColor("_Tint", Color.Lerp(clearTint, StormSky, t)); // overcast: the warm sunset is greyed out
            if (skyInstance != null) skyInstance.SetFloat("_Exposure", Mathf.Lerp(clearExposure, clearExposure * 0.62f, t));
            if (oceanInstance != null && oceanInstance.HasProperty("_WaveSpeed"))
            {
                oceanInstance.SetFloat("_WaveSpeed", Mathf.Lerp(clearWaveSpeed, clearWaveSpeed * 2.2f, t));
                oceanInstance.SetFloat("_NormalStrength", Mathf.Lerp(clearRipple, clearRipple * 1.6f, t));
            }
            if (Time.unscaledTime - lastGiUpdate > 0.5f)             // lets the dimmer sky update ambient light and reflections
            {
                lastGiUpdate = Time.unscaledTime;
                DynamicGI.UpdateEnvironment();
            }
        }

        IEnumerator Lightning()
        {
            // Two quick flashes, then the thunder arrives a little later
            float baseSun = sun != null ? sun.intensity : 0f;
            Color baseSky = RenderSettings.ambientSkyColor;
            for (int i = 0; i < 2; i++)
            {
                if (sun != null) sun.intensity = baseSun + 2.2f;
                RenderSettings.ambientSkyColor = baseSky + new Color(0.5f, 0.55f, 0.7f);
                yield return new WaitForSeconds(0.06f);
                if (sun != null) sun.intensity = baseSun;
                RenderSettings.ambientSkyColor = baseSky;
                yield return new WaitForSeconds(0.09f);
            }
            yield return new WaitForSeconds(Random.Range(0.4f, 2.4f));
            if (thunderSource != null && thunderClip != null) thunderSource.PlayOneShot(thunderClip, Random.Range(0.55f, 0.9f));
        }

        void OnDisable()
        {
            if (sun != null) { sun.intensity = clearSun; sun.color = clearSunColor; }
            RenderSettings.ambientSkyColor = clearSky;
            RenderSettings.ambientEquatorColor = clearEquator;
            RenderSettings.ambientGroundColor = clearGround;
            RenderSettings.fogColor = clearFog;
            RenderSettings.fogStartDistance = clearFogStart;
            RenderSettings.fogEndDistance = clearFogEnd;
        }

        // ------------------------------------------------------------------ synthesized sound

        /// <summary>Three seconds of soft, looping rain: filtered noise with a slow swell and a crossfaded loop point.</summary>
        static AudioClip MakeRainClip()
        {
            const int rate = 22050, seconds = 4, fadeSamples = rate / 4;
            int n = rate * seconds;
            var data = new float[n];
            var rng = new System.Random(7);
            float low = 0f, band = 0f;
            for (int i = 0; i < n; i++)
            {
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.12f;              // soft hiss
                band += (low - band) * 0.5f;
                float swell = 0.85f + 0.15f * Mathf.Sin(i / (float)rate * 1.7f);
                data[i] = (low * 0.9f + (white - low) * 0.25f) * swell * 0.5f;
            }
            for (int i = 0; i < fadeSamples; i++)          // blend the end into the start so the loop doesn't click
            {
                float k = i / (float)fadeSamples;
                data[n - fadeSamples + i] = Mathf.Lerp(data[n - fadeSamples + i], data[i], k);
            }
            var clip = AudioClip.Create("Rain", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>A few seconds of low rolling thunder: dark noise under a slow decaying rumble.</summary>
        static AudioClip MakeThunderClip()
        {
            const int rate = 22050;
            float seconds = 4.5f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            var rng = new System.Random(21);
            float low = 0f, low2 = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.025f;
                low2 += (low - low2) * 0.2f;
                float envelope = Mathf.Min(1f, t * 8f) * Mathf.Exp(-t * 0.85f) * (0.65f + 0.35f * Mathf.Sin(t * 5.5f));
                data[i] = Mathf.Clamp(low2 * 7f * envelope, -1f, 1f);
            }
            var clip = AudioClip.Create("Thunder", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
