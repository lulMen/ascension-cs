# Ascension — Design Document

> Last updated: June 2026
> This document captures design decisions, future implementations, and mechanics
> that cannot be gleaned from reading the codebase alone. Written for both
> developer reference and Claude session continuity.

---

## 0. Design Reference

### Primary Reference: Reincarnation of the Strongest Sword God (RSSG)

RSSG is used as a primary reference framework for game mechanics and progression design.
We adapt its systems to our scale and world — borrowing the shape and philosophy,
not the raw numbers or setting.

What we borrow from RSSG:

- Milestone-based progression (equipment thresholds every 5 levels, flat stat bonuses at 10)
- Equipment carrying late-game power growth (not just raw stats)
- Tier promotion system structure
- Ability tier scaling with job changes
- Dungeon/boss design philosophy
- Equipment rarity tiers as power breakpoints

What we do NOT copy:

- Raw numbers (RSSG scales to 70,000+ attributes; ours stays in the hundreds)
- Story and setting (our world is mythological amalgam, not VRMMORPG)
- Exact ability designs (storyboarded separately)
- Level cap (RSSG goes 200+; our tower caps at 100 floors, level cap at 200)

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

Clearing the tower (Floor 100) is not the end of the game — it is the end of Phase 1
content. Levels continue to 200. The tower was the beginning, not the destination.

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

- Early game (1-20): Mobs roughly equal player stats. Skill matters.
- Mid game (21-90): Mobs pull 20-80% ahead. Gear starts to matter.
- Late game (91-200): Mobs pull 100%+ ahead. Gear + abilities required.

### Mob Stat Pool Formula

```
mobStatPool(level) = 20 + (level - 1)^1.3 × 1.82
```

All constants live in TowerConfig.cs:

- MobScaleBase = 20.0
- MobScalePow = 1.3
- MobScaleFactor = 1.82

| Level | Mob stats | Player raw stats | Mob advantage |
| ----- | --------- | ---------------- | ------------- |
| 1     | 20        | 20               | Even          |
| 20    | 103       | 91               | +13%          |
| 50    | 307       | 209              | +47%          |
| 100   | 735       | 411              | +79%          |
| 200   | ~2,100    | ~750             | +180%         |

### Enemy Type Multipliers

```
Standard mob : 1.0x stat pool
Elite        : 1.3x - 1.5x stat pool  (random within range)
Boss         : 1.8x - 2.3x stat pool  (random within range)
```

### Stat Flag Distribution System

Each enemy template in the database has five flags (one per attribute).
Flags determine how the total stat pool is distributed across STR/AGI/VIT/INT/WIL.
Every individual enemy instance rolls randomly within its flag's range — same
archetype, different individual. No two rats are identical.

```
Flag 0 / null : excluded from primary allocation (gets remainder only)
Flag 1        : 5%  - 10% of total stat pool
Flag 2        : 15% - 20% of total stat pool
Flag 3        : 25% - 30% of total stat pool
Flag 4        : 35% - 40% of total stat pool
Flag 5        : 45% - 50% of total stat pool
```

Allocation order:

1. Sort flagged attributes highest flag to lowest
2. For each, roll a random % within that flag's range, deduct from pool
3. Any remainder → split across ALL five attributes, rounded UP (harder, not floored)

Example — Dustfang Rat (StrFlag=3, AgiFlag=2, VitFlag=3, IntFlag=1, WilFlag=0):

```
Pool at Level 1: 20
Roll STR (Flag 3): 27% → ceil(5.4) = 6
Roll VIT (Flag 3): 26% → ceil(5.2) = 6
Roll AGI (Flag 2): 17% → ceil(3.4) = 4
Roll INT (Flag 1):  7% → ceil(1.4) = 2
Remainder: 20 - 6 - 6 - 4 - 2 = 2
Split across all 5: ceil(2/5) = 1 each
Final: STR 7, VIT 7, AGI 5, INT 3, WIL 1
```

### Enemy Categories (Database)

An enemy can belong to multiple categories (many-to-many relationship).
A zombie rat is both Vermin AND Undead — this is correct modeling.

Current categories seeded: Vermin, Beast, Undead, Spirit, Elemental, Divine, Cursed

Category membership affects:

- Loot table selection (future)
- Status effect resistances (future — all Undead resist poison, etc.)
- Ability assignments (future)

### Floor-Level Parity

```
Floor N → Level N standard mobs
Elite   → approximately Level N+2 effective strength
Boss    → approximately Level N+3 to N+5 effective strength
```

---

## 4. XP and Progression

### XP Per Kill

```
Standard mob : mob level × 10 XP
Elite        : mob level × 15 XP
Boss         : mob level × 25 XP
```

All multipliers live in TowerConfig.cs.

### XP to Level Up

```
XP required = currentLevel × 150
```

