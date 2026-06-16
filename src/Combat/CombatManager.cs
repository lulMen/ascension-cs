using Ascension.Models;

namespace Ascension.Combat;

public class CombatManager
{
    // ── State ─────────────────────────────────────────────────
    private List<Character> _sideA;
    private List<Character> _sideB;
    private int _round;
    private List<string> _log;

    public IReadOnlyList<Character> SideA => _sideA;
    public IReadOnlyList<Character> SideB => _sideB;
    public int Round => _round;
    public IReadOnlyList<string> Log => _log;

    public CombatManager(List<Character> sideA, List<Character> sideB)
    {
        _sideA = sideA;
        _sideB = sideB;
        _round = 1;
        _log = new List<string>();
    }

    // ── Private Helpers ───────────────────────────────────────
    private bool IsOnSideA(Character c) =>
        _sideA.Any(a => a.Id == c.Id);

    private List<Character> GetOpponents(Character c) =>
        IsOnSideA(c) ? _sideB : _sideA;

    private void UpdateCharacter(Character updated)
    {
        if (IsOnSideA(updated))
            _sideA = _sideA.Select(c => c.Id == updated.Id ? updated : c).ToList();
        else
            _sideB = _sideB.Select(c => c.Id == updated.Id ? updated : c).ToList();
    }

    private void ApplyDamage(Character target, int damage)
    {
        var updated = target with
        {
            Resources = target.Resources with
            {
                CurrentHp = target.Resources.CurrentHp - damage
            }
        };
        UpdateCharacter(updated);
    }

    private void MarkActed(Character c)
    {
        var updated = c with
        {
            Resources = c.Resources with { HasActed = true }
        };
        UpdateCharacter(updated);
    }

    // ── Public API ────────────────────────────────────────────
    public Character? GetNextActor()
    {
        var all = _sideA.Concat(_sideB).ToList();
        return CombatCalculator.DetermineTurnOrder(all).FirstOrDefault();
    }

    public bool IsRoundOver()
    {
        var all = _sideA.Concat(_sideB).ToList();
        return all.Where(c => c.Resources.CurrentHp > 0).All(c => c.Resources.HasActed);
    }

    public void NextRound()
    {
        _round++;
        _sideA = _sideA.Select(c => c with
        {
            Resources = c.Resources with { HasActed = false }
        }).ToList();
        _sideB = _sideB.Select(c => c with
        {
            Resources = c.Resources with { HasActed = false }
        }).ToList();
    }

    public void SetDefending(Character c)
    {
        var updated = c with
        {
            Resources = c.Resources with { Defending = true }
        };
        UpdateCharacter(updated);
        MarkActed(c);
        _log.Add($"{c.Name} braces for impact.");
    }

    public string? CheckWin() =>
        CombatCalculator.CheckWinCondition(_sideA, _sideB);

    public void ExecuteTurn(Character attacker, Character target, AttackType type, float modifiers, float roll)
    {
        var opponents = GetOpponents(attacker);
        if (!opponents.Any(c => c.Id == target.Id))
        {
            _log.Add($"{attacker.Name} tried to target an invalid opponent.");
            return;
        }

        // refresh target from state
        var freshTarget = (_sideA.Concat(_sideB)).First(c => c.Id == target.Id);

        AttackResult attack = CombatCalculator.ResolveAttack(attacker, freshTarget, type, modifiers, roll);

        if (!attack.Hit)
        {
            _log.Add($"{attacker.Name} attacks {freshTarget.Name} - miss!");
            MarkActed(attacker);
            return;
        }

        int finalDamage = attack.Damage;

        if (freshTarget.Resources.Defending)
        {
            BlockResult block = CombatCalculator.ResolveBlock(attacker, freshTarget);
            finalDamage = (int)(attack.Damage * (1f - block.Reduction));
            _log.Add($"{freshTarget.Name} {(block.Tier == BlockTier.Full ? "fullyblocks" : "partially blocks")} the attack!");
        }

        ApplyDamage(freshTarget, finalDamage);
        MarkActed(attacker);

        _log.Add($"{attacker.Name} hits {freshTarget.Name} for {finalDamage} damage." +
        (attack.StatusEffect != null ? $" [{attack.StatusEffect}]" : ""));
    }
}