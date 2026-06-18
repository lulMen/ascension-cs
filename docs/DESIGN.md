# Ascension — Design Document

> Last updated: June 2026
> This document captures design decisions, future implementations, and mechanics
> that cannot be gleaned from reading the codebase alone. Written for both
> developer reference and Claude session continuity.

---

## 1. World Concept

### Setting

The world is an amalgamation of the world's mythological, religious, and folklore
traditions. Enemies, bosses, quests, and lore draw from all cultures simultaneously.
Greek, Norse, Egyptian, Chinese, Japanese, Celtic, Mesoamerican, Slavic, and more
coexist naturally. A player may encounter a Pegasus on the same floor as a Jiangshi
or a Banshee. No single mythology dominates.

### The Tower — Axis Mundi

The central structure of the game is Axis Mundi — the world axis. In mythology,
nearly every culture has a version of this concept:

- Norse: Yggdrasil
- Hindu: Mount Meru
- Greek: Mount Olympus
- Egyptian: Benben / Pyramid
- Aztec: Pyramid of the Sun
- Babylon: Ziggurat
- Chinese: Kunlun Mountain

Axis Mundi is the convergence point of all mythologies. The higher a player climbs,
the closer they approach the divine. The tower has 100 floors. Floor 1 has rats.
Floor 100 has something that makes gods nervous.

---

## 2. Core Game Loop

### Current Implementation (Phase 1 — Console Dungeon Crawler)

```
Town (Base) → Floor Entry → 1-3 Encounters → Floor Complete → Loot/Rest/Save → Next Floor
```

### Future Phases

- Phase 2: 2D RPG (Godot, top-down overworld)
- Phase 3: 2D MMO (Godot + server backend)
- Phase 4 (deferred indefinitely): 3D

Each phase reuses the combat engine and class system built in Phase 1.

### Floor Structure

```
Floor 1-4   → Standard encounters (1-3 fights)
Floor 5     → Elite encounter
Floor 6-9   → Standard encounters
Floor 10    → Boss + Checkpoint (save point unlocked)
(pattern repeats every 10 floors)
```

### Floor Themes (Future — Phase 2)

```
Floors 1-10   → Plains
Floors 11-20  → Forest
Floors 21-30  → Mountain
Floors 31-40  → Ruins
Floors 41-50  → Underground Caverns
Floors 51-60  → Ancient Temple
Floors 61-70  → Frozen Wastes
Floors 71-80  → Volcanic Depths
Floors 81-90  → Celestial Approach
Floors 91-100 → The Apex
```

---

## 3. Mob Scaling

### Philosophy

- Early game (1-15): Mobs roughly equal player stats. Skill matters.
- Mid game (16-50): Mobs pull 20-50% ahead. Gear starts to matter.
- Late game (51-100): Mobs pull 100%+ ahead. Gear + abilities required.

This accounts for the player gaining equipment, items, and abilities that the raw
stat comparison doesn't capture. Without this scaling, end-game mobs would be
steamrolled by well-equipped players.

### Mob Stat Pool Formula

```
mobStatPool(level) = 20 + (level - 1)^1.3 × 1.82
```

| Level | Mob stats | Player raw stats | Mob advantage |
| ----- | --------- | ---------------- | ------------- |
| 1     | 20        | 20               | Even          |
| 10    | 48        | 52               | Even          |
| 20    | 97        | 87               | +12%          |
| 50    | 294       | 192              | +53%          |
| 100   | 734       | 367              | +100%         |

All scaling values are tunable in `TowerConfig.cs` (to be built).

### Enemy Type Multipliers

```
Standard mob : 1.0x stat pool
Elite        : 1.3x - 1.5x stat pool
Boss         : 1.8x - 2.3x stat pool
```

Elites and bosses also have abilities/skills beyond standard mobs (future).

### Floor-Level Parity

```
Floor N → Level N standard mobs
Elite mob  → approximately Level N+2 in effective strength
Boss mob   → approximately Level N+3 to N+5 in effective strength
```

---

## 4. XP and Progression

### XP Per Kill

