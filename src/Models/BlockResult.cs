namespace Ascension.Models;

public enum BlockTier
{
    Full,
    Partial
}

public record BlockResult(
    BlockTier Tier,
    float Reduction
);