# Ascension — Design Document

> Last updated: June 2026
> Phase 1 (C# Console) — COMPLETE
> Phase 2 (Godot 2D) — STARTING NEXT
>
> This document is the single source of truth for design intent.
> The code captures implementation. This captures why.
> Written for developer reference and Claude session continuity.

---

## 0. Design Reference

### Primary Reference: Reincarnation of the Strongest Sword God (RSSG)

Used as a primary framework for mechanics and progression. We borrow the shape
and philosophy, not the raw numbers or setting.

What we borrow:

- Milestone progression (equipment thresholds every 5 levels, flat bonuses at 10)
- Equipment carrying late-game power growth
- Tier promotion structure
- Ability tier scaling with job changes
- Equipment rarity tiers as power breakpoints

What we don't copy:

- Raw numbers (RSSG: 70,000+ attributes. Ours: hundreds)
- Story/setting (mythological amalgam, not VRMMORPG)
- Level cap (RSSG: 200+. Ours: tower 100 floors, level cap 200)

---

## 1. World Concept

### Setting

Mythological amalgam — all cultures coexist. Greek, Norse, Egyptian, Chinese,
Japanese, Celtic, Mesoamerican, Slavic and more. A Pegasus and a Jiangshi can
appear on the same floor. No single mythology dominates.

### The Tower — Axis Mundi

The world axis. Every culture has a version:
Norse: Yggdrasil | Hindu: Mount Meru | Greek: Mount Olympus
Egyptian: Benben | Aztec: Pyramid of the Sun | Babylon: Ziggurat

Axis Mundi is the convergence point of all mythologies. Floor 1 has rats.
Floor 100 has something that makes gods nervous.

Clearing Floor 100 ends Phase 1 content. Levels continue to 200.
The tower was the beginning, not the destination.

---

## 2. Platform Path

```
Phase 1 (COMPLETE) : C# console dungeon crawler — ascension-cs repo
Phase 2 (NEXT)     : Godot 2D top-down RPG — ascension-2d repo (new)
    - Action combat (time gauge replaces turn order)
    - Overworld exploration
    - Quest system
    - Full story/dialogue
Phase 3            : Godot 2D MMO
    - Server backend, multiplayer, guilds
Phase 4 (deferred) : 3D
```

### Engine Decision: Godot

Chosen over Unity for:

- Superior 2D tooling (built from ground up, not bolted on)
- Zero cost forever (Unity had 2023 runtime fee controversy)
- C# is first-class (same language as Phase 1)
- Built-in multiplayer API adequate for Phase 3

Free asset sources (engine agnostic):

- itch.io/game-assets (filter free)
- kenney.nl/assets (professional quality, free)
- opengameart.org (community, all free)
- freesound.org (sound/music)
- github.com/godotengine/awesome-godot (curated Godot resources)

### What Ports from Phase 1 to Phase 2

```
PORTS DIRECTLY (logic, rewrite as Godot C# scripts):
- CombatCalculator.cs    ← all math, pure functions
- XpSystem.cs            ← all logic, pure functions
- TowerConfig.cs         ← all constants
- MobFactory.cs          ← reads from DB, builds Character
- All Models/            ← records port to classes/structs in Godot

STAYS (database):
- ascension.db           ← SQLite file, copy to new project
- DB schema unchanged
- Add save data tables

REPLACED (display/UI):
- Spectre.Console        ← Godot nodes replace all UI
- CombatManager.cs       ← Godot scene tree manages state
- GameLoop.cs            ← Godot SceneManager handles flow
- Turn-based loop        ← Action gauge system replaces Option B
```

---

## 3. Core Game Loop

### Phase 2 Target Loop

```
Town (Base Camp) → Enter Tower Floor → Encounter(s) → Floor Complete
→ Loot / Rest / Level Up / Save → Next Floor or Return to Town
```

### Floor Structure

```
Floor 1-4   : Standard encounters (1-3 fights)
Floor 5     : Elite encounter
Floor 6-9   : Standard encounters
Floor 10    : Boss + Checkpoint (save point, full restore)
(Repeats every 10 floors)
```

### Floor Themes (Phase 2 — when overworld exploration added)

```
Floors 1-10   : Plains
Floors 11-20  : Forest
Floors 21-30  : Mountain
Floors 31-40  : Ruins
Floors 41-50  : Underground Caverns
Floors 51-60  : Ancient Temple
Floors 61-70  : Frozen Wastes
Floors 71-80  : Volcanic Depths
Floors 81-90  : Celestial Approach
Floors 91-100 : The Apex
```

---

## 4. Combat System

### Architecture (Phase 1 — reference for Phase 2 port)

- **Decide layer**: CombatCalculator.cs — pure static functions, no state
- **Apply layer**: CombatManager.cs — stateful mutations
- All records immutable — always re-fetch after UpdateCharacter calls

### Phase 2 Combat — Action Gauge (Option C)

Replaces turn-based Option B. Characters have a time-to-next-action counter.
Faster characters (higher AGI/Initiative) act more frequently.
Effectively real-time with pause. Renders as visual bar draining in UI.
Design TBD when Phase 2 starts — current math foundation carries over.

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

### Hit / Damage / Block Formulas

```
hitChance  = Accuracy / (Accuracy + Evasion) + 0.25
damage     = (int)(rawDamage × modifiers × DEFENSE_K/(DEFENSE_K + defense))
             DEFENSE_K = 30. No minimum — 0 damage is valid.
fullBlock  = BlockSpeed × 1.5 >= attacker Initiative
             Full block: 100% reduction. Partial: 50%.
             Block deterministic (no dice). Hit/miss uses dice.
```

### Stamina Rules

```
Attack  : costs AttackSpCost (derived from STR + level)
Defend  : costs BlockSpCost  (derived from VIT/STR + level)
Wait    : 0 cost, doubles SP and MP regen this round
Dodge   : costs DodgeSpCost  (not yet implemented)
0 SP    : can still attack at 0.75x modifier, cannot block
```

Two-force SP cost system:
`Final Cost = max(1, baseCost + level/10 - attributeReduction)`
Level scaling pushes cost up. Attribute investment pulls it down.

### SP/MP Regen

```
Every round: base regen applies (min 1), regardless of action taken
Wait action: doubles both SP and MP regen
HP: NO passive regen in combat. Only via abilities/items/rest.
```

### Block → Wait → Attack Rhythm

Core tactical pattern. Block incoming damage, Wait to double-regen SP,
Attack with full resources. Rewards patient play at all levels.

### AI Decision Tree (Rudimentary — Phase 1)

```
1. SP == 0 AND HP > 40%            → Wait
2. Target HP < 15%                 → Attack (finish them)
3. HP < 30% AND defended last turn → Attack (no turtling)
4. HP < 30% AND slower than target → Defend
5. HP < 30% AND faster than target → Attack
6. Default                         → Attack
```

### Enemy Display Rules

- Enemy HP: bar graphic + condition label. NO exact numbers.
- Conditions: Unscathed / Scratched / Bloodied / Wounded / Critical / Near Death
- Enemy stats hidden (???) until Inspect ability unlocked (future)
- IsPlayerControlled flag on Character — flippable by Dominate/Charm abilities

### Status Effects (Planned — not yet implemented)

AttackResult.StatusEffect string field already wired.
Examples: Bleed, Poison, Burn, Stun, Slow, Charm, Blind, Freeze.
Design TBD during ability storyboarding.

---

## 5. Mob Scaling

### Philosophy

- Early (1-20): Mobs ≈ player stats. Skill matters.
- Mid (21-90): Mobs pull 20-80% ahead. Gear matters.
- Late (91-200): Mobs pull 100%+ ahead. Gear + abilities required.

Mob scaling outpaces raw stat growth intentionally.
Equipment bridges the gap. This is why gear matters at high levels.

### Mob Stat Pool Formula

```
mobStatPool(level) = 20 + (level - 1)^1.3 × 1.82
```

Constants in TowerConfig.cs: MobScaleBase=20, MobScalePow=1.3, MobScaleFactor=1.82

| Level | Mob stats | Player raw | Mob advantage |
| ----- | --------- | ---------- | ------------- |
| 1     | 20        | 20         | Even          |
| 20    | 103       | 91         | +13%          |
| 50    | 307       | 209        | +47%          |
| 100   | 735       | 411        | +79%          |

### Enemy Type Multipliers

```
Standard : 1.0x
Elite    : 1.3x - 1.5x (random within range)
Boss     : 1.8x - 2.3x (random within range)
```

### Stat Flag Distribution System

Each enemy template has 5 flags (one per attribute) stored in DB.
Controls how stat pool distributes across STR/AGI/VIT/INT/WIL.
Random roll within flag range = same archetype, different individual.

```
Flag 0/null : remainder only (not primary allocated)
Flag 1      : 5%  - 10%
Flag 2      : 15% - 20%
Flag 3      : 25% - 30%
Flag 4      : 35% - 40%
Flag 5      : 45% - 50%
```

Allocation order: highest flag first → roll random % → deduct from pool.
Remainder: split across ALL five attributes, rounded UP (harder, not floored).

Dustfang Rat example (StrF3 AgiF2 VitF3 IntF1 WilF0, pool=20):

```
STR: roll 27% → ceil(5.4) = 6
VIT: roll 26% → ceil(5.2) = 6
AGI: roll 17% → ceil(3.4) = 4
INT: roll  7% → ceil(1.4) = 2
Remainder 2 → ceil(2/5)=1 each
Final: STR 7, VIT 7, AGI 5, INT 3, WIL 1
```

### Enemy Categories (Many-to-Many)

Current: Vermin, Beast, Undead, Spirit, Elemental, Divine, Cursed
A zombie rat = Vermin AND Undead (correct modeling, not a bug).
Future use: loot table selection, status effect resistances, ability assignments.

---

## 6. XP and Progression

### XP Per Kill

```
Standard : mob level × 10
Elite    : mob level × 15
Boss     : mob level × 25
```

### XP to Level Up

`XP required = currentLevel × 150`

| Level | XP needed | ~Floors to level |
| ----- | --------- | ---------------- |
| 1→2   | 150       | 3                |
| 10→11 | 1500      | 3                |
| 50→51 | 7500      | 3                |

Early floors are shorter → early levels feel slightly harder. Intentional.
Level 1→2 should feel like an achievement.

### Level Cap: 200

Tower floors: 100. Floors 1-100 = Phase 2 content.
Levels 101-200 = Phase 3+ content (overworld, MMO).

### Level Up Timing

- Phase 2: Level up triggers after defeating a mob
- Stat allocation: when player chooses (menu/pause)

### Stat Points Per Level (RSSG-inspired)

```
Standard level    : +3 free points
Every 5th level   : +3 free points + equipment tier unlocks
Every 10th level  : +3 free points + flat +5 to ALL attributes
Job change levels : +10 free points + optional attribute reset
Level 200 (cap)   : +15 free points + flat +10 to ALL attributes
```

Priority (higher supersedes, not stacked):
Level 200 (+15+flat10) > job change (+10) > mult10 (+3+flat5) > mult5 (+3) > standard (+3)

### Death Mechanics

```
On death: lose 20% total accumulated XP
Respawn: last checkpoint floor (or Floor 1 if none cleared)
Keep: character, level, attributes, abilities, gear
Lose: floor progress since last checkpoint
```

Level regression: XP loss may drop below level threshold → regress.
Stat points and abilities from lost level are NOT removed.

Regression hard floors (cannot drop below):

```
Tier 0 (Adventurer) : Level 1
Tier 1              : Level 20
Tier 2              : Level 50
Tier 3              : Level 90
Tier 4              : Level 140
Level 200           : Cannot regress
```

---

## 7. Class System

### Birth Class: Adventurer (Level 1-20)

- No class identity yet
- Access to Tier 0 abilities only (general, not class-locked)
- Exploration phase — player discovers their playstyle
- Stats tracked silently to influence job change offer at Level 20

### Job Change Levels

```
Tier 0 → Tier 1 : Level 20   (+10 points, optional full reset — ONLY reset ever)
Tier 1 → Tier 2 : Level 50   (+10 points, resets Tier 1 points only)
Tier 2 → Tier 3 : Level 90   (+10 points, resets Tier 2 points only)
Tier 3 → Tier 4 : Level 140  (+10 points, resets Tier 3 points only)
Level 200        : True cap — something special (TBD)
```

Gaps: 30, 40, 50, 60 levels. Intentionally widening — each tier is a chapter.

### Realm System

```
Mortal Realm    : Level 1-20    (Adventurer)
Awakened Realm  : Level 21-50   (Tier 1)
Exalted Realm   : Level 51-90   (Tier 2)
Heroic Realm    : Level 91-140  (Tier 3)
Legendary Realm : Level 141-200 (Tier 4)
```

NPCs react to realm, not level. A peasant feels your realm, not your number.

### Job Change Quest Logic

```
Mostly physical ability usage  → Warrior or Rogue quest
Mostly arcane ability usage    → Mage or Cleric quest
Balanced usage                 → All four offered (true hybrid path)
```

Quest nature reveals the class — challenge shows what kind of fighter you are.

### Starting Classes (200 base + 15 bonus at creation)

| Class   | STR | AGI | VIT | INT | WIL | Identity             |
| ------- | --- | --- | --- | --- | --- | -------------------- |
| Warrior | 60  | 40  | 55  | 20  | 25  | Frontline, physical  |
| Mage    | 15  | 30  | 25  | 75  | 55  | Pure arcane, fragile |
| Rogue   | 35  | 70  | 30  | 35  | 30  | Speed, precision     |
| Cleric  | 30  | 25  | 45  | 40  | 60  | Spirit, support      |

### Evolution Paths (all at Level 20 job change)

**Warrior:** Sentinel (tank) | Berserker (DPS) | Champion (hybrid) | ★ Warlord (hidden)
**Mage:** Elementalist (attack) | Enchanter (control) | Arcanist (hybrid) | ★ Archmage (hidden)
**Rogue:** Assassin (melee) | Marksman (ranged) | Shadow (hybrid) | ★ Phantom (hidden)
**Cleric:** Priest (heal) | Paladin (tank) | Templar (solo) | ★ Saint (hidden)

Hidden classes:

- Require specific playstyle conditions during Levels 1-20
- Quest is noticeably harder than standard
- Failing has consequences (TBD — storyboard later)
- Specific unlock conditions TBD per class

### Equipment Rarity Tiers

```
Common    : Level 1+
Iron      : Level 5+    (first threshold)
Silver    : Level 10+
Gold      : Level 20+   (Tier 1 job change)
Dark Gold : Level 35+
Epic      : Level 50+   (Tier 2 job change)
Legendary : Level 90+   (Tier 3 job change)
Divine    : Level 140+  (Tier 4 job change)
Mythic    : Level 200   (true cap)
```

Finding gear above your expected floor rarity = meaningful discovery.

---

## 8. Ability System (Design Phase — Phase 2)

### Tiers

```
Tier 0 : General, all classes, Levels 1-20
Tier 1 : Class-specific, unlocked at Level 20 job change
Tier 2 : Evolution-specific, Level 50 job change
Tier 3 : Advanced, Level 90 job change
Tier 4 : Transcendence, Level 140+
```

### Cost Philosophy

- All costs derived from attributes, not hardcoded
- Physical (SP): `Cost = max(1, baseCost + level/10 - Scale(STR, 0.5f))`
- Magical (MP): softer scaling `level/20`
- Costs increase with ability tier

### Tier 0 Ability Categories

| Category     | Examples                   | Signals       |
| ------------ | -------------------------- | ------------- |
| Basic Combat | Power Strike, Quick Step   | Universal     |
| Physical     | Rend, Rush, Parry Counter  | Warrior/Rogue |
| Arcane       | Mana Bolt, Ward            | Mage/Cleric   |
| Survival     | Second Wind, Steady Breath | Universal     |
| Utility      | Detect, Inspect, Forage    | Universal     |

Inspect (future): reveals enemy stats currently shown as ???

---

## 9. Database

### Technology

SQLite via Microsoft.Data.Sqlite.
DB file: `data/ascension.db` — committed to repo.

Future split (Phase 3):

- `ascension.db`: game/seed data (committed)
- `save.db`: player save data (gitignored)

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
    EnemyId    TEXT NOT NULL  → FK EnemyTemplates
    CategoryId TEXT NOT NULL  → FK Categories
    PRIMARY KEY (EnemyId, CategoryId)
)
```

### Seeded Data

Categories: Vermin, Beast, Undead, Spirit, Elemental, Divine, Cursed
Enemies: Dustfang Rat (Floor 1-10, Vermin, StrF3 AgiF2 VitF3 IntF1 WilF0)

### Planned Tables

```
Abilities       : ability definitions
EnemyAbilities  : which abilities each enemy has (many-to-many)
LootTables      : loot definitions
EnemyLoot       : which loot each enemy drops (many-to-many)
PlayerSaves     : save data (separate file, gitignored)
```

### Design Principles

- Third normal form: one table, one responsibility
- Relationships via foreign keys, not duplicated data
- Many-to-many via join tables
- A zombie rat is Vermin AND Undead — both rows in EnemyCategories

---

## 10. Phase 2 Starting Point — What to Build First in Godot

This is the porting roadmap. Read this at the start of the next session.

### Step 1 — Godot Project Setup

- New project: `ascension-2d`
- New repo: `github.com/lulMen/ascension-2d`
- Copy `docs/DESIGN.md` into `ascension-2d/docs/`
- Copy `data/ascension.db` into `ascension-2d/data/`
- Language: C# (.NET)

### Step 2 — Port Core Logic (no visuals yet)

Port these files as Godot C# scripts (minimal changes needed):

```
CombatCalculator.cs  → Scripts/Combat/CombatCalculator.cs
XpSystem.cs          → Scripts/Core/XpSystem.cs
TowerConfig.cs       → Scripts/Core/TowerConfig.cs
MobFactory.cs        → Scripts/Data/MobFactory.cs
All Models/          → Scripts/Models/
```

Keep logic pure — no Godot-specific code in these files yet.
They should compile and pass unit tests independently.

### Step 3 — Scene Structure

```
res://
├── Scenes/
│   ├── Main.tscn           ← entry point
│   ├── MainMenu.tscn
│   ├── CharacterCreation.tscn
│   ├── TowerFloor.tscn     ← the combat scene
│   ├── FloorTransition.tscn← level up, loot, save
│   └── HUD.tscn            ← HP/SP/MP bars, enemy panel
├── Scripts/
│   ├── Combat/
│   ├── Core/
│   ├── Data/
│   ├── Models/
│   └── UI/
├── Assets/
│   ├── Sprites/
│   ├── Tilesets/
│   └── Audio/
└── data/
    └── ascension.db
```

### Step 4 — First Visual Build

Build in this order:

1. HUD — HP/SP/MP bars (Godot ProgressBar nodes)
2. Character sprite placeholder on screen
3. Enemy sprite placeholder on screen
4. Basic attack animation (sprite flash, damage number popup)
5. Action gauge (Godot Timer + ProgressBar)
6. Wire CombatCalculator to the visual combat

### Step 5 — Action Gauge Combat (Option C)

Replace turn order with time gauge:

- Each combatant has a gauge filling based on Initiative
- When gauge fills → that character acts
- Player gets action menu when their gauge fills
- Enemy acts automatically when theirs fills
- Faster characters act more often, not just first

This is the core Phase 2 design work. No detailed spec yet —
design it as you build it, starting from the math foundation.

### Step 6 — Floor Progression

Wire FloorManager (port from Phase 1 design):

- Floor entry → generate encounters from DB
- Encounter complete → award XP, check level up
- Floor complete → transition screen
- Boss floor → checkpoint, save, full restore

---

## 11. What's NOT Done Yet (Backlog)

### Needs Building in Phase 2

- [ ] Action gauge combat
- [ ] Visual combat scene
- [ ] FloorManager (port from Phase 1 design, not fully built)
- [ ] Floor transition screen (level up, stat allocation)
- [ ] Save/load system (System.Text.Json Phase 1, SQLite Phase 2)
- [ ] Multiple enemies per encounter
- [ ] Named boss enemies in DB
- [ ] Loot system + loot tables
- [ ] Town/base camp scene
- [ ] Overworld map (later Phase 2)

### Needs Storyboarding First

- [ ] Full ability system (Tier 0 through Tier 4)
- [ ] Enemy roster beyond Dustfang Rat
- [ ] Boss designs (named, with abilities)
- [ ] Hidden class unlock conditions
- [ ] Hidden class quest consequences for failure
- [ ] Status effect interactions
- [ ] Loot table design
- [ ] Story/dialogue/lore
- [ ] Quest system design
- [ ] Level 200 "something special"

### Design Decisions Still Open

- [ ] Exactly what happens at Level 200
- [ ] Realm beyond Legendary (Level 200+)
- [ ] Tower completion reward (Floor 100)
- [ ] Post-tower content structure before Phase 3

---

## 12. Phase 1 Reference Repo

`github.com/lulMen/ascension-cs`

Complete console implementation. Reference this when porting logic.
All combat math is working and tested. All formulas are validated.
Do not modify this repo — it is the archived Phase 1 reference.

---

## 13. Tools and Libraries

| Tool                  | Purpose                      | Status  |
| --------------------- | ---------------------------- | ------- |
| Godot 4 (C#)          | 2D game engine               | Phase 2 |
| Microsoft.Data.Sqlite | Database                     | Active  |
| System.Text.Json      | Save files (Phase 2 interim) | Planned |
| xUnit                 | Unit tests for combat math   | Planned |
| SQLite + Dapper       | Cleaner DB queries           | Phase 2 |
| PostgreSQL + EF Core  | MMO backend                  | Phase 3 |
| DB Browser for SQLite | Visual DB management (dev)   | Active  |
| itch.io / Kenney      | Free art assets              | Phase 2 |

---

## 14. Conventions Carried Forward

- Models are pure data (no logic inside them)
- Decide layer = pure static functions (CombatCalculator pattern)
- Apply layer = stateful mutations (CombatManager pattern)
- Always re-fetch from state after any mutation (stale reference rule)
- Tunable constants in TowerConfig.cs only — never scattered in logic
- Display logic in its own layer — never mixed with combat logic
- DB: third normal form, one table one responsibility, joins for many-to-many
- Enemy stats hidden from player until Inspect ability unlocked
- IsPlayerControlled flag on Character (not hardcoded in game loop)
- No passive HP regen in combat — only via abilities/items/rest
- 0 damage is valid (realistic — high level player tanks a weak hit)

---

_See you in the next session. Start at Section 10._
_The math works. The design is solid. Time to make it visual._