```
Standard mob : mob level × 10 XP
Elite        : mob level × 15 XP
Boss         : mob level × 25 XP
```

### XP to Level Up

```
XP required = currentLevel × 150
```

| Level | XP needed | Approx floors to level |
| ----- | --------- | ---------------------- |
| 1→2   | 150       | 3 floors               |
| 5→6   | 750       | 3 floors               |
| 10→11 | 1500      | 3 floors               |
| 50→51 | 7500      | 3 floors               |

Early floors are shorter (fewer fights) so early levels feel slightly harder to earn.
This is intentional — the jump from Level 1 to 2 should feel like an achievement.

### Level Cap

Maximum level: 100

### Level Up Timing

- Phase 1 (console): Level up and stat allocation at floor completion screen
- Phase 2+ (2D): Level up after defeating a mob; stat allocation when player chooses

### Stat Points Per Level

```
Standard level up  : +3 points
Every 5th level    : +5 points  (5, 15, 25, 35, 45, 55, 65, 75, 85, 95)
Every 10th level   : +8 points  (10, 20, 30, 40, 50, 60, 70, 80, 90)
Level 50 and 100   : +15 points
Level 100 (final)  : +25 points (supersedes all)
```

Priority order (higher always applies, not stacked):

```
Level 100 → +25 (final reward, once)
Level 50  → +15
Mult of 10 → +8
Mult of 5  → +5
All others → +3
```

### Checkpoint Bonuses (Every 10th Floor Boss)

```
All checkpoints (10, 20, 30...90):
├── Stat point bonus per above table
├── Full HP/SP/MP restore (free rest)
└── Save point unlocked (respawn here on death)

Floors 25, 50, 75 additionally:
└── +1 bonus point into class primary stat

Floor 100:
├── Transcendence class unlocked
├── +25 stat points
└── Title: "Apex Climber" (future cosmetic system)
```

### Max Stat Points Available (Total, Level 1-100)

Rough calculation: approximately 450-500 total stat points over a full playthrough,
including checkpoint bonuses. Exact value to be confirmed during tuning.

---

## 5. Death Mechanics

### On Death

```
Lose 20% of total accumulated XP
Respawn at last cleared checkpoint floor
If no checkpoint cleared → respawn at Floor 1
Keep: character, level, attributes, abilities, gear
Lose: floor progress since last checkpoint
```

### Level Regression

If XP loss drops you below the current level threshold, you regress in level.
You do NOT lose stat points or abilities gained from the lost level.

### Regression Hard Floors (Cannot Drop Below)

```
Adventurer (Tier 0)  : Level 1
Tier 1 class         : Level 15
Tier 2 class         : Level 40
Tier 3 class         : Level 70
Transcendence        : Level 100
```

---

## 6. Class System

### Birth Class

All characters begin as **Adventurer** (Level 1-15).

- No combat class identity yet
- Access to Tier 0 abilities only (general, not class-locked)
- Exploration phase: player discovers their playstyle

### First Job Change — Level 15

At Level 15, a class quest is triggered. The quest type offered is influenced
by how the player distributed stats and used abilities during Levels 1-15
(tracked silently by the game).

```
Mostly physical ability usage  → Warrior or Rogue quest offered
Mostly arcane ability usage    → Mage or Cleric quest offered
Balanced usage                 → All four offered (true hybrid path)
```

The final class choice is determined by the nature of the quest itself —
the challenge reveals what kind of fighter you are.

### Attribute Reset at Job Change

- One full reset of ALL attributes is offered at Tier 1 job change (optional)
- This is the ONLY reset in the game, ever
- After this point, attributes are permanent until new tier points are earned
- Each tier job change grants fresh points for that tier only — no reset

### Starting Classes (200 base points, 15 bonus at creation)

| Class   | STR | AGI | VIT | INT | WIL | Identity             |
| ------- | --- | --- | --- | --- | --- | -------------------- |
| Warrior | 60  | 40  | 55  | 20  | 25  | Frontline, physical  |
| Mage    | 15  | 30  | 25  | 75  | 55  | Pure arcane, fragile |
| Rogue   | 35  | 70  | 30  | 35  | 30  | Speed, precision     |
| Cleric  | 30  | 25  | 45  | 40  | 60  | Spirit, support      |