| Level | XP needed | Approx floors to level |
| ----- | --------- | ---------------------- |
| 1→2   | 150       | 3 floors               |
| 5→6   | 750       | 3 floors               |
| 20→21 | 3000      | 3 floors               |
| 50→51 | 7500      | 3 floors               |

Early floors are shorter (fewer fights) so early levels feel slightly harder to earn.
The jump from Level 1 to 2 should feel like an achievement.

### Level Cap

Maximum level: 200
Tower floors: 100 (Floor 100 = end of Phase 1 content)
Levels 101-200 = Phase 2+ content

### Level Up Timing

- Phase 1 (console): Level up and stat allocation at floor completion screen
- Phase 2+ (2D): Level up after defeating a mob; stat allocation when player chooses

### Stat Points Per Level — RSSG-Inspired System

```
Standard level up      : +3 free points
Every 5th level        : +3 free points + equipment tier unlocked
Every 10th level       : +3 free points + flat +5 to ALL attributes
Tier job change levels : +10 free points + attribute reset option
Level 200 (cap)        : +15 free points + flat +10 to ALL attributes
```

Equipment tier unlocks (every 5 levels) — gear carries the late-game power growth
that raw stats alone cannot.

### Checkpoint Bonuses (Every 10th Floor Boss)

```
All checkpoints:
├── Full HP/SP/MP restore
└── Save point unlocked

Floor 50: +1 bonus point into class primary stat
Floor 100: Special reward TBD + Title "Apex Climber"
```

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

XP loss may drop player below current level threshold → regress in level.
Stat points and abilities from the lost level are NOT removed.

### Regression Hard Floors

```
Adventurer (Tier 0)  : Cannot drop below Level 1
Tier 1 class         : Cannot drop below Level 20
Tier 2 class         : Cannot drop below Level 50
Tier 3 class         : Cannot drop below Level 90
Tier 4 class         : Cannot drop below Level 140
Level 200            : Cannot regress
```

---

## 6. Class System

### Birth Class

All characters begin as Adventurer (Level 1-20).
Access to Tier 0 abilities only. Level 20 triggers First Job Change.

### Job Change Levels

```
Tier 0 → Tier 1 : Level 20   (First Job Change)
Tier 1 → Tier 2 : Level 50   (Second Job Change)
Tier 2 → Tier 3 : Level 90   (Third Job Change)
Tier 3 → Tier 4 : Level 140  (Fourth Job Change)
Level 200        : True cap — something special (TBD)
```

Gaps between tiers: 30, 40, 50, 60 levels — intentionally widening.

### Realm System

```
Mortal Realm      : Level 1-20    (Adventurer)
Awakened Realm    : Level 21-50   (Tier 1)
Exalted Realm     : Level 51-90   (Tier 2)
Heroic Realm      : Level 91-140  (Tier 3)
Legendary Realm   : Level 141-200 (Tier 4)
```

NPCs react to realm, not level. Level 200 may unlock a realm beyond Legendary (TBD).

### Attribute Reset

- One full reset offered at Tier 1 job change (Level 20) — optional
- This is the ONLY full reset in the game, ever
- Each tier job change grants fresh points for that tier ONLY

### Starting Classes

| Class   | STR | AGI | VIT | INT | WIL | Identity             |
| ------- | --- | --- | --- | --- | --- | -------------------- |
| Warrior | 60  | 40  | 55  | 20  | 25  | Frontline, physical  |
| Mage    | 15  | 30  | 25  | 75  | 55  | Pure arcane, fragile |
| Rogue   | 35  | 70  | 30  | 35  | 30  | Speed, precision     |
| Cleric  | 30  | 25  | 45  | 40  | 60  | Spirit, support      |

### Evolution Paths

**Warrior:** Sentinel (tank) | Berserker (DPS) | Champion (hybrid) | ★ Warlord (hidden)
**Mage:** Elementalist (attack) | Enchanter (control) | Arcanist (hybrid) | ★ Archmage (hidden)
**Rogue:** Assassin (melee) | Marksman (ranged) | Shadow (hybrid) | ★ Phantom (hidden)
**Cleric:** Priest (heal) | Paladin (tank) | Templar (solo) | ★ Saint (hidden)

Hidden classes: require specific playstyle during Levels 1-20, harder quest,
failure has consequences (TBD). Specific unlock conditions TBD per class.

### Equipment Rarity Tiers

```
Common      : Level 1+
Iron        : Level 5+
Silver      : Level 10+
Gold        : Level 20+    (Tier 1 threshold)
Dark Gold   : Level 35+
Epic        : Level 50+    (Tier 2 threshold)
Legendary   : Level 90+    (Tier 3 threshold)
Divine      : Level 140+   (Tier 4 threshold)
Mythic      : Level 200
```

---

## 7. Ability System (Design Phase — Not Yet Implemented)

### Tiers

```
Tier 0: General, all classes, Levels 1-20
Tier 1: Class-specific, Level 20 job change
Tier 2: Evolution-specific, Level 50 job change
Tier 3: Advanced, Level 90 job change
Tier 4: Transcendence, Level 140+
```

### Cost Philosophy

- Physical abilities: SP cost derived from STR
  `Cost = max(1, baseCost + level/10 - Scale(STR, 0.5f))`
- Magic abilities: MP cost, softer scaling (level/20)
- All costs attribute-derived, not hardcoded flat numbers
- Costs increase with ability tier

### Tier 0 Ability Categories

| Category     | Examples                   | Signal        |
| ------------ | -------------------------- | ------------- |
| Basic Combat | Power Strike, Quick Step   | Universal     |
| Physical     | Rend, Rush, Parry Counter  | Warrior/Rogue |
| Arcane       | Mana Bolt, Ward            | Mage/Cleric   |
| Survival     | Second Wind, Steady Breath | Universal     |
| Utility      | Detect, Inspect, Forage    | Universal     |

Inspect (future): reveals enemy stats currently hidden in UI.

---

## 8. Combat System Notes

### Architecture

- Decide layer: CombatCalculator.cs — pure static functions, no state
- Apply layer: CombatManager.cs — stateful, mutates via immutable record with expressions
- Always re-fetch character from state after any UpdateCharacter call (stale reference rule)

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

### Combat Rules

- Hit chance: Accuracy / (Accuracy + Evasion) + 0.25 flat
- Damage reduction: DEFENSE_K(30) / (DEFENSE_K + defense)
- No minimum damage — 0 damage is valid
- Block: deterministic (BlockSpeed × 1.5 >= attacker Initiative)
- Full block: 100% reduction. Partial: 50% reduction
- No passive HP regen in combat
- SP/MP regen every round (min 1), doubled on Wait action
- Exhausted (0 SP): can still attack at 0.75x modifier, cannot block

### Stamina Actions

```
Attack : costs AttackSpCost (derived from STR)
Defend : costs BlockSpCost  (derived from VIT/STR blend)
Wait   : 0 cost, double SP/MP regen this round
Dodge  : costs DodgeSpCost  (not yet implemented)
```

### Turn Order (Current)

Recalculated after each action by Initiative. HasActed flag tracks who has gone.
Round ends when all living fighters have acted.

Future (Phase 2): Action gauge — time-to-next-action counter replaces rounds.

### AI Decision Tree

```
1. SP == 0 AND HP > 40%             → Wait
2. Target HP < 15%                  → Attack (finish them)
3. HP < 30% AND defended last turn  → Attack (no turtling)
4. HP < 30% AND slower than target  → Defend
5. HP < 30% AND faster than target  → Attack
6. Default                          → Attack
```

### Enemy Display

- Enemy HP: bar + condition label (no numbers). Player HP: bar + numbers.
- Conditions: Unscathed / Scratched / Bloodied / Wounded / Critical / Near Death
- Enemy stats (???) hidden until Inspect ability unlocked
- IsPlayerControlled flag on Character — flippable by future Dominate/Charm abilities

---

## 9. Status Effects (Planned)

AttackResult carries a nullable StatusEffect string field (already wired).
Design TBD during ability storyboarding.
Examples: Bleed, Poison, Burn, Stun, Slow, Charm, Blind, Freeze.

---

## 10. Stamina Philosophy

Two-force SP cost system:

```
Final SP Cost = max(1, baseCost + level/10 - attributeReduction)
```

Level scaling pushes cost up. Attribute investment pulls cost down.
Both MaxSP and costs grow — ratio stays meaningful at all levels.

Block → Wait → Attack rhythm is a core tactical pattern.

---

## 11. Database

### Technology

SQLite via Microsoft.Data.Sqlite. Single file at `data/ascension.db`.
Committed to repo — contains seed data the game needs to run.

Future split:

- `ascension.db`: seed/game data (committed)
- `save.db`: player save data (gitignored, Phase 2)

### Current Schema

```sql
Categories (
    Id   TEXT PRIMARY KEY,
    Name TEXT NOT NULL UNIQUE
)

EnemyTemplates (
    Id       TEXT PRIMARY KEY,
    Name     TEXT NOT NULL,
    FloorMin INTEGER NOT NULL,
    FloorMax INTEGER NOT NULL,
    StrFlag  INTEGER NOT NULL DEFAULT 0,
    AgiFlag  INTEGER NOT NULL DEFAULT 0,
    VitFlag  INTEGER NOT NULL DEFAULT 0,
    IntFlag  INTEGER NOT NULL DEFAULT 0,
    WilFlag  INTEGER NOT NULL DEFAULT 0,
    IsElite  INTEGER NOT NULL DEFAULT 0,
    IsBoss   INTEGER NOT NULL DEFAULT 0
)

EnemyCategories (
    EnemyId    TEXT NOT NULL,  → FK to EnemyTemplates
    CategoryId TEXT NOT NULL,  → FK to Categories
    PRIMARY KEY (EnemyId, CategoryId)
)
```

