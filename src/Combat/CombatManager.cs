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

    private void ApplySpCost(Character c, int cost)
    {
        var updated = c with
        {
            Resources = c.Resources with
            {
                CurrentStamina = Math.Max(0, c.Resources.CurrentStamina - cost)
            }
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

        _sideA = _sideA.Select(c =>
        {
            if (c.Resources.CurrentHp <= 0) return c;
            var stats = CombatCalculator.CalculateDerivedStats(c.Attributes, c.Level);
            int spRegen = c.Resources.IsWaiting ? stats.SpRegen * 2 : stats.SpRegen;
            int mpRegen = c.Resources.IsWaiting ? stats.MpRegen * 2 : stats.MpRegen;
            return c with
            {
                Resources = c.Resources with
                {
                    CurrentStamina = Math.Min(stats.MaxStamina, c.Resources.CurrentStamina + spRegen),
                    CurrentMp = Math.Min(stats.MaxMp, c.Resources.CurrentMp + mpRegen),
                    HasActed = false,
                    DefendedLastTurn = c.Resources.Defending,
                    IsWaiting = false
                }
            };
        }).ToList();

        _sideB = _sideB.Select(c =>
        {
            if (c.Resources.CurrentHp <= 0) return c;
            var stats = CombatCalculator.CalculateDerivedStats(c.Attributes, c.Level);
            int spRegen = c.Resources.IsWaiting ? stats.SpRegen * 2 : stats.SpRegen;
            int mpRegen = c.Resources.IsWaiting ? stats.MpRegen * 2 : stats.MpRegen;
            return c with
            {
                Resources = c.Resources with
                {
                    CurrentStamina = Math.Min(stats.MaxStamina, c.Resources.CurrentStamina + spRegen),
                    CurrentMp = Math.Min(stats.MaxMp, c.Resources.CurrentMp + mpRegen),
                    HasActed = false,
                    DefendedLastTurn = c.Resources.Defending,
                    IsWaiting = false
                }
            };
        }).ToList();
    }

    public void SetDefending(Character c)
    {
        var fresh = (_sideA.Concat(_sideB)).First(x => x.Id == c.Id);
        var stats = CombatCalculator.CalculateDerivedStats(fresh.Attributes, fresh.Level);

        if (fresh.Resources.CurrentStamina < stats.BlockSpCost)
        {
            _log.Add($"{fresh.Name} is too exhausted to defend!");
            var target = GetOpponents(fresh).FirstOrDefault(x => x.Resources.CurrentHp > 0);
            if (target != null)
                ExecuteTurn(fresh, target, AttackType.Physical, 0.75f, (float)new Random().NextDouble());
            else
                MarkActed(fresh);
            return;
        }

        var updated = fresh with
        {
            Resources = fresh.Resources with
            {
                Defending = true,
                DefendedLastTurn = true
            }
        };
        UpdateCharacter(updated);
        ApplySpCost(updated, stats.BlockSpCost);

        var afterCost = (_sideA.Concat(_sideB)).First(x => x.Id == c.Id);
        MarkActed(afterCost);
        _log.Add($"{fresh.Name} braces for impact.");
    }

    public void Wait(Character c)
    {
        var fresh = (_sideA.Concat(_sideB)).First(x => x.Id == c.Id);
        var updated = fresh with
        {
            Resources = fresh.Resources with
            {
                IsWaiting = true,
                HasActed = true
            }
        };
        UpdateCharacter(updated);
        _log.Add($"{fresh.Name} waits, conserving energy.");
    }

    public string? CheckWin() =>
        CombatCalculator.CheckWinCondition(_sideA, _sideB);

    public void ExecuteTurn(Character attacker, Character target, AttackType type, float modifiers, float roll)
    {
        var freshAttacker = (_sideA.Concat(_sideB)).First(c => c.Id == attacker.Id);

        // Drop defensive stance when committing to attack
        if (freshAttacker.Resources.Defending)
        {
            freshAttacker = freshAttacker with { Resources = freshAttacker.Resources with { Defending = false } };
            UpdateCharacter(freshAttacker);
        }

        // Deduct SP cost then re-fetch
        var atkStats = CombatCalculator.CalculateDerivedStats(freshAttacker.Attributes, freshAttacker.Level);
        ApplySpCost(freshAttacker, atkStats.AttackSpCost);
        freshAttacker = (_sideA.Concat(_sideB)).First(c => c.Id == attacker.Id);

        var opponents = GetOpponents(freshAttacker);
        if (!opponents.Any(c => c.Id == target.Id))
        {
            _log.Add($"{freshAttacker.Name} tried to target an invalid opponent.");
            MarkActed(freshAttacker);
            return;
        }

        var freshTarget = (_sideA.Concat(_sideB)).First(c => c.Id == target.Id);
        AttackResult attack = CombatCalculator.ResolveAttack(freshAttacker, freshTarget, type, modifiers, roll);

        if (!attack.Hit)
        {
            _log.Add($"{freshAttacker.Name} attacks {freshTarget.Name} - miss!");
            MarkActed(freshAttacker);
            return;
        }

        int finalDamage = attack.Damage;

        if (freshTarget.Resources.Defending)
        {
            BlockResult block = CombatCalculator.ResolveBlock(freshAttacker, freshTarget);
            finalDamage = (int)(attack.Damage * (1f - block.Reduction));
            _log.Add($"{freshTarget.Name} {(block.Tier == BlockTier.Full ? "fully blocks" : "partially blocks")} the attack!");
        }

        ApplyDamage(freshTarget, finalDamage);
        MarkActed(freshAttacker);

        _log.Add($"{freshAttacker.Name} hits {freshTarget.Name} for {finalDamage} damage." +
                 (attack.StatusEffect != null ? $" [{attack.StatusEffect}]" : ""));
    }
}