### Evolution Paths (Job Change at Level 15)

**Warrior:**
| Class | Type | Stat emphasis |
|----------|---------------|-------------------|
| Sentinel | Tank | VIT↑↑ WIL↑ |
| Berserker| DPS | STR↑↑ AGI↑ |
| Champion | Hybrid | STR↑ VIT↑ |
| Warlord | ★ Hidden | Sentinel + Berserker simultaneously |

**Mage:**
| Class | Type | Stat emphasis |
|--------------|--------------|-------------------|
| Elementalist | Pure attack | INT↑↑ |
| Enchanter | Control | WIL↑↑ INT↑ |
| Arcanist | Hybrid | INT↑ WIL↑ |
| Archmage | ★ Hidden | Elementalist + Enchanter simultaneously |

**Rogue:**
| Class | Type | Stat emphasis |
|----------|--------------|-------------------|
| Assassin | Melee DPS | STR↑ AGI↑↑ |
| Marksman | Ranged DPS | AGI↑↑ INT↑ |
| Shadow | Hybrid | AGI↑↑ WIL↑ |
| Phantom | ★ Hidden | All three — no range restriction |

**Cleric:**
| Class | Type | Stat emphasis |
|---------|--------------------|-------------------|
| Priest | Heal primary | WIL↑↑ VIT↑ |
| Paladin | Tank primary | VIT↑↑ STR↑ WIL↑|
| Templar | Solo (tank + heal) | VIT↑ WIL↑ balanced|
| Saint | ★ Hidden | All three simultaneously |

### Hidden Class Unlock Conditions

Hidden classes require very specific playstyle conditions during Levels 1-15.
The hidden class quest is noticeably harder than standard class quests.
Failing a hidden class quest has consequences (TBD — storyboard later).
Specific unlock conditions are TBD.

### Tier Progression (Future)

```
Tier 0: Adventurer         (Level 1-15)
Tier 1: First job class    (Level 15-40)   → Job change at Level 15
Tier 2: Evolution          (Level 40-70)   → Job change at Level 40, resets Tier 1 points
Tier 3: Second evolution   (Level 70-100)  → Job change at Level 70, resets Tier 2 points
Transcendence              (Level 100)     → Resets Tier 3 points
```

### Realm System

```
Mortal Realm    → Level 1-15   (Adventurer)
Awakened Realm  → Level 16-40  (First job)
Exalted Realm   → Level 41-70  (Second job)
Heroic Realm    → Level 71-100 (Third job)
Legendary Realm → Level 100+   (Transcendence)
```

NPCs react to realm, not level. A peasant doesn't know your level — they feel your realm.

---

## 7. Ability System (Design Phase — Not Yet Implemented)

### Tiers

```
Tier 0 abilities: General, available to all classes, Levels 1-15
Tier 1 abilities: Class-specific, unlocked at first job change
Tier 2 abilities: Evolution-specific, unlocked at second job change
Tier 3 abilities: Advanced evolution abilities
Tier 4 abilities: Transcendence abilities
```

### Cost Philosophy

- All ability costs are derived from attributes, not hardcoded flat numbers
- Physical abilities cost SP. SP cost formula:
  ```
  Cost = max(1, baseCost + level/10 - Scale(relevantAttribute, 0.5f))
  ```
  Level scaling keeps costs meaningful at all tiers.
  Attribute investment reduces cost — high STR fighter attacks more efficiently.
- Magic abilities cost MP. Similar formula but softer level scaling (level/20).
- Skills and spells increase in cost with their tier.

### Stamina (SP) Action Costs (Derived — See CombatCalculator.cs)

```
Attack  : derived from STR
Defend  : derived from VIT/STR blend
Dodge   : derived from AGI (not yet implemented)
Wait    : 0 cost, grants double SP/MP regen this round
```

Exhausted fighters (0 SP) can still attack at 0.75x damage modifier.
Cannot block at 0 SP — forced to attack or wait.

