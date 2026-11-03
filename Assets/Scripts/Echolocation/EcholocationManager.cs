using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EcholocationGame
{
    public enum EchoShapeMode
    {
        BoxOrPlane = 0,
        TriangleOrRamp = 1,
        Sphere = 2,
        Cylinder = 3
    }

    [ExecuteAlways]
    [DefaultExecutionOrder(-50)]
    public class EcholocationManager : MonoBehaviour
    {
        public static EcholocationManager Instance { get; private set; }

        public const int MaxPulses = 32;

        [Serializable]
        public class EchoPulse
        {
            public Vector3 position;
            public float currentRadius;
            public float maxRadius;
            public float expandSpeed;
            public float ringWidth;
            public float age;
            public float duration;
            public float interiorReveal;
            public Color color;
        }

        [Header("Material & Shader")]
        [SerializeField] private Shader echolocationShader;
        [SerializeField] private Material sharedEchoMaterial;

        [Header("Footstep Pulse (Lebih Kecil dari Lempar Barang)")]
        public float walkStepRadius = 5.5f;
        public float sprintStepRadius = 8.5f;
        public float stepExpandSpeed = 10.5f;
        public float stepDuration = 1.75f;
        public float stepRingWidth = 0.20f;

        [Header("Jump Landing Pulse (Muncul Saat Menyentuh Tanah)")]
        public float jumpLandBaseRadius = 16.5f;
        public float jumpLandMaxRadius = 21.0f;
        public float jumpExpandSpeed = 15.5f;
        public float jumpDuration = 2.8f;

        [Header("Thrown Object Impact Pulse (Besar Saat Tabrakan Pertama, Makin Kecil Tiap Pantulan)")]
        public float throwFirstImpactRadius = 17.5f;
        public float throwMaxImpactRadius = 22.0f;
        public float throwExpandSpeed = 15.0f;
        public float throwDuration = 2.9f;
        public float throwRingWidth = 0.26f;

        [Header("Echolocation Noise Indicator Bar (0 - 100)")]
        [Range(0f, 100f)]
        public float indicatorValue = 0f;
        public float maxIndicatorValue = 100f;
        [Tooltip("Kecepatan turunnya Indicator Bar per detik saat tidak ada echolocation")]
        public float indicatorDecayPerSecond = 13f;
        [Tooltip("Ambang batas Indicator Bar dimana Monster mulai mendekati sumber echolocation")]
        public float monsterAttractThreshold = 12f;

        public Vector3 LastSoundOrigin { get; private set; }
        public float LastSoundRadius { get; private set; }
        public float LastSoundTime { get; private set; } = -100f;

        // Event dipanggil setiap ada echolocation dari player atau objek lemparan: (posisi, radius, nilaiIndicator01)
        public event Action<Vector3, float, float> OnPlayerEcholocationEmitted;

        private readonly List<EchoPulse> _activePulses = new List<EchoPulse>(MaxPulses);
        private readonly Vector4[] _pulsePositions = new Vector4[MaxPulses];
        private readonly Vector4[] _pulseParams = new Vector4[MaxPulses];
        private readonly Vector4[] _pulseColors = new Vector4[MaxPulses];

        private static readonly int PulseCountId = Shader.PropertyToID("_PulseCount");
        private static readonly int PulsePositionsId = Shader.PropertyToID("_PulsePositions");
        private static readonly int PulseParamsId = Shader.PropertyToID("_PulseParams");
        private static readonly int PulseColorsId = Shader.PropertyToID("_PulseColors");

        private AudioClip _footstepClip;
        private AudioClip _impactClip;
        private AudioClip _throwClip;
        private AudioClip _monsterStepClip;

        public float IndicatorNormalized => Mathf.Clamp01(indicatorValue / Mathf.Max(1f, maxIndicatorValue));

        private void Awake()
        {
            if (Application.isPlaying && Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureShaderAndMaterial();
            ConfigureBlackVisionEnvironment();
            if (Application.isPlaying)
            {
                _activePulses.Clear();
                indicatorValue = 0f;
                GenerateProceduralAudioClips();
            }
        }

        private void OnEnable()
        {
            Instance = this;
            EnsureShaderAndMaterial();
            ConfigureBlackVisionEnvironment();
        }

        private void OnDisable()
        {
            Shader.SetGlobalInt(PulseCountId, 0);
        }

        public void EnsureShaderAndMaterial()
        {
            if (echolocationShader == null)
            {
                echolocationShader = Shader.Find("Custom/Echolocation");
            }

            if (sharedEchoMaterial == null && echolocationShader != null)
            {
                sharedEchoMaterial = new Material(echolocationShader)
                {
                    name = "Echolocation_DefaultMat",
                    enableInstancing = true
                };
                sharedEchoMaterial.SetColor("_BaseColor", Color.black);
                sharedEchoMaterial.SetColor("_EdgeColor", Color.white);
                sharedEchoMaterial.SetFloat("_EdgeWidth", 1.65f);
            }
        }

        public Material GetOrCreateMaterial()
        {
            EnsureShaderAndMaterial();
            return sharedEchoMaterial;
        }

        public void ConfigureBlackVisionEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = false;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = Color.black;
            }

            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                l.enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                // Preview di Scene View (Edit Mode) agar map terlihat saat diedit
                _pulsePositions[0] = new Vector4(0f, 0f, 6f, 42f);
                _pulseParams[0] = new Vector4(55f, 0.25f, 0.85f, 0.9f);
                _pulseColors[0] = new Vector4(1f, 1f, 1f, 1f);

                Shader.SetGlobalInt(PulseCountId, 1);
                Shader.SetGlobalVectorArray(PulsePositionsId, _pulsePositions);
                Shader.SetGlobalVectorArray(PulseParamsId, _pulseParams);
                Shader.SetGlobalVectorArray(PulseColorsId, _pulseColors);
                return;
            }

            float dt = Time.deltaTime;

            // Turunkan Indicator Bar secara perlahan jika tidak ada suara baru
            if (indicatorValue > 0f)
            {
                indicatorValue = Mathf.Max(0f, indicatorValue - indicatorDecayPerSecond * dt);
            }

            for (int i = _activePulses.Count - 1; i >= 0; i--)
            {
                EchoPulse p = _activePulses[i];
                p.age += dt;
                p.currentRadius += p.expandSpeed * dt;

                // Efek echolocation mengecil & hilang saat mencapai jarak maksimum atau durasi habis
                if (p.age >= p.duration || p.currentRadius >= p.maxRadius)
                {
                    _activePulses.RemoveAt(i);
                }
            }

            UpdateShaderGlobals();
        }

        private void UpdateShaderGlobals()
        {
            int count = Mathf.Min(_activePulses.Count, MaxPulses);
            Shader.SetGlobalInt(PulseCountId, count);

            for (int i = 0; i < count; i++)
            {
                EchoPulse p = _activePulses[i];
                float lifeRatio = Mathf.Clamp01(p.age / Mathf.Max(0.01f, p.duration));
                float distRatio = Mathf.Clamp01(p.currentRadius / Mathf.Max(0.01f, p.maxRadius));

                // Semakin jauh merambat, kekuatan & ketebalan gelombang semakin kecil dan akhirnya 0 (hilang)
                float fadeByTime = 1f - Mathf.Pow(lifeRatio, 1.35f);
                float fadeByDistance = 1f - Mathf.Pow(distRatio, 1.25f);
                float alpha = Mathf.Clamp01(fadeByTime * fadeByDistance);

                _pulsePositions[i] = new Vector4(p.position.x, p.position.y, p.position.z, p.currentRadius);
                _pulseParams[i] = new Vector4(p.maxRadius, p.ringWidth, alpha, p.interiorReveal);
                _pulseColors[i] = new Vector4(p.color.r, p.color.g, p.color.b, 1f);
            }

            if (count > 0)
            {
                Shader.SetGlobalVectorArray(PulsePositionsId, _pulsePositions);
                Shader.SetGlobalVectorArray(PulseParamsId, _pulseParams);
                Shader.SetGlobalVectorArray(PulseColorsId, _pulseColors);
            }
        }

        public void EmitPulse(
            Vector3 worldPos,
            float maxRadius,
            float expandSpeed,
            float duration,
            float ringWidth = 0.24f,
            float interiorReveal = 0.95f,
            Color? pulseColor = null,
            bool affectsIndicatorAndMonster = true)
        {
            if (_activePulses.Count >= MaxPulses)
            {
                _activePulses.RemoveAt(0);
            }

            // Pastikan durasi sinkron dengan waktu tempuh gelombang mencapai maxRadius sehingga hilang tepat saat mencapai batas jauh
            float travelTime = maxRadius / Mathf.Max(0.1f, expandSpeed);
            float effectiveDuration = Mathf.Min(duration, travelTime);

            EchoPulse pulse = new EchoPulse
            {
                position = worldPos,
                currentRadius = 0.08f,
                maxRadius = maxRadius,
                expandSpeed = expandSpeed,
                ringWidth = ringWidth,
                age = 0f,
                duration = effectiveDuration,
                interiorReveal = interiorReveal,
                color = pulseColor ?? Color.white
            };

            _activePulses.Add(pulse);

            if (affectsIndicatorAndMonster && Application.isPlaying)
            {
                RegisterSoundForIndicatorAndMonster(worldPos, maxRadius);
            }
        }

        private void RegisterSoundForIndicatorAndMonster(Vector3 soundOrigin, float soundRadius)
        {
            // Semakin besar echolocation yang diberikan, semakin tinggi kenaikan Indicator Bar
            // Langkah kaki (radius ~5.5) -> naik ~11 poin
            // Lempar barang tabrakan pertama / Lompat mendarat (radius ~18) -> naik ~45 poin
            float normalizedSize = Mathf.Clamp01(soundRadius / 20f);
            float indicatorGain = Mathf.Lerp(4f, 48f, Mathf.Pow(normalizedSize, 1.15f));

            indicatorValue = Mathf.Clamp(indicatorValue + indicatorGain, 0f, maxIndicatorValue);
            LastSoundOrigin = soundOrigin;
            LastSoundRadius = soundRadius;
            LastSoundTime = Time.time;

            if (indicatorValue >= monsterAttractThreshold)
            {
                OnPlayerEcholocationEmitted?.Invoke(soundOrigin, soundRadius, IndicatorNormalized);
            }
        }

        /// <summary>
        /// Langkah kaki biasa / sprint: echolocation lebih kecil dibanding lempar barang & lompat.
        /// </summary>
        public void EmitFootstepPulse(Vector3 footPosition, bool isSprinting = false)
        {
            float radius = isSprinting ? sprintStepRadius : walkStepRadius;
            float speed = isSprinting ? stepExpandSpeed * 1.15f : stepExpandSpeed;

            EmitPulse(footPosition, radius, speed, stepDuration, stepRingWidth, 0.88f, Color.white, true);
            PlaySpatialSound(_footstepClip, footPosition, isSprinting ? 0.45f : 0.25f, Random.Range(0.92f, 1.08f));
        }

        /// <summary>
        /// Echolocation saat mendarat dari lompatan (menyentuh tanah): ukuran besar seperti lempar barang.
        /// </summary>
        public void EmitJumpLandingPulse(Vector3 footPosition, float fallSpeed)
        {
            float intensity = Mathf.Clamp01((fallSpeed - 3f) / 10f);
            float radius = Mathf.Lerp(jumpLandBaseRadius, jumpLandMaxRadius, intensity);

            EmitPulse(footPosition, radius, jumpExpandSpeed, jumpDuration, 0.28f, 1.0f, Color.white, true);
            PlaySpatialSound(_impactClip, footPosition, 0.75f, Random.Range(0.75f, 0.9f));
        }

        /// <summary>
        /// Echolocation dari objek yang dilempar HANYA saat bertabrakan, dan makin kecil setiap pantulan berikutnya.
        /// </summary>
        public void EmitThrowBouncePulse(Vector3 impactPoint, float impactSpeed, int bounceIndex)
        {
            // Pantulan ke-1 = 100%, pantulan ke-2 = 55%, ke-3 = 30%, ke-4 = 16%, ke-5 = 9%
            float bounceMultiplier = Mathf.Pow(0.55f, Mathf.Max(0, bounceIndex - 1));
            float speedFactor = Mathf.Clamp01(impactSpeed / 12f);

            float baseRadius = Mathf.Lerp(throwFirstImpactRadius * 0.75f, throwMaxImpactRadius, speedFactor);
            float radius = baseRadius * bounceMultiplier;

            if (radius < 1.2f) return;

            float speed = Mathf.Lerp(8.5f, throwExpandSpeed, bounceMultiplier);
            float duration = Mathf.Lerp(1.0f, throwDuration, bounceMultiplier);
            float ringW = Mathf.Lerp(0.12f, throwRingWidth, bounceMultiplier);

            EmitPulse(impactPoint, radius, speed, duration, ringW, 0.98f, Color.white, true);
            PlaySpatialSound(_impactClip, impactPoint, Mathf.Clamp01(0.75f * bounceMultiplier), Random.Range(0.9f, 1.2f));
        }

        /// <summary>
        /// Gelombang echolocation merah kecil dari langkah kaki Monster (tidak menaikkan indicator player).
        /// </summary>
        public void EmitMonsterFootstepPulse(Vector3 monsterFootPos, bool isChasing)
        {
            float radius = isChasing ? 7.5f : 4.8f;
            float speed = isChasing ? 11f : 8f;
            Color monsterRed = new Color(1f, 0.14f, 0.14f, 1f);

            EmitPulse(monsterFootPos, radius, speed, 1.4f, 0.18f, 0.9f, monsterRed, false);
            PlaySpatialSound(_monsterStepClip, monsterFootPos, isChasing ? 0.55f : 0.35f, Random.Range(0.7f, 0.85f));
        }

        public void PlayThrowWhoosh(Vector3 pos)
        {
            PlaySpatialSound(_throwClip, pos, 0.35f, Random.Range(0.95f, 1.08f));
        }

        private void PlaySpatialSound(AudioClip clip, Vector3 pos, float volume, float pitch)
        {
            if (clip == null) return;

            GameObject audioObj = new GameObject("EchoAudioOneShot");
            audioObj.transform.position = pos;
            AudioSource src = audioObj.AddComponent<AudioSource>();
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.spatialBlend = 0.85f;
            src.minDistance = 1.5f;
            src.maxDistance = 32f;
            src.Play();
            Destroy(audioObj, clip.length / Mathf.Max(0.1f, pitch) + 0.05f);
        }

        private void GenerateProceduralAudioClips()
        {
            int sampleRate = 44100;

            // 1. Footstep click + low thud
            int stepSamples = sampleRate / 6;
            float[] stepData = new float[stepSamples];
            for (int i = 0; i < stepSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 32f);
                float thud = Mathf.Sin(2f * Mathf.PI * 110f * t * Mathf.Exp(-t * 12f));
                float click = Mathf.Sin(2f * Mathf.PI * 620f * t) * Mathf.Exp(-t * 80f);
                stepData[i] = (thud * 0.7f + click * 0.3f) * env;
            }
            _footstepClip = AudioClip.Create("ProcFootstep", stepSamples, 1, sampleRate, false);
            _footstepClip.SetData(stepData, 0);

            // 2. Impact bounce + sonar resonance
            int impactSamples = sampleRate / 3;
            float[] impactData = new float[impactSamples];
            for (int i = 0; i < impactSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 16f);
                float ping = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.5f +
                             Mathf.Sin(2f * Mathf.PI * 1040f * t) * 0.25f +
                             Mathf.Sin(2f * Mathf.PI * 190f * t * Mathf.Exp(-t * 8f)) * 0.4f;
                impactData[i] = ping * env;
            }
            _impactClip = AudioClip.Create("ProcImpact", impactSamples, 1, sampleRate, false);
            _impactClip.SetData(impactData, 0);

            // 3. Throw whoosh
            int throwSamples = sampleRate / 7;
            float[] throwData = new float[throwSamples];
            for (int i = 0; i < throwSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Sin(Mathf.PI * (i / (float)throwSamples));
                float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(260f, 160f, i / (float)throwSamples) * t);
                throwData[i] = sweep * env * 0.4f;
            }
            _throwClip = AudioClip.Create("ProcThrow", throwSamples, 1, sampleRate, false);
            _throwClip.SetData(throwData, 0);

            // 4. Heavy monster footstep
            int monsterSamples = sampleRate / 4;
            float[] monsterData = new float[monsterSamples];
            for (int i = 0; i < monsterSamples; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t * 18f);
                float growlThud = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.8f +
                                  Mathf.Sin(2f * Mathf.PI * 130f * t) * 0.3f;
                monsterData[i] = growlThud * env;
            }
            _monsterStepClip = AudioClip.Create("ProcMonsterStep", monsterSamples, 1, sampleRate, false);
            _monsterStepClip.SetData(monsterData, 0);
        }
    }
}
