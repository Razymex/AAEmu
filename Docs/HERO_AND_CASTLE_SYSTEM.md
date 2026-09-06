# Hero and castle (dominion / siege) — current state

Hero election and the Hero/faction castle path (claim, tax, siege raid teams) live on this
branch. Zone groups **54 / 56** (Exeloch / Sungold) have no `siege_zones` row, so they stay
on `GuildDominionManager` as the no-schedule / guild claim path. Player-founded nations are
out of scope.

There is **no compact.sqlite3 surgery**. Shipped `special_effects` / `skill_effects` do not
include synthetic types 197–199. Skills **41079** (new wall/gate/tower drawings +
`dominion_housings`) and **33550** (old keep-set housings 644–656) are tooltip/reagent
skills with zero `skill_effects` by design. They are not dead. Real declare is **13661**
→ special **50**. Territory buildings go through `HousingManager.Build` when the item has
an `item_housings` design.

## MySQL (`aaemu_game`)

Applied by `MySqlDatabaseUpdater` when `Connections.AutoApplyUpdates` is true
(`SQL/updates/`, tracked in `updates`):

| File | Adds |
|---|---|
| `2026-08-13_aaemu_game_dominion_lodestones.sql` | Seeds Archeum Lodestone houses for skill 13661, including housings **271 / 272** (zone groups 54 / 56). |
| `2026-08-13_aaemu_game_dominions.sql` | Live claim rows (no `national_*` columns). |
| `2026-08-13_aaemu_game_dominions_guard_tower_step.sql` | `dominions.guard_tower_step` (legacy column; nothing writes a live step). |
| `2026-08-13_aaemu_game_hero_election.sql` | `hero_candidates` and companion election tables. |
| `2026-08-13_aaemu_game_leadership_point.sql` | First `characters.leadership_point` column. |
| `2026-08-13_aaemu_game_siege_raid_teams.sql` | `siege_raid_team_members` / siege scores. |
| `2026-08-14_aaemu_game_leadership_point_period.sql` | Period column + backfill. |
| `2026-08-15_aaemu_game_hero_votes_pk.sql` | Ballot PK includes `candidate_character_id`. |
| `2026-08-15_aaemu_game_leadership_period_split.sql` | Current / previous / lifetime / daily-cap leadership columns. |
| `2026-08-21_aaemu_game_dominions_castle_tier.sql` | `dominions.castle_tier` (legacy column; nothing writes a live tier). |
| `2026-08-21_aaemu_game_dominions_faction_id.sql` | `dominions.faction_id` for Hero/faction claims. |
| `2026-08-24_aaemu_game_dominion_locked_zones.sql` | GM claim lock per zone group. |
| `2026-08-31_aaemu_game_hero_bonus_progress.sql` | Daily Hero-bonus box progress. |
| `2026-08-31_aaemu_game_hero_mobilization_order.sql` | Mobilization issue counts / last-issued. |
| `2026-09-06_aaemu_game_hero_dominion_point_gives.sql` | `hero_dominion_point_gives` (weekly-capped gifts). |
| `2026-09-06_aaemu_game_world_doodad_phases.sql` | Ambient doodad phase persist (statues already have funcs). |

Deleted / not on this branch: `faction_statues.sql`, `hero_dominion_points.sql` (character
columns), every `nations*` / `nation_relations` script. Existing `guard_tower_step` /
`castle_tier` columns are left in place; they are not dropped.

## Compact (read-only)

Used as shipped. No `special_effects` 197/198/199. `feature_sets` is empty.
There is no `guild_dominion_housings` table in any compact — the client only loads
`dominion_housings` (8 rows). Statues **10588 / 10650 / 10651** have `DoodadFuncDevote`
+ `DoodadFuncUse`. `siege_zones` covers **33 / 34 / 43 / 44** only. Zone groups **54 /
56** are real (`o_abyss_gate` / `o_land_of_sunlights`) with lodestones **271 / 272**,
items **27697 / 27698**, and `guard_tower_settings` **11 / 12**, but no siege calendar.

## Live path (no GM)

- **Hero election** — `hero_schedules` + leadership + ballots + `hero_rewards` mail.
- **Declare** — skill **13661** on a seeded lodestone. A `siege_zones` row is the Hero
  path (all four: Friday 20:00–21:00 after a Tuesday `week_start`). No row (54 / 56)
  is the guild path. Plot 2 / `world_message_effects` on 13661 are the announcements.
- **Territory agent** — `siege_zones.dominion_merchant_id`, housing `AutoZOffset*` +
  `TerrainFloor`. Re-announced on Zone reload. 54 / 56 have no merchant id.
- **Tax** — `content_configs.hero_dominion_tax_rate_min/max` (10/30) and
  `dominion_tax_limit`. Weekly mail on `siege_plans.week_start` rollover
  (`MailType.NationTaxReceipt`). House-tax **collection** into the pool is still unwired
  (do not invent a credit from `doodad_func_dominion_tax_in_kinds`).
- **Mobilization** — flag doodads + `content_configs` (level, daily 5, accept window
  `mobilization_order_accept_delay` = 60, item 43763). `hero_dominion_cooldown` 1800 is
  the dominion-**point** cooldown, not the accept window. CS/SC already wired.
- **Dominion points** — `hero_dominion_*` configs + `hero_dominion_point_gives`.
- **Guild residences** — `housings.family` prefix `hs_expedition_house`, one per guild.
- **Unique territory buildings** — `dominion_housings` via `HousingManager.Build` on a
  claimed zone. Hero-only on a Hero claim.

`/claimterritory`, `/herophase`, `/siegewindow` are diagnostics only.

## Removed (not retail)

- Invented `guild_dominion_housings` loader / `IsGuildDominionHousingTemplate`.
- Synthetic special types 197 / 198 / 199 and any World-authored wall spawn from
  `guard_tower_steps`.
- Leftover SC leftovers with no client packet and colliding opcodes:
  `SCDominionTaxBalanced` **0x23**, `CDominionStartUnk` **0x24**, `SCDominionEndUnk`
  **0x25**, `SCSiegeReinforce` **0xeb**.
- In-memory `guard_tower_step` / `castle_tier` counters (nothing in shipped data writes
  them). MySQL columns stay; new writes store 0.

## Left open

- Exeloch / Sungold (54 / 56): no `siege_zones` calendar. Fail-closed on siege schedule;
  13661 can still take the guild claim path.
- `guard_tower_steps` gate/wall counts do not spawn objects (player drawings do).
- House-tax collection into the weekly pool is unwired; do not invent a formula.
- Several unused-but-real SC Hero packet constructors are still not sent.
- Ordinary (non-`dominion_housings`) designs, including 41079 wall/gate drawings, are
  still refused on claimed land — pre-existing, not invented here.
