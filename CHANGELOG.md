# Changelog

All notable changes to RVRepairVan are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project adheres to Semantic Versioning.

## [2.3.0] - 2026-07-08

### Added
- German translation. The whole questline - journal entries, objectives, all of
  Donna's/Mrs. Ming's/Marco's dialogue and the quest items - is now available in German.
- Automatic language pick: German systems get German, everything else gets English
  (the game itself has no language setting, so the OS language decides).
- A shared `Language` setting (under `DooDesch` in `MelonPreferences.cfg` / the Mod
  Manager UI) to force a language: `auto`, `en` or `de`. Other DooDesch mods will use
  the same setting as they pick up translations.

## [2.2.1] - 2026-07-01

### Fixed
- The quest marker now tracks Marco, Mrs. Ming and Donna as they move around town, instead of
  freezing at wherever the target happened to be standing when the objective updated. Following
  the marker now leads you straight to the right person even while they walk their daily route.

## [2.2.0] - 2026-06-24

### Fixed
- Multiplayer co-op now works end to end and has been verified in a real two-player session:
  both players can play through the entire repair questline together. The previous build shipped
  co-op as experimental; this release fixes the issues that kept it from working.
- The joining player now gets the repair quest from Donna and sees the same quest progress as
  the host, instead of having no dialogue option or a different quest state.
- Both players can see, pick up, carry and hand in the errand items (Ming's crate, Marco's
  package). The quest advances no matter which player makes the delivery, and the pay-instead
  fallback still applies if the item is lost.
- The dead-drop marker now points to a valid drop for the joining player; previously the marker
  could lead to nothing on the client.
- Giving Marco samples to lower the price works for the joining player too, and the discount
  stays in sync for both players.
- Quest items are now reliably removed from the joining player's inventory once they are
  handed in.

## [2.1.0] - 2026-06-24

### Fixed
- Giving Marco a packaged sample no longer fails with "That ain't packaged" while you are
  holding sealed product. The check now reads your inventory instead of the in-hand item,
  which the game empties while a conversation is open.
- Each sample's discount is now based on the value of the single package you hand over,
  rather than scaling with how many were left in the stack.
- The quest marker (the wrench) could end up pointing nowhere after returning to the main
  menu and reloading a save; it now re-points to the right target once the world finishes
  loading.

### Added
- You can now choose which packaged product to give Marco: the dialogue lists each packaged
  product you are carrying together with the discount it would give.
- Marco actually consumes the sample you hand him now (the matching smoke/snort/eat
  animation and effects), instead of just taking it.
- Marco pays more for cleaner product and less for junk: the per-sample discount is scaled
  by quality (Trash 0.6x up to Heavenly 2.0x).
- Experimental host-authoritative co-op support. Single-player is unaffected; co-op has
  only had limited testing, so please report any multiplayer issues.

## [2.0.1] - 2026-06-17

### Added
- Marco now shows a persistent "What can I bring to lower the price?" reminder once you
  have earned his trust (after the package favour). It explains that bringing packaged
  product lowers the bill, so players who skipped the dialogue still know how to keep
  progressing and afford the repair. It appears whenever you are not currently holding
  product to hand over (when you are, the "give a sample" action is shown instead).

## [2.0.0] - 2026-06-17

A ground-up rebuild on S1API with a full story questline, real items, save-bound
persistence and a repair cinematic.

### Added
- "Back on the Road" questline (default `QuestMode = Questline`): the RV is wrecked in
  the intro, then Donna points you to Mrs. Ming, who sends you on a dead-drop crate
  errand, who refers you to Marco the mechanic at the docks.
- Native S1API dialogue: real in-conversation choices and replies (no more floating
  worldspace-only bubbles).
- Referral pricing: Marco charges $50,000 up front, dropping to $10,000 once you tell
  him "Mrs. Ming sent me". Optional packaged-sample discounts cut the bill further,
  down to the repair-price floor.
- Real carried items in dead drops (Ming's Crate, Marco's Package) cloned from in-game
  items so they have a proper inventory icon and in-hand model. Lose one and the NPC
  charges a $500 recovery fee to continue.
- Repair cinematic when you pay: the screen fades to black, repair sounds play, Marco
  grunts as he works, then it fades back and he tells you it is done. Plays in both
  Questline and Simple mode.
- A wrench quest icon in the journal/HUD tracker.
- Settings: `QuestMode`, `BasePriceNoReferral`, `BasePriceWithReferral`,
  `MinSampleDiscount`, `MaxSampleDiscount` (plus the existing `Enabled`, `RepairPrice`).

### Changed
- Persistence is now bound to the game save (via the S1API save system): progress is
  written when the game saves and read on load, so it stays in sync with the world.
  Finishing the quest without saving (or after a crash) correctly reverts, instead of
  leaving the quest stuck or the RV unrepairable.
- "Simple" mode (`QuestMode = Simple`) repairs directly at Marco for the flat
  `RepairPrice` and now also plays the repair cinematic.
- Repair sounds route through the game audio mixer, so they respect the in-game volume
  settings.

### Fixed
- Delivered quest items now correctly leave the inventory.
- The quest reliably starts once the RV is actually wrecked and the intro has passed,
  and never flickers on an intact RV.
