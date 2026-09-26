# W03B instance-reward bonus counts

W03B is stacked on W03A delivery commit `8b7ab04279ed2f9e965bad693455dfc565edd3ba`. It keeps W03A's difficulty-only selection,
soldier-rank rejection, in-memory copy marker, and exactly-once claim guard unchanged.

## What the content proves

`instance_reward_bonus_counts` joins an authored `instance_rewards.id` to a real `buffs.id`
and carries a positive count. The shipped catalog also contains legacy rows whose reward id is
absent from `instance_rewards`. Those rows are rejected loudly and reported through
`IndunGameData.InstanceRewardBonusDiagnostics`; they are never attached to another reward.

A row is rejected during content load when it has a zero catalog id, a non-positive count, a
duplicate id, a duplicate reward/buff pair, or no matching `buffs` row. An absent reward is
reported as an orphan because the shipped catalog contains such rows and refusing to boot on
them would make the runtime content unusable.

## Transactional delivery

The same reward selection that builds the mail also chooses the bonus rows for the selected
reward ids. For every recipient, the claim, mail and typed bonus grants are written on one
MySQL transaction:

- `indun_reward_claims` keeps the W03A `INSERT IGNORE` exactly-once guard.
- `indun_reward_bonus_grants` stores reward id, buff id and the authored count in that same
  transaction.
- A retry after an ambiguous commit resolves the durable claim and the exact ordered bonus-grant rows
  together. A mismatched set stays unknown instead of publishing a second delivery.

The grant ledger is a durable record of the content-backed bonus count. W03B does **not** mutate
character buffs: the shipped data proves the association and count, but not a player-visible buff
application, and several shipped buffs are permanent single-stack account benefits.

## Runtime reachability in shipped 10.0.2.13 content

The grant path is **unreachable with the shipped content**. Its current runtime effect is zero.

- All 496 loadable bonus rows belong to non-difficulty reward kinds: 271 `team_rank` rows and
  75 each for `win_team`, `lose_team`, and `draw_team`.
- Those rows span 13 instances. Every one of those 13 instances has zero
  `instance_reward_mail_texts` rows, zero `instance_difficult_infos` rows, and no
  `IndunActionSendMailReward` action wired to the relevant instance and reward kind.
- The only difficulty-backed instance carries kind 6 only and has zero bonus rows.
- No shipped instance has both mail text and bonus rows.

W03A deliberately accepts only the shipped difficulty selection source, so none of the
non-difficulty bonus rows can reach the W03B transaction in the current catalog. A follow-up
selection slice must supply both the missing mail text and evidence for the applicable selection
value. It must not infer or backfill either from the bonus rows.

### Advisory B: zero-amount reward interaction

All eight shipped `instance_rewards` rows with `reward_amount = 0` are bonus-backed. W03A's
non-positive-amount guard refuses those selections before a mail or bonus grant is created. This
becomes a real interaction as soon as any affected instance gains both mail text and the required
difficulty evidence; the follow-up must resolve the zero-amount semantics explicitly rather than
bypassing the existing guard.

## Refused scope

- No soldier-rank selection was added. W03A's explicit rejection remains.
- No restart rehydration was invented. The server has no durable dungeon/run model from which to
  supply the caller-owned `RewardRunId`; W03A's run id remains a live-copy identity with an
  explicit caller seam for a future persistence model.
- No character-buff application was inferred from a bonus count.

## Verification

Tests cover typed selection, orphan/duplicate/non-positive rejection, transactional bonus
persistence, ambiguous-commit retry, rollback, and the unchanged soldier-rank refusal. The
runtime-content check is opt-in and verifies the real compact catalog, including its reported
legacy orphan rows.