### Seeded Data

Categories: Vermin, Beast, Undead, Spirit, Elemental, Divine, Cursed
Enemies: Dustfang Rat (Floor 1-10, Vermin, StrF3 AgiF2 VitF3 IntF1 WilF0)

### Planned Tables (Future)

```
Abilities       ← ability definitions
EnemyAbilities  ← which abilities each enemy has (many-to-many)
LootTables      ← loot definitions
EnemyLoot       ← which loot each enemy drops (many-to-many)
PlayerSaves     ← save data (separate file, gitignored)
```

### Design Principles

- Third normal form: each table has one responsibility
- Relationships via foreign keys, never duplicated data
- Many-to-many via join tables (EnemyCategories pattern)
- A zombie rat belongs to both Vermin AND Undead — correct modeling

---

## 12. Save System (Planned)

- Save at checkpoint floors (every 10th floor boss cleared)
- System.Text.Json for Phase 1 saves
- PostgreSQL + EF Core for Phase 3 MMO scale
- Current enemy C# files (Vermin.cs etc.) will become DB seed data

---

## 13. Equipment and Loot (Planned)

Rarity tiers in Section 6. Loot tables TBD during storyboarding.
Equipment bridges the gap between player raw stats and mob scaling.
This is why mob scaling outpaces raw stat growth — gear is the intended equalizer.

---

## 14. Future Platform Path

```
Phase 1 (current): C# console dungeon crawler
Phase 2: Godot 2D top-down RPG
    - Action combat (time gauge replaces turn order)
    - Overworld exploration, quests, dialogue
Phase 3: Godot 2D MMO
    - Server backend, multiplayer, guilds
Phase 4 (indefinitely deferred): 3D
```

---

## 15. Planned Tools / Libraries

| Tool                  | Purpose                         | Status  |
| --------------------- | ------------------------------- | ------- |
| Spectre.Console       | Terminal UI                     | Active  |
| Microsoft.Data.Sqlite | Database                        | Active  |
| System.Text.Json      | Save files                      | Planned |
| xUnit                 | Unit tests (CombatCalculator)   | Planned |
| SQLite + Dapper       | Cleaner DB queries (Phase 2)    | Future  |
| PostgreSQL + EF Core  | MMO backend                     | Phase 3 |
| Godot (C# bindings)   | 2D game engine                  | Phase 2 |
| DB Browser for SQLite | Visual DB management (dev tool) | Active  |

---

## 16. Repo Structure

```
ascension-cs/
├── src/
│   ├── Models/          ← Pure data records (no logic)
│   ├── Combat/          ← CombatCalculator (Decide) + CombatManager (Apply)
│   ├── Core/            ← TowerConfig, XpSystem, FloorManager (in progress)
│   ├── Data/
│   │   ├── Database/    ← DbConfig, DbInitializer
│   │   ├── Enemies/     ← Vermin.cs (legacy, migrating to DB)
│   │   └── Fighters.cs  ← Demo characters (temporary)
│   ├── UI/              ← All Spectre.Console display logic
│   └── Program.cs       ← Entry point (3 lines)
├── data/
│   └── ascension.db     ← SQLite database (committed)
└── docs/
    └── DESIGN.md        ← This file
```

Rules:

- Models/ never imports from Combat/
- Display logic only in UI/
- All tunable constants in TowerConfig.cs
- Always re-fetch character from CombatManager state after mutations

---

## 17. Current Build State

### Implemented and Working

- Full character model (Identity, Progression, Control, Combat)
- CombatCalculator: all derived stats, hit/damage/block resolution, turn order
- CombatManager: full Apply layer, SP costs, regen, Wait action
- CharacterCreation: name input, arrow-key stat allocation, live preview
- MainMenu, GameLoop: full game flow with player-controlled combat
- CombatDisplay: HP/SP/MP bars, condition labels, enemy obfuscation, action menu
- TowerConfig: all game constants and helper methods
- XpSystem: GainXp (with level up events), ApplyDeath, SpendStatPoint
- SQLite: schema created, categories and Dustfang Rat seeded

### In Progress

- MobFactory: reads enemy templates from DB, applies flag system, generates Character
- FloorManager: floor progression, encounter generation, XP awards

### Not Yet Started

- Floor transition screen (level up display, stat allocation)
- Save/load system
- Multiple enemies per encounter
- Ability system
- Loot system
- Named boss enemies

---

_Update this document whenever a significant design decision is made._
_The code captures implementation. This captures why._
