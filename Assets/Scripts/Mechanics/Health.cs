using System;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Represents the current vital statistics of some game entity.
    /// Supports a heart-based health system where each heart = hpPerHeart HP.
    ///
    /// SETUP:
    ///   - Attached to the Player (or any entity that has health).
    ///   - Set "numberOfHearts" in the Inspector to configure total hearts (default 3).
    ///   - maxHP is automatically calculated as numberOfHearts * hpPerHeart.
    /// </summary>
    public class Health : MonoBehaviour
    {
        // ──────────────────────────────────────────────
        //  Configurable Fields
        // ──────────────────────────────────────────────

        [Header("Heart Configuration")]
        [Tooltip("Number of heart icons displayed in the UI. Changing this single value scales the entire health system.")]
        public int numberOfHearts = 3;

        [Tooltip("HP represented by each heart. 2 = full heart is 2 HP, half heart is 1 HP.")]
        public int hpPerHeart = 2;

        // ──────────────────────────────────────────────
        //  Computed Properties
        // ──────────────────────────────────────────────

        /// <summary>
        /// The maximum hit points for the entity.
        /// Automatically calculated: numberOfHearts * hpPerHeart.
        /// </summary>
        public int maxHP => numberOfHearts * hpPerHeart;

        /// <summary>
        /// Current HP. Exposed as read-only for the HealthBar UI.
        /// </summary>
        public int CurrentHP => currentHP;

        /// <summary>
        /// Indicates if the entity should be considered 'alive'.
        /// </summary>
        public bool IsAlive => currentHP > 0;

        /// <summary>
        /// Invincibility window flag — prevents stacking damage on the same frame.
        /// </summary>
        [HideInInspector]
        public bool isInvincible = false;

        [Header("Invincibility")]
        [Tooltip("Duration in seconds the player is invincible after taking damage.")]
        public float invincibilityDuration = 1f;

        private float invincibilityTimer = 0f;

        int currentHP;

        // ──────────────────────────────────────────────
        //  Lifecycle
        // ──────────────────────────────────────────────

        void Awake()
        {
            currentHP = maxHP;
        }

        void Update()
        {
            // Tick down invincibility timer
            if (isInvincible)
            {
                invincibilityTimer -= Time.deltaTime;
                if (invincibilityTimer <= 0f)
                {
                    isInvincible = false;
                }
            }
        }

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────

        /// <summary>
        /// Deals a specified amount of damage (default 1 = half a heart).
        /// Respects the invincibility window.
        /// Fires HealthIsZero when HP reaches 0.
        /// </summary>
        public void TakeDamage(int amount = 1)
        {
            if (!IsAlive) return;
            if (isInvincible) return;

            currentHP = Mathf.Clamp(currentHP - amount, 0, maxHP);

            // Start invincibility window
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;

            if (currentHP == 0)
            {
                var ev = Schedule<HealthIsZero>();
                ev.health = this;
            }
        }

        /// <summary>
        /// Heal a specified amount of HP.
        /// </summary>
        public void Heal(int amount = 1)
        {
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        }

        /// <summary>
        /// Restore health to full. Useful on respawn.
        /// </summary>
        public void ResetHealth()
        {
            currentHP = maxHP;
            isInvincible = false;
            invincibilityTimer = 0f;
        }

        /// <summary>
        /// Increment the HP of the entity by 1.
        /// Kept for backward compatibility with PlayerSpawn.
        /// </summary>
        public void Increment()
        {
            Heal(1);
        }

        /// <summary>
        /// Decrement the HP of the entity by 1.
        /// Kept for backward compatibility with existing events.
        /// Will trigger a HealthIsZero event when current HP reaches 0.
        /// </summary>
        public void Decrement()
        {
            currentHP = Mathf.Clamp(currentHP - 1, 0, maxHP);
            if (currentHP == 0)
            {
                var ev = Schedule<HealthIsZero>();
                ev.health = this;
            }
        }

        /// <summary>
        /// Decrement the HP of the entity until HP reaches 0.
        /// </summary>
        public void Die()
        {
            while (currentHP > 0) Decrement();
        }

        /// <summary>
        /// Placeholder for future death / game-over logic.
        /// Called when all hearts are empty.
        /// Override or extend this method to implement:
        ///   - Game Over screen
        ///   - Respawn with limited lives
        ///   - Score penalty
        ///   - Animation triggers
        /// </summary>
        public virtual void OnPlayerDeath()
        {
            Debug.Log("[Health] Player has died! All hearts empty. Implement game-over logic here.");
            // TODO: Add your death / game-over logic
        }
    }
}
