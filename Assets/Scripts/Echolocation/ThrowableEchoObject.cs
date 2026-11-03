using System.Collections.Generic;
using UnityEngine;

namespace EcholocationGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableEchoObject : MonoBehaviour
    {
        private static readonly Queue<ThrowableEchoObject> ActiveThrownObjects = new Queue<ThrowableEchoObject>();
        private const int MaxThrownObjectsInScene = 25;

        [Header("Bounce Echolocation Settings")]
        public int maxEchoBounces = 5;
        public float minImpactSpeedForPulse = 0.45f;
        public float impactCooldown = 0.06f;

        private Rigidbody _rb;
        private EcholocationSurface _surface;
        private float _lastImpactTime = -10f;
        private int _bounceCount = 0;
        private float _spawnTime;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _surface = GetComponent<EcholocationSurface>();
            _spawnTime = Time.time;

            // Saat masih melayang sebelum menabrak, garis tepi objek itu sendiri tipis/redup
            if (_surface != null)
            {
                _surface.alwaysVisibleEdge = 0.25f;
                _surface.ApplySetup();
            }

            ActiveThrownObjects.Enqueue(this);
            while (ActiveThrownObjects.Count > MaxThrownObjectsInScene)
            {
                ThrowableEchoObject oldest = ActiveThrownObjects.Dequeue();
                if (oldest != null && oldest != this)
                {
                    Destroy(oldest.gameObject);
                }
            }
        }

        private void Update()
        {
            if (_rb == null || _surface == null) return;

            // Setelah berhenti memantul, outline objek menyatu dengan gelap kecuali terkena scan echolocation
            if (_bounceCount > 0 && _rb.linearVelocity.magnitude < 0.4f && _surface.alwaysVisibleEdge > 0f)
            {
                _surface.alwaysVisibleEdge = Mathf.MoveTowards(_surface.alwaysVisibleEdge, 0f, Time.deltaTime * 1.2f);
                _surface.ApplySetup();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Abaikan tabrakan dengan tubuh player saat baru dilempar
            if (Time.time - _spawnTime < 0.08f && collision.gameObject.GetComponent<CharacterController>() != null)
            {
                return;
            }

            if (_bounceCount >= maxEchoBounces)
            {
                return;
            }

            if (Time.time - _lastImpactTime < impactCooldown)
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeedForPulse)
            {
                return;
            }

            _lastImpactTime = Time.time;
            _bounceCount++;

            Vector3 hitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            // Hanya munculkan echolocation saat objek tertabrak, dan makin kecil setiap kali bertabrakan
            if (EcholocationManager.Instance != null)
            {
                EcholocationManager.Instance.EmitThrowBouncePulse(hitPoint, impactSpeed, _bounceCount);
            }
        }
    }
}
