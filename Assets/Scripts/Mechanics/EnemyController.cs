using System.Collections;
using System.Collections.Generic;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour
    {
        public PatrolPath path;
        public AudioClip ouch;

        [Header("Contact Damage")]
        [Tooltip("Seconds between each contact damage tick while the player stays touching this enemy.")]
        public float contactDamageInterval = 1f;

        [Tooltip("HP removed per contact damage tick (1 = half a heart).")]
        public int contactDamageAmount = 1;

        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D _collider;
        internal AudioSource _audio;
        SpriteRenderer spriteRenderer;

        /// <summary>
        /// Timer tracking time since last contact damage tick.
        /// </summary>
        private float contactDamageTimer = 0f;

        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            control = GetComponent<AnimationController>();
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnemyCollision>();
                ev.player = player;
                ev.enemy = this;

                // Reset timer so the first stay-tick doesn't fire immediately
                contactDamageTimer = 0f;
            }
        }

        /// <summary>
        /// Deals continuous contact damage while the player remains touching this enemy.
        /// Damage is applied every contactDamageInterval seconds (default 1s).
        /// </summary>
        void OnCollisionStay2D(Collision2D collision)
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player == null) return;

            contactDamageTimer += Time.deltaTime;

            if (contactDamageTimer >= contactDamageInterval)
            {
                contactDamageTimer = 0f;

                var health = player.GetComponent<Health>();
                if (health != null && health.IsAlive)
                {
                    health.TakeDamage(contactDamageAmount);
                }
            }
        }

        /// <summary>
        /// Reset the contact damage timer when the player separates from this enemy.
        /// </summary>
        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<PlayerController>() != null)
            {
                contactDamageTimer = 0f;
            }
        }

        void Update()
        {
            if (path != null)
            {
                if (mover == null) mover = path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

    }
}