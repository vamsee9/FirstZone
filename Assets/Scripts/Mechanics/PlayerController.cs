using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        // ──────────────────────────────────────────────
        //  Multi-Jump Configuration
        // ──────────────────────────────────────────────

        /// <summary>
        /// Total number of jumps allowed before the player must land.
        /// Set to 2 for double-jump, 3 for triple-jump, etc.
        /// Adjustable from the Inspector.
        /// </summary>
        [SerializeField] public int maxJumps = 2;

        /// <summary>
        /// Force multiplier applied to mid-air jumps (2nd jump onward).
        /// Values less than 1 make air jumps weaker; greater than 1 makes them stronger.
        /// </summary>
        [SerializeField] public float airJumpForceMultiplier = 0.85f;

        /// <summary>
        /// Tracks how many jumps the player has performed since last touching ground.
        /// Resets to 0 on landing.
        /// </summary>
        private int jumpCount = 0;

        // ──────────────────────────────────────────────

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/ public Collider2D collider2d;
        /*internal new*/ public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction m_MoveAction;
        private InputAction m_JumpAction;

        public Bounds Bounds => collider2d.bounds;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");
            
            m_MoveAction.Enable();
            m_JumpAction.Enable();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = m_MoveAction.ReadValue<Vector2>().x;

                // ── Desktop input: Spacebar / Gamepad via InputSystem ──
                if (m_JumpAction.WasPressedThisFrame())
                {
                    TryJump();
                }
                else if (m_JumpAction.WasReleasedThisFrame())
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        // ──────────────────────────────────────────────
        //  Mobile UI Button Hook
        // ──────────────────────────────────────────────
        //
        //  HOW TO WIRE UP IN UNITY:
        //  1. Select your "Up Arrow" / Jump UI Button in the Hierarchy.
        //  2. In the Inspector, find the Button component's "On Click ()" list.
        //  3. Drag the Player GameObject into the object field.
        //  4. From the function dropdown, choose:
        //         PlayerController -> OnJumpButtonPressed()
        //
        //  This method is called once per tap, which mirrors GetKeyDown behavior.
        // ──────────────────────────────────────────────

        /// <summary>
        /// Public method designed to be called from a Mobile UI Button's OnClick event.
        /// Triggers a jump if the player has remaining jumps and controls are enabled.
        /// </summary>
        public void OnJumpButtonPressed()
        {
            if (!controlEnabled) return;
            TryJump();
        }

        /// <summary>
        /// Central jump-request method used by both Desktop and Mobile input paths.
        /// Checks the jump counter against maxJumps and initiates the jump if allowed.
        /// </summary>
        private void TryJump()
        {
            if (jumpCount < maxJumps)
            {
                jumpState = JumpState.PrepareToJump;
            }
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump)
            {
                if (IsGrounded)
                {
                    // ── First jump from the ground ──
                    velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                    jumpCount = 1;
                }
                else if (jumpCount > 0 && jumpCount < maxJumps)
                {
                    // ── Mid-air jump (double-jump, triple-jump, etc.) ──
                    // Cancel any existing downward velocity so the air jump feels crisp,
                    // then apply the jump force scaled by airJumpForceMultiplier.
                    velocity.y = jumpTakeOffSpeed * model.jumpModifier * airJumpForceMultiplier;
                    jumpCount++;
                }

                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            // ── Reset jump counter on landing ──
            if (IsGrounded && jumpState == JumpState.Grounded)
            {
                jumpCount = 0;
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}