# GF-W12 faction-scoring runtime slice

This slice is stacked on the published W12 metadata contracts
(`feat/w12-faction-score-metadata`). It adds only deterministic, catalog-backed scoring rules.

## Implemented

- NPC-kill contribution resolution through `faction_competition_npc_infos`.
- Quest-completion contribution resolution through `faction_competition_quest_infos`.
- Point lookup from `faction_competitions.point_pc_kill_value`,
  `point_npc_kill_value`, and `point_quest_complete_value`.
- Required-point checks from `faction_competitions.req_point`.
- Checked in-memory score application with the resulting required-point flag.
- Zone-score level resolution from `zone_score_levels.required_score`, including the shipped
  level-zero baseline and signed-delta transitions.

All content values come from the W12 typed catalogs. There are no point, threshold, competition,
NPC, quest, faction, or display-name literals in production code.

## Explicitly not implemented

The metadata slice does not prove the following, so this runtime slice does not guess them:

- faction-state or active-competition selection;
- hostile/party/faction eligibility for player kills;
- winner, reset-state, or `point_reset_id` behavior;
- World-to-Zone ownership, relay, replay, or restart persistence;
- quest-act completion for faction-competition objectives;
- special effects 177 (`GiveFactionCompetitionPoint`) and 181 (`ChangeZoneScore`);
- rewards, rank details, `max_score` clamping, or level buffs;
- packet senders. The existing SC packet contracts remain contract-only.

Duplicate NPC/quest link rows are preserved by the metadata loader and collapsed only at the
contribution boundary, so one observed event cannot award the same competition twice.
