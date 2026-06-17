namespace Ascension.Models;

public record Character(
    string Id,
    string Name,
    string BirthClass,
    string? CurrentClass,
    int Tier,
    bool ResetUsed,
    Attributes Attributes,
    Resources Resources
);