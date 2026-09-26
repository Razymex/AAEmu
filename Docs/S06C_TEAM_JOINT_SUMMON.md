# GF-S06C — the raid-team summon is a move, and the joint follow-ups

S06 shipped the joint and summon flows as consent-only: a summon round was opened, the member
answered, and the answer produced an empty `SCTeamSummonPacket`. Nothing moved. This slice makes the
summon do what its own text says it does, and closes the joint gaps that ship alongside it.

Every claim below was re-verified read-only against the shipped game catalogs (`compact.sqlite3` and
`game.sqlite3`, both opened `?mode=ro`), the shipped client script set, and the client's raw
serializer description. No content database was written at any point.

## 1. The summon moves the member

`ui_texts` carries three rows that together say this is a move and not a handshake:

| id | key | text |
|---|---|---|
| 9649 | `team_summon_question` | "Shall you summon the raid member to your current location?" |
| 9651 | `team_summon_move` | "Your raid leader wants to summon you. Will you **move**?" |
| 894 (`enum_error_messages`) | `SUMMON_CANCEL_NOT_MOVE` | "The summon was cancelled so you cannot **move**." |

`ReplyToSummon` now performs the move. The order is deliberate:

1. consume the round (this is what keeps the exactly-once guarantee from S06),
2. reject any state from the shipped caution list,
3. reject when there is nowhere to land,
4. read the template out of compact, refusing loudly if it is incomplete,
5. check the summon cooldown,
6. ask whether the summoner is carrying a flame at all,
7. move,
8. take the flame,
9. only then answer the frame.

**No refusal costs the summoner an item.** Every check that can refuse runs before the flame is
touched, and the move — which is what the flame pays for — is the last thing that can go wrong before
the spend. A cooldown, a missing flame, a missing template or a refused move all leave the summoner
exactly as they were, and both of those paths are pinned by a `Consumed.Count == 0` assertion. The
one remaining window is a summoner spending their last flame between the availability check and the
take; that costs them the flame to a summon that did happen, which is the intended outcome rather
than a refund the client has already been told about.

**The move is a real server-side teleport, not a blink packet.** `TeleportToUnit` documents why:
sending the blink alone leaves the zone simulating the character at the old position and pulling it
back, which reads as "the summon did nothing". The landing is written through
`Transform.GetLocalFromWorld` → `SetPosition` → `SCBlinkUnitPacket`, and the zone is told through
`WorldIntegration.RelayBlinkToZone` when `ZoneAuthority` is on.

A summon that would cross a zone boundary is refused rather than half-performed; a cross-zone handoff
is a different mechanism and this slice does not model it.

## 2. Cost and timings, all from content

| catalog key | table | shipped value |
|---|---|---|
| `team_summon` | `const_item_types` | item 46130, `bind_id` 2, `max_stack_size` 1000 |
| `team_summon` | `const_skill_types` | skill 39700, `cooldown_time` 1000, `casting_time` 3000, `max_range` 4 |
| `team_summon_channeling` | `const_skill_types` | skill 39701, `channeling_time` 60000, `channeling_buff_id` 23368 |

The flame's own description says it is consumed once the summon succeeds, so the summoner pays and
the member does not.

`TeamJointSummonContent.TryResolve` refuses rather than defaulting: a summon with no item, no
cooldown, no channel or no channel buff is a different feature, and silently running it with a zero
would be worse than not running it. The manager reads content through the `ITeamSummonContent` seam
so a test can decide whether content is present without a container.

**Of those values, only the item id and the skill cooldown are actually used at runtime.** The cast
time, the channel, the channeling skill, the channeling buff and the max range are loaded and
validated — a row missing any of them refuses the whole summon rather than degrading — but none of
them drives behaviour here. The cast is not gated on `casting_time`, the 60 s channel is not
simulated, and the 4 m range is not enforced: the summon is answered by a dialog rather than by a
cast aimed at a target, so there is no range check to make. They are read so that their absence is
caught, not because they take effect.

## 3. The refusal matrix is the shipped list, not an invented one

`ui_texts` key `team_summon_notice` (id 9650, with `team_summon_caution_desc` 10632 carrying the same
body) enumerates exactly seven conditions, in this order: dungeon entry, trial, imprisonment, siege
participation, incapacitated, death, and a worn backpack. `TeamSummonRefusal.ShippedList` mirrors
that list exactly and a test pins it.

The client additionally drops the reply entirely when the member is in combat — its dialog's hide
handler shows `IMPOSSIBLE_COMBAT_STATE` and returns without calling `RequestSummonReply` — so the
server never hears about it. `InCombat` is therefore refused server-side too, but it is **not** in
`ShippedList`, because the caution box does not name it.

Any one condition refuses. Picking a "most important" reason would be a guess the client does not
make. All refusals report `SUMMON_FAIL` (892); a member who cannot receive anything where it stands
reports `SUMMON_NOT_ENOUGH_SPACE` (893).

**Which of the seven a live `Character` can answer today is a separate question.** Dead, in combat,
imprisoned and a worn backpack each have a real predicate and are reported. Dungeon entry, trial,
siege participation and incapacitation have no predicate in the current world model, so
`WorldTeamJointContext.BlockedStates` leaves them clear. The rule still refuses on them; this build
simply cannot be tripped by them yet.

`HasSummonLandingSpace` likewise has nothing to test against and always answers true. It exists as
the seam for 893, not as a claim that room is proven.

## 4. The break ask now expires

The client's joint-dismiss frame counts down `REMAIN_TIME` 120000 ms. A pending ask now expires at
the same instant, so an answer that arrives after the frame has gone cannot dissolve a joint. The
value is a client-UI protocol constant, not a shipped gameplay rate, which is why it is not read
from compact.

## 5. The loot reset

Three shipped `ui_texts` rows promise a reset: 8909 (`raid_joint_warning` — accepting a joint resets
the dice bidding method), 8915 (`raid_jointed` — jointing resets loot acquisition and distribution)
and 8926 (`raid_joint_dismissed` — dissolving resets the same). `ResetLootRules` runs on both
forming and dissolving, and does the two halves that are real server state:

- **Acquisition** — the team's `LootingRule` is rebuilt to its defaults.
- **Dice bid** — every member is put back to `DiceBidRuleKind.Default` through
  `ITeamManager.ChangeDiceBidRule`, which validates the kind, stores it on the member and broadcasts
  `SCDiceBidRuleChangedPacket`. It is called on the injected `ITeamManager`, not on a singleton, and
  the method was added to that interface for this slice.

**The distribution method named by rows 8915 and 8926 is genuinely not reset.** The server's team
model carries a `LootingRule` and a per-member `DiceBidRule`; there is no distribution-method field,
so there is nothing to restore. That half is a real gap and is not papered over.

## 6. The frames are opened by dialog task

`SCNotifyUIMessagePacket` (0x249) is what actually opens a frame. Its serializer confirms `u32
msgType`, `u32 argc`, then `argv0` and `argv1` as length-prefixed strings, each capped at 255 bytes on
read — so both arguments are always written and `argv1` was added to the existing packet, which
previously stopped at `argv0`.

| task | id | handler |
|---|---|---|
| `DLG_TASK_REQUEST_RAID_JOINT` | 88 | `RequestRaidJointDlgTask` |
| `DLG_TASK_RESOPONSE_RAID_JOINT` | 93 | `DialogTeamJointResponse` (60000 ms reply timer) |
| `DLG_TASK_TEAM_SUMMON_SUGGEST` | 94 | `DialogTeamSummonSuggest` (60000 ms show time) |

Each id is cross-checked against the client's exported constant table, and each has the matching
`X2DialogManager:SetHandler` registration in the client's dialog scripts. The raw joint and summon
packets still go out: the dialog opens the frame, the packet carries the roster or the position the
frame then shows. For the summon the position matters — the frame offers a map lookup at the
summoner's coordinates before the member agrees to be moved.

## 7. Refusal messages that were never sent

Five `enum_error_messages` rows exist for this flow and none was being used. They are now on the
path that describes them:

| id | name | sent when |
|---|---|---|
| 1084 | `ALREADY_RAID_JOINTED` | the asker's own raid is already federated |
| 1010 | `TARGET_ALREADY_RAID_JOINTED` | the asker is free but the named target is not |
| 1019 | `RAID_JOINT_FAILED_STATUS_CHANGED` | the raid changed shape between asking and answering |
| 1008 | `RAID_JOINT_ERROR_DISMISSED` | the other raid is gone by the time it answers |
| 1014 | `RAID_JOINTED_CANNOT_ENTER_INSTANCE` | see blockers — the gate is not wired |

The asker's own state is checked before the target's: a raid already inside a federation cannot ask
at all, so that is the more fundamental reason and it is reported first.

`1014` is the reason a jointed raid may not enter an instance. **The gate is not implemented here** —
nothing in the instance-entry path consults joint state, and this slice does not add that hook.

## Corrections to S06

**Mode 3 travels client → server.** The client's target-context handler reaches the server through
`X2Team:JointInfoReq(TEAM_JOINT_REQUEST, arg)`, so `TEAM_JOINT_REQUEST` is a request *origin*, not a
server prompt. It is renamed `ContextRequest` and moved into `IsRequestMode`. (S06's
`IsRequestMode` did not actually include it — the constant was simply misnamed and mis-documented as
server-originated, and the request path sent it back to the asker. Both the name and the comment were
wrong; the wire byte is unaffected.) The request frame is now opened by its dialog task, and the
info packet that follows carries the roster.

**`SCTeamJointPacket.packetMode` — position known, value not.** The client's serializer carries the
field as a confirmed one-byte value at the fifth of five body fields, object offset 36, fixed 21-byte
body. The derived `_packet_structs_*.json` also lists the field, but with object offset 0, because it
hoists a guarded read to the top level without carrying the offset through. The raw description is
authoritative. The doc comment now separates the two. The *value* stays unresolved and
`PacketModeUnresolved = 0` stays pinned: the mode table was not recovered, and the consuming handler
was never decompiled. No value is invented here.

### A note on the derived packet summaries

The `_packet_structs_*.json` files are derived from the raw `ir`, and the derivation is lossy in two
ways that both matter when a wire is being matched:

- **Signedness is lost.** `SCTeamJointPacket.type` is `s64` in the raw description and reported as
  `u64` in the derived one; `jointOrder` is `s32` and reported as `u32`; `SCShowCommonFarmPacket.count`
  is `s32` and reported as `u32`. A writer following the derived file would emit a different byte
  pattern for any negative value.
- **Guarded-field offsets are mangled.** A field that only appears inside an `if` in the raw `ir` is
  hoisted to the top level with its object offset zeroed — `packetMode` is reported at offset 0 where
  the raw says 36.

So the derived file does not drop fields, and a missing field in it means nothing. It is recoverable
through known transformations, but the raw `ir` is the authority and every shape in this slice was
taken from it.

## Blockers — not implemented, and why

1. **The channelling follow-through.** Skill 39701's 60000 ms channel and buff 23368 are loaded and
   validated, but the buff's effect was not recovered, so the channel is not simulated. The member
   moves at the end of the cast, not after a 60 s channel the server is not tracking.
2. **The summon special effect is bound, but unparameterised.** The chain is `skill_effects`
   (skill 39700) -> `effects` 72228 -> `actual_id` 37559 -> `special_effects` 37559 ->
   `special_effect_type_id` 171 (`team_summon`). The type-171 effect *is* bound to the actual summon
   skill, through the `effects` indirection table. (An earlier draft of this document claimed the
   reference resolved to nothing; it resolves, one indirection further than the first query went.)
   What it carries is why nothing is implemented from it: `value1` through `value7` are all zero, so
   it names no target, no position, no distance and no buff. It confirms the concept is real content
   and that the summon skill is wired to it; it supplies no parameter to apply, and nothing was
   inferred from the zeros.
3. **Four of the seven caution states have no server predicate.** Dungeon entry, trial, siege
   participation and incapacitation. The rule refuses them; this build cannot be tripped by them.
4. **Landing space is unprovable.** `HasSummonLandingSpace` always answers true.
5. **The loot *distribution* method named by rows 8915 and 8926 has no server field**, so that half of
   the promised reset is not performed. The acquisition method and the dice bid are reset.
6. **The joint break capability push is not implemented.** The client's raid frame does not open a
   dialog for `TEAM_JOINT_BREAK`; with `requester == true` it only sets
   `dismissRaidBtn:Enable(enable)`, where that enable is `IsJointLeader() and
   `GetCanJointBreakAck()`. `GetCanJointBreakAck` is a distinct client binding, but **no packet in
   the raw serializer carries a `(requester, enable)` pair** -- `SCTeamJointBreakPacket` is
   `(ask, accept)`, a different shape. (The derived summaries cannot settle this either way: they lose
   signedness and zero guarded-field offsets, so absence from them is not evidence, and absence from
   the raw `ir` is.) Rather than guess a wire, nothing claims to push it, and the capability is left
   to the client to derive from the joint header it already receives.
7. **`RAID_JOINTED_CANNOT_ENTER_INSTANCE` is not enforced.** The instance-entry path does not
   consult joint state.
8. **Cross-zone summon is refused**, not performed.
9. **The cast time, the 60 s channel, the channeling buff and the 4 m range are loaded and validated
   but unused.** Only the item id and the skill cooldown drive behaviour.
10. **No live run.** The move, the dialogs, the break expiry and the loot reset have not been walked
    through a running client; the unit tests drive the manager through the fake context only.

## Tests

`TeamJointSummonTeleportTests` covers the destination the member lands on, the flame being spent
once, cooldown, missing content, a refused world move, no landing space, the exactly-once property
across a repeated accept, a decline, each of the eight refusal states individually, the shipped
seven-element caution list, the refusal log naming combat, the 120 s break expiry on both sides of
the boundary, the loot reset on forming and dissolving, and each dialog task. Both refusal paths that
could spend the flame assert `Consumed.Count == 0`, so the check-before-spend order cannot silently
regress.

The existing `TeamJointFlowTests` mode assertions and the already-jointed refusal were updated: the
first encoded the wrong direction for mode 3, the second expected a generic message where a specific
one now applies.

`WorldTeamJointContextLootTests` is separate and exists because the flow tests cannot reach the dice
bid. Those tests drive the manager through a fake `ITeamJointContext`, whose `ResetLootRules` is a
recorder; the dice bid is server state reached through `ITeamManager.ChangeDiceBidRule`, and that loop
lives in the live context. Without this class the promise made by the shipped jointed and dissolved
texts would be untested and would still pass if the loop were deleted. It builds a real `Team`, a
real `Character` per member and a recording `ITeamManager`, then asserts that every member of the raid
— not just its leader — is put back to `DiceBidRuleKind.Default` as an explicit (not idle-state)
change, that the acquisition rule returns to a freshly built one, that a missing raid is a no-op, and
that the fixed-size member array's empty tail is skipped.

Every new behaviour was mutation-checked by removing it and confirming the tests fail:

| mutation | failing tests |
|---|---|
| never call the teleport | 4 |
| spend the flame before the cooldown check | 3 |
| spend the flame before the move | 3 |
| skip the refusal matrix | 9 |
| drop the loot reset | 3 |
| delete the dice-bid loop | 2 |
| zero the dialog task ids | 4 |
| drop the break-ask expiry purge | 1 |

The two spend-order mutations each fail `Accept_WhenTheWorldRefusesTheMoveReportsFailure`, which is
the test that pins check-before-spend. The teleport mutation fails only the four tests that reach the
teleport: the eight refusal cases refuse before it, so they are unaffected, which is why the count
is 4 rather than the number of cases in the file.
