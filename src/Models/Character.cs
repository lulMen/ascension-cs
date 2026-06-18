namespace Ascension.Models;

public record Character(
    // ── Identity ──────────────────────────
    string Id,
    string Name,
    string BirthClass,
    string? CurrentClass,

    // ── Progression ───────────────────────
    int Tier,
    int Level,
    int Xp,
    int StatPointsAvailable,
    bool ResetUsed,

    // ── Control ───────────────────────────
    bool IsPlayerControlled,

    // ── Combat ────────────────────────────
    Attributes Attributes,
    Resources Resources
);