### Intended Ability Categories (Tier 0 examples)

| Category     | Examples                   | Signal        |
| ------------ | -------------------------- | ------------- |
| Basic Combat | Power Strike, Quick Step   | Universal     |
| Physical     | Rend, Rush, Parry Counter  | Warrior/Rogue |
| Arcane       | Mana Bolt, Ward            | Mage/Cleric   |
| Survival     | Second Wind, Steady Breath | Universal     |
| Utility      | Detect, Inspect, Forage    | Universal     |

The Inspect ability (future) will reveal enemy stats — currently hidden in the UI.

---

## 8. Combat System Notes

### Architecture

- Decide layer: `CombatCalculator.cs` — pure static functions, no state
- Apply layer: `CombatManager.cs` — stateful, mutates via immutable record `with` expressions
- All records are immutable; always re-fetch after `UpdateCharacter` calls

### Attribute → Derived Stat Formulas

```
Scale(attribute, c) = (int)(c × √attribute)
Blend(primary, secondary) = primary × 0.65 + secondary × 0.35

MaxHp           = Scale(VIT, 5)
MaxStamina      = Scale(VIT, 3)
MaxMp           = Scale(INT, 3)
PhysicalDamage  = Scale(STR, 2)
MagicalDamage   = Scale(INT, 2)
PhysicalDefense = Scale(VIT, 1.5)
MagicalDefense  = Scale(WIL, 1.5)
Initiative      = Scale(AGI, 2)
Evasion         = Scale(AGI, 2)
Accuracy        = Scale(AGI, 2)
BlockSpeed      = Scale(Blend(AGI, STR), 2)
BlockPower      = Scale(Blend(VIT, STR), 2)
SpRegen         = max(1, Scale(Blend(VIT, AGI), 1.2))
MpRegen         = max(1, Scale(Blend(WIL, INT), 1.2))
AttackSpCost    = max(1, 4 + level/10 - Scale(STR, 0.5))
BlockSpCost     = max(1, 5 + level/10 - Scale(Blend(VIT,STR), 0.5))
DodgeSpCost     = max(1, 3 + level/10 - Scale(AGI, 0.5))
```

### Hit Chance Formula

```
hitChance = Accuracy / (Accuracy + Evasion) + 0.25 (BASIC_ATTACK_ACCURACY)
hit = roll < hitChance
```

### Damage Reduction Formula

```
reduction = DEFENSE_K / (DEFENSE_K + defense)   (DEFENSE_K = 30)
damage = (int)(rawDamage × modifiers × reduction)
```

No minimum damage — 0 damage is valid and realistic (high-level player vs weak mob).

### Block System

```
fullBlock = BlockSpeed × 1.5 (DEFEND_REACTION) >= attacker Initiative
Full block    → 100% damage reduction
Partial block → 50% damage reduction
```

Block is deterministic (no dice). Attack hit/miss uses dice.
Blocking costs BlockSpCost SP per round.
At 0 SP, cannot choose to defend — forced to attack at 0.75x or wait.

### HP Recovery in Combat

No passive HP regen in combat. HP only recovers via:

- Cleric abilities / healing skills
- Items (potions — future)
- Post-combat rest / floor transition

### Turn Order (Option B — Current)

Turn order recalculated after each action based on AGI-derived Initiative.
Characters with HasActed = true are excluded from the queue.
Round ends when all living fighters have acted.

Future (Option C — Phase 2 with GUI):
Action gauge system — characters act based on time-to-next-action counter.
Effectively real-time with pause. Replaces Option B when GUI is implemented.

### AI Decision Tree (Current — Rudimentary)

```
1. SP == 0 AND HP > 40% → Wait (recover)
2. Target HP < 15%      → Attack (finish them)
3. HP < 30% AND defended last turn → Attack (no turtling)
4. HP < 30% AND slower than target → Defend (survival)
5. HP < 30% AND faster than target → Attack (aggression)
6. Default              → Attack
```

### Enemy HP Display

Enemy HP shown as condition label + bar graphic (no exact numbers visible to player).
Conditions: Unscathed / Scratched / Bloodied / Wounded / Critical / Near Death
Exact HP only visible to player-controlled characters.
Inspect ability (future) will reveal enemy stats.

### IsPlayerControlled Flag

Lives on `Character` model. Default false for all NPCs and enemies.
True for player characters. This flag is intentionally on the Character model
because future abilities may temporarily flip it (Dominate, Puppet, Charm effects).

---

## 9. Status Effects (Planned — Not Yet Implemented)

`AttackResult` already carries a nullable `StatusEffect` string field.
Status effects will be applied here. Design TBD during ability storyboarding.
Examples: Bleed, Poison, Burn, Stun, Slow, Charm, Blind, Freeze.

---

## 10. Stamina Philosophy

Physical actions cost SP. SP represents physical exertion.

- STR affects efficiency of physical actions (reduces cost, not increases pool)
- AGI affects movement economy (reduces dodge cost, improves regen)
- VIT is primary for MaxStamina and regen

The two-force cost system:

```
Final SP Cost = max(1, baseCost + level/10 - attributeReduction)
```

Level scaling pushes cost up. Attribute investment pulls cost down.
At high levels, cost grows but so does MaxSP — ratio stays meaningful.

Wait action: 0 SP cost, doubles both SP and MP regen for that round.
Strategic use: Block → Wait → Attack rhythm is a core tactical pattern.

---

## 11. Save System (Planned — Not Yet Implemented)

Planned persistence:

- Save at checkpoint floors (every 10th floor boss cleared)
- Save file: JSON via System.Text.Json (built into .NET, no install needed)
- Save data: character, level, XP, attributes, abilities, current checkpoint floor

Future (Phase 2+ with DB):

- PostgreSQL + EF Core for MMO scale
- Enemy data files (Vermin.cs, Undead.cs etc.) become DB seed data

---

## 12. Equipment and Loot (Planned — Not Yet Implemented)

Loot tables: TBD during storyboarding.
Equipment will affect derived stats (bonus to existing stats or flat additions).
This is a key reason mob scaling pulls ahead of raw player stats at high levels —
gear bridges the gap the numbers alone cannot.

---

## 13. Future Platform Path

```
Phase 1 (current): C# console dungeon crawler
Phase 2: Godot 2D top-down RPG
    - Action combat (Option C — time gauge replaces turn order)
    - Overworld exploration
    - Quest system
    - Full story/dialogue
Phase 3: Godot 2D MMO
    - Server backend
    - Multiplayer
    - Guild/party systems
Phase 4 (indefinitely deferred): 3D
```

Spectre.Console UI (current) serves as UI prototype.
Layout decisions made here translate to Godot node structure later.

---

## 14. Planned Tools / Libraries

| Tool                 | Purpose                         | Status                            |
| -------------------- | ------------------------------- | --------------------------------- |
| Spectre.Console      | Terminal UI                     | Active                            |
| System.Text.Json     | Save files                      | Planned                           |
| xUnit                | Unit tests for CombatCalculator | Planned (after combat stabilizes) |
| SQLite + Dapper      | Local DB for Phase 2            | Future                            |
| PostgreSQL + EF Core | MMO backend                     | Phase 3                           |
| Godot (C# bindings)  | 2D game engine                  | Phase 2                           |

---

## 15. Naming Conventions and Repo Structure

```
src/
├── Models/          ← Pure data records (no logic)
├── Combat/          ← CombatCalculator (Decide) + CombatManager (Apply)
├── Data/
│   ├── Enemies/     ← One file per enemy category (Vermin, Undead, Beasts...)
│   └── Fighters.cs  ← Demo characters (Kael, Veyra) — temporary
├── UI/              ← All Spectre.Console display logic
└── Program.cs       ← Entry point only (3 lines)

docs/
└── DESIGN.md        ← This file
```

Rule: `Models/` never imports from `Combat/`. Display logic lives in `UI/` only.
All tunable constants live in dedicated config files, not scattered in logic.

---

_This document should be updated whenever a significant design decision is made._
_Keep it as the source of truth for intent — the code captures implementation,_
_this captures why._
