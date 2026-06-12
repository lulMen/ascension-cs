namespace Ascension.Models;

public record Character(
    string Id,
    string Name,
    Attributes Attributes,
    Resources Resources
);