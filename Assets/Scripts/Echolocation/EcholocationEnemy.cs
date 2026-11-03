using System.Collections.Generic;
using UnityEngine;

namespace EcholocationGame
{
    [RequireComponent(typeof(CharacterController))]
    public class EcholocationEnemy : MonoBehaviour
    {
        public enum EnemyState
        {
            Patrol = 0,
            ApproachingEcholocation = 1,
            SearchingLastEcho = 2
        }

        [Header("Status Monster")]
        public EnemyState currentState = EnemyState.Patrol;
        public Vector3 targetEchoPosition;

        [Header("Kecepatan Gerak")]
        public float patrolSpeed = 2.0f;
        public float baseInvestigateSpeed = 3.8f;
        public float maxChaseSpeed = 7.4f;
        public float rotationSpeed = 7.0f;
        public float gravity = -19.6f;

        [Header("Outliner Merah Settings")]
        public Color redOutlineColor = new Color(1.0f, 0.12f, 0.12f, 1.0f);
        [Range(0.1f, 1f)]
        public float baseRedOutlineVisibility = 0.55f;

        [Header("Patrol Waypoints")]
        public Vector3[] patrolPoints = new Vector3[]
        {
            new Vector3(-8f, 1.1f, 8f),
            new Vector3( 8f, 1.1f, 8f),
            new Vector3( 8f, 1.1f, 22f),
            new Vector3(-8f, 1.1f, 22f),
            new Vector3( 0f, 1.1f, 3f)
        };

        private CharacterController _controller;
        private Vector3 _spawnPosition;
        private int _currentPatrolIndex = 0;
        private float _searchTimer = 0f;
        private float _stepDistanceAccum = 0f;
        private float _verticalVelocity = 0f;
        private float _animTime = 0f;

        // Visual body parts for procedural walk/menacing animation
        private Transform _visualRoot;
        private Transform _headPart;
        private Transform _leftArmPart;
        private Transform _rightArmPart;
        private readonly List<EcholocationSurface> _enemySurfaces = new List<EcholocationSurface>();

        // Player catch event
        public static float LastCatchTime { get; private set; } = -100f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _spawnPosition = transform.position;
            BuildRedOutlinedMonsterVisuals();
        }

        private void OnEnable()
        {
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.OnPlayerEcholocationEmitted += HandleEcholocationDetected;
            }
        }

        private void Start()
        {
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.OnPlayerEcholocationEmitted -= HandleEcholocationDetected;
                EcholocationManager.Instance.OnPlayerEcholocationEmitted += HandleEcholocationDetected;
            }
        }

        private void OnDisable()
        {
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.OnPlayerEcholocationEmitted -= HandleEcholocationDetected;
            }
        }

        /// <summary>
        /// Dipanggil setiap kali ada echolocation (dari langkah kaki, lompatan mendarat, atau lemparan barang yang tertabrak).
        /// Monster akan mengejar menuju titik lokasi dimana echolocation itu berada!
        /// </summary>
        private void HandleEcholocationDetected(Vector3 echoOrigin, float echoRadius, float indicatorNormalized)
        {
            targetEchoPosition = new Vector3(echoOrigin.x, transform.position.y, echoOrigin.z);
            currentState = EnemyState.ApproachingEcholocation;
            _searchTimer = Mathf.Lerp(2.5f, 5.5f, indicatorNormalized);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float dt = Time.deltaTime;
            float indicator01 = EcholocationManager.Instance != null ? EcholocationManager.Instance.IndicatorNormalized : 0f;

            // Update state machine berdasarkan lokasi echolocation & level Indicator Bar
            Vector3 desiredTarget = transform.position;
            float moveSpeed = patrolSpeed;

            if (currentState == EnemyState.ApproachingEcholocation)
            {
                desiredTarget = targetEchoPosition;
                // Semakin tinggi Indicator Bar, semakin cepat Monster mengejar titik echolocation tersebut
                moveSpeed = Mathf.Lerp(baseInvestigateSpeed, maxChaseSpeed, indicator01);

                float distToEcho = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(targetEchoPosition.x, targetEchoPosition.z));

                if (distToEcho < 1.15f)
                {
                    currentState = EnemyState.SearchingLastEcho;
                }
            }
            else if (currentState == EnemyState.SearchingLastEcho)
            {
                _searchTimer -= dt;
                moveSpeed = 0f;
                // Tengok kanan-kiri mencari sumber suara di titik echolocation terakhir
                transform.Rotate(Vector3.up * (Mathf.Sin(Time.time * 3.5f) * 65f * dt));

                if (_searchTimer <= 0f && indicator01 < 0.15f)
                {
                    currentState = EnemyState.Patrol;
                }
            }
            else
            {
                // Patroli normal saat tidak ada echolocation besar
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    desiredTarget = patrolPoints[_currentPatrolIndex];
                    float dist = Vector2.Distance(
                        new Vector2(transform.position.x, transform.position.z),
                        new Vector2(desiredTarget.x, desiredTarget.z));

                    if (dist < 1.4f)
                    {
                        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
                        desiredTarget = patrolPoints[_currentPatrolIndex];
                    }
                }
            }

            // Gerakkan Monster dengan steering penghindar rintangan
            MoveTowardsTargetWithSteering(desiredTarget, moveSpeed, dt);

            // Animasi badan & kekuatan Outliner Merah
            UpdateMonsterAnimationAndRedOutline(moveSpeed, indicator01, dt);

            // Cek apakah Monster menyentuh Player
            CheckPlayerCatch();
        }

        private void MoveTowardsTargetWithSteering(Vector3 targetPos, float speed, float dt)
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            _verticalVelocity += gravity * dt;

            Vector3 horizMove = Vector3.zero;
            if (speed > 0.05f)
            {
                Vector3 toTarget = targetPos - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude > 0.05f)
                {
                    Vector3 desiredDir = toTarget.normalized;
                    Vector3 steeredDir = ComputeObstacleAvoidanceDir(desiredDir);

                    Quaternion targetRot = Quaternion.LookRotation(steeredDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * dt);

                    horizMove = steeredDir * speed;
                }
            }

            Vector3 before = transform.position;
            _controller.Move((horizMove + Vector3.up * _verticalVelocity) * dt);
            Vector3 after = transform.position;

            float stepDist = Vector2.Distance(new Vector2(before.x, before.z), new Vector2(after.x, after.z));
            if (_controller.isGrounded && stepDist > 0.001f)
            {
                _stepDistanceAccum += stepDist;
                float stepInterval = currentState == EnemyState.ApproachingEcholocation ? 2.1f : 2.6f;
                if (_stepDistanceAccum >= stepInterval)
                {
                    _stepDistanceAccum -= stepInterval;
                    if (EcholocationManager.Instance != null)
                    {
                        Vector3 footPos = new Vector3(transform.position.x, transform.position.y - (_controller.height * 0.5f) + 0.06f, transform.position.z);
                        EcholocationManager.Instance.EmitMonsterFootstepPulse(footPos, currentState == EnemyState.ApproachingEcholocation);
                    }
                }
            }
        }

        private Vector3 ComputeObstacleAvoidanceDir(Vector3 desiredDir)
        {
            Vector3 origin = transform.position + Vector3.up * 0.2f;
            float checkDist = 2.4f;

            // Jika arah lurus ke sumber echolocation bebas dari tembok/balok, langsung maju
            if (!Physics.SphereCast(origin, 0.4f, desiredDir, out RaycastHit hit, checkDist) ||
                hit.collider.GetComponent<EcholocationPlayerController>() != null)
            {
                return desiredDir;
            }

            // Cari sudut alternatif untuk memutari dinding/rintangan
            float[] testAngles = { 35f, -35f, 65f, -65f, 95f, -95f, 130f, -130f };
            foreach (float angle in testAngles)
            {
                Vector3 candidateDir = Quaternion.Euler(0f, angle, 0f) * desiredDir;
                if (!Physics.SphereCast(origin, 0.38f, candidateDir, out RaycastHit sideHit, checkDist * 0.9f) ||
                    sideHit.collider.GetComponent<EcholocationPlayerController>() != null)
                {
                    return candidateDir;
                }
            }

            return -desiredDir;
        }

        private void UpdateMonsterAnimationAndRedOutline(float currentSpeed, float indicator01, float dt)
        {
            _animTime += dt * Mathf.Max(1.5f, currentSpeed * 1.6f);

            if (_visualRoot != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(_animTime)) * 0.06f;
                _visualRoot.localPosition = new Vector3(0f, bob, 0f);
            }

            if (_leftArmPart != null && _rightArmPart != null)
            {
                float swing = Mathf.Sin(_animTime) * (currentState == EnemyState.ApproachingEcholocation ? 38f : 18f);
                float reachForward = currentState == EnemyState.ApproachingEcholocation ? -35f : -10f;
                _leftArmPart.localRotation = Quaternion.Euler(reachForward + swing, 0f, 12f);
                _rightArmPart.localRotation = Quaternion.Euler(reachForward - swing, 0f, -12f);
            }

            if (_headPart != null)
            {
                float headTilt = Mathf.Sin(_animTime * 0.7f) * 8f;
                _headPart.localRotation = Quaternion.Euler(0f, headTilt, 0f);
            }

            // Outliner merah selalu terlihat samar & menyala makin terang saat mengejar sumber echolocation
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (currentState == EnemyState.ApproachingEcholocation ? 8f : 3f));
            float visibility = currentState == EnemyState.ApproachingEcholocation
                ? Mathf.Lerp(0.75f, 1.0f, pulse)
                : Mathf.Lerp(baseRedOutlineVisibility * 0.8f, baseRedOutlineVisibility * 1.15f, pulse);

            foreach (EcholocationSurface surf in _enemySurfaces)
            {
                if (surf == null) continue;
                surf.edgeColor = redOutlineColor;
                surf.alwaysVisibleEdge = visibility;
                surf.edgeWidth = currentState == EnemyState.ApproachingEcholocation ? 2.1f : 1.8f;
                surf.ApplySetup();
            }
        }

        private void CheckPlayerCatch()
        {
            EcholocationPlayerController player = FindFirstObjectByType<EcholocationPlayerController>();
            if (player == null) return;

            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer < 1.35f)
            {
                LastCatchTime = Time.time;

                // Reset posisi player & monster agar bisa lanjut bermain
                CharacterController playerCC = player.GetComponent<CharacterController>();
                if (playerCC != null) playerCC.enabled = false;
                player.transform.position = new Vector3(0f, 1.05f, -7.5f);
                player.transform.rotation = Quaternion.identity;
                if (playerCC != null) playerCC.enabled = true;

                _controller.enabled = false;
                transform.position = _spawnPosition;
                _controller.enabled = true;

                currentState = EnemyState.Patrol;
                if (EcholocationManager.Instance != null)
                {
                    EcholocationManager.Instance.indicatorValue = 0f;
                    EcholocationManager.Instance.EmitMonsterFootstepPulse(player.transform.position, true);
                }
            }
        }

        public void BuildRedOutlinedMonsterVisuals()
        {
            Transform existing = transform.Find("MonsterVisual_Root");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
            _enemySurfaces.Clear();

            GameObject root = new GameObject("MonsterVisual_Root");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            _visualRoot = root.transform;

            // 1. Torso (Balok Tegak dengan Outliner Merah)
            GameObject torso = EcholocationMeshBuilder.CreateBox(
                "Monster_Torso",
                Vector3.zero,
                new Vector3(0.75f, 1.25f, 0.45f),
                Quaternion.identity,
                _visualRoot,
                baseRedOutlineVisibility);
            torso.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            StripColliderAndRegisterRed(torso);

            // 2. Head (Bola + Tanduk Segitiga dengan Outliner Merah)
            GameObject head = EcholocationMeshBuilder.CreateSphere(
                "Monster_Head",
                Vector3.zero,
                new Vector3(0.52f, 0.55f, 0.52f),
                _visualRoot,
                baseRedOutlineVisibility);
            head.transform.localPosition = new Vector3(0f, 0.98f, 0.04f);
            StripColliderAndRegisterRed(head);
            _headPart = head.transform;

            // Left & Right Horns (Segitiga Limas Tajam)
            GameObject hornL = EcholocationMeshBuilder.CreatePyramid(
                "Monster_HornL",
                Vector3.zero,
                new Vector3(0.35f, 0.75f, 0.35f),
                Quaternion.identity,
                _headPart,
                true,
                baseRedOutlineVisibility);
            hornL.transform.localPosition = new Vector3(-0.35f, 0.45f, 0f);
            hornL.transform.localRotation = Quaternion.Euler(15f, 0f, 28f);
            StripColliderAndRegisterRed(hornL);

            GameObject hornR = EcholocationMeshBuilder.CreatePyramid(
                "Monster_HornR",
                Vector3.zero,
                new Vector3(0.35f, 0.75f, 0.35f),
                Quaternion.identity,
                _headPart,
                true,
                baseRedOutlineVisibility);
            hornR.transform.localPosition = new Vector3(0.35f, 0.45f, 0f);
            hornR.transform.localRotation = Quaternion.Euler(15f, 0f, -28f);
            StripColliderAndRegisterRed(hornR);

            // 3. Shoulder Spikes (Segitiga di Bahu Kiri & Kanan)
            GameObject spikeL = EcholocationMeshBuilder.CreatePyramid(
                "Monster_ShoulderSpikeL",
                Vector3.zero,
                new Vector3(0.32f, 0.5f, 0.32f),
                Quaternion.identity,
                _visualRoot,
                true,
                baseRedOutlineVisibility);
            spikeL.transform.localPosition = new Vector3(-0.52f, 0.65f, 0f);
            spikeL.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            StripColliderAndRegisterRed(spikeL);

            GameObject spikeR = EcholocationMeshBuilder.CreatePyramid(
                "Monster_ShoulderSpikeR",
                Vector3.zero,
                new Vector3(0.32f, 0.5f, 0.32f),
                Quaternion.identity,
                _visualRoot,
                true,
                baseRedOutlineVisibility);
            spikeR.transform.localPosition = new Vector3(0.52f, 0.65f, 0f);
            spikeR.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            StripColliderAndRegisterRed(spikeR);

            // 4. Arms (Tangan Panjang Beroutline Merah)
            GameObject armL = EcholocationMeshBuilder.CreateBox(
                "Monster_ArmL",
                Vector3.zero,
                new Vector3(0.22f, 0.95f, 0.22f),
                Quaternion.identity,
                _visualRoot,
                baseRedOutlineVisibility);
            armL.transform.localPosition = new Vector3(-0.55f, 0.15f, 0.1f);
            StripColliderAndRegisterRed(armL);
            _leftArmPart = armL.transform;

            GameObject armR = EcholocationMeshBuilder.CreateBox(
                "Monster_ArmR",
                Vector3.zero,
                new Vector3(0.22f, 0.95f, 0.22f),
                Quaternion.identity,
                _visualRoot,
                baseRedOutlineVisibility);
            armR.transform.localPosition = new Vector3(0.55f, 0.15f, 0.1f);
            StripColliderAndRegisterRed(armR);
            _rightArmPart = armR.transform;

            // 5. Legs
            GameObject legL = EcholocationMeshBuilder.CreateBox(
                "Monster_LegL",
                Vector3.zero,
                new Vector3(0.26f, 0.75f, 0.26f),
                Quaternion.identity,
                _visualRoot,
                baseRedOutlineVisibility);
            legL.transform.localPosition = new Vector3(-0.22f, -0.72f, 0f);
            StripColliderAndRegisterRed(legL);

            GameObject legR = EcholocationMeshBuilder.CreateBox(
                "Monster_LegR",
                Vector3.zero,
                new Vector3(0.26f, 0.75f, 0.26f),
                Quaternion.identity,
                _visualRoot,
                baseRedOutlineVisibility);
            legR.transform.localPosition = new Vector3(0.22f, -0.72f, 0f);
            StripColliderAndRegisterRed(legR);
        }

        private void StripColliderAndRegisterRed(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            EcholocationSurface surf = go.GetComponent<EcholocationSurface>();
            if (surf != null)
            {
                surf.edgeColor = redOutlineColor;
                surf.alwaysVisibleEdge = baseRedOutlineVisibility;
                surf.edgeWidth = 1.9f;
                surf.ApplySetup();
                _enemySurfaces.Add(surf);
            }
        }
    }
}
