using UnityEngine;
using UnityEngine.UI;
using Platformer.Mechanics;

/// <summary>
/// Manages the heart-based health bar UI using a two-image overlay system.
/// Attach this script to the "Healthbar" GameObject (parent of HealthbarTotal and HealthbarCurrent).
///
/// HOW IT WORKS:
///   - HealthbarTotal: Background image showing empty heart SLOTS (the outline/dark hearts).
///     Its fillAmount is set to show the max number of hearts (e.g., 0.3 = 3 hearts out of 10 in the sprite strip).
///   - HealthbarCurrent: Foreground image overlaid on top, showing FILLED hearts.
///     Its fillAmount is scaled proportionally based on current HP.
///
/// SETUP:
///   1. This script is on the "Healthbar" GameObject under UICanvas.
///   2. Drag the Player GameObject into the "playerHealth" field.
///   3. Drag HealthbarTotal Image into the "healthbarTotal" field.
///   4. Drag HealthbarCurrent Image into the "healthbarCurrent" field.
///   5. Set HealthbarTotal's fill amount in the Inspector to match your number of hearts
///      (e.g., 0.3 for 3 hearts if the sprite strip contains 10 hearts).
/// </summary>
public class HealthBar : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector Fields
    // ──────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Drag the Player GameObject here. The script will read its Health component.")]
    public Health playerHealth;

    [Tooltip("The background Image showing empty heart slots (max health outline).")]
    public Image healthbarTotal;

    [Tooltip("The foreground Image showing filled hearts (current health).")]
    public Image healthbarCurrent;

    // ──────────────────────────────────────────────
    //  Private State
    // ──────────────────────────────────────────────

    /// <summary>
    /// The fill amount that represents full health.
    /// Captured from HealthbarTotal at Start (e.g., 0.3 for 3 hearts).
    /// </summary>
    private float maxFillAmount;

    void Start()
    {
        // Capture the fill amount that represents full (max) health.
        // HealthbarTotal's fill is set in the Inspector to show all heart slots.
        if (healthbarTotal != null)
        {
            maxFillAmount = healthbarTotal.fillAmount;
        }

        // Initial UI sync
        UpdateHearts();
    }

    void Update()
    {
        UpdateHearts();
    }

    // ──────────────────────────────────────────────
    //  Core UI Update
    // ──────────────────────────────────────────────

    /// <summary>
    /// Updates HealthbarCurrent's fillAmount based on the player's current HP.
    ///
    /// Formula:
    ///   currentFill = (currentHP / maxHP) * maxFillAmount
    ///
    /// Example with 3 hearts (6 HP), maxFillAmount = 0.3:
    ///   6/6 HP → 0.3  (3 full hearts)
    ///   5/6 HP → 0.25 (2.5 hearts — last heart is half)
    ///   4/6 HP → 0.2  (2 full hearts)
    ///   3/6 HP → 0.15 (1.5 hearts)
    ///   1/6 HP → 0.05 (half a heart)
    ///   0/6 HP → 0.0  (empty — dead)
    /// </summary>
    public void UpdateHearts()
    {
        if (playerHealth == null || healthbarCurrent == null) return;

        float hpFraction = (float)playerHealth.CurrentHP / playerHealth.maxHP;
        healthbarCurrent.fillAmount = hpFraction * maxFillAmount;
    }
}
