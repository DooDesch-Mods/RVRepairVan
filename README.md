# RVRepairVan - Get Your RV Back on the Road

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de](https://support.doodesch.de).

> Your RV got blown up in the intro? Now you can actually fix it. A proper tracked side
> quest sends you from the motel manager to Mrs. Ming to Marco the mechanic, with real
> errands, referral haggling, and a little repair cinematic at the end. Built on
> [S1API](https://github.com/ifBars/S1API).

![Version](https://img.shields.io/badge/version-2.4.0-blue)
![Game](https://img.shields.io/badge/game-Schedule%20I-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![S1API](https://img.shields.io/badge/S1API-required-orange)
![Status](https://img.shields.io/badge/status-working-brightgreen)

## Features

- **"Back on the Road" questline** (default). After the RV is wrecked during the intro,
  the quest starts on its own and points you to the motel manager (Donna), who sends you
  to **Mrs. Ming**, who has you fetch a crate from a **dead drop** before she refers you
  to **Marco** the mechanic at the docks.
- **Native dialogue** via S1API: real in-conversation choices and replies, not floating
  one-line bubbles, and it shows up in the journal and the on-screen HUD tracker.
- **Referral pricing.** Marco quotes **$50,000** up front; tell him *"Mrs. Ming sent me"*
  and it drops to **$10,000**. Optionally earn his trust (a second dead-drop pickup) to
  hand over **packaged product samples** that shave the bill down further, to the floor.
- **Real carried items.** The crate and package are actual items you pick up and carry
  (with a proper icon and in-hand model). Lose one and the NPC charges a **$500** recovery
  fee to continue.
- **Repair cinematic.** When you pay, the screen fades to black, you hear Marco work on
  it (and grumble), then it fades back and he tells you she is ready. Plays in both modes.
- **Save-bound persistence.** Progress and the repaired RV are stored inside the game
  save, so they stay in sync with the world: finishing without saving (or after a crash)
  correctly reverts instead of leaving you stuck.
- **Simple mode** (`QuestMode = Simple`): skip the story and just talk to Marco, pay the
  flat repair price, and get the cinematic.
- **Configurable** prices and discounts, read live - change them in settings and Marco's
  offer updates without a restart.
- **English and German - and your language.** The questline picks German automatically
  on a German system (the game has no language setting of its own), English everywhere
  else, and you can add or fix translations yourself with a simple JSON file - no
  modding needed. See Configuration.

## Requirements

| Component | Version / Source |
|-----------|------------------|
| Schedule I | `0.4.5f2` (IL2CPP, current Steam public build) |
| MelonLoader | `0.7.3+` |
| S1API | [ifBars/S1API_Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) (dialogue, items, dead drops, money, save system) |
| Mod Manager & Phone App | [Prowiler, Nexus mods/397](https://www.nexusmods.com/schedule1/mods/397) - optional, for the in-game settings UI |

## Installation

### Recommended: a Thunderstore mod manager

Install with a mod manager (r2modman / Gale) from the Schedule I community; the
dependencies (MelonLoader, S1API) are pulled in automatically.

### Manual

1. Install **MelonLoader 0.7.3** for Schedule I.
2. Install **S1API** (its DLLs go in `Mods/` and `Plugins/` per its own instructions).
3. Drop **`RVRepairVan.dll`** into your Schedule I `Mods/` folder.
4. (Optional) Install **Mod Manager & Phone App** for the in-game settings UI.

## Configuration

Settings live in the **Mod Manager & Phone App** UI in-game, or in
`UserData/MelonPreferences.cfg` under `RVRepairVan_01_Main`.

| Setting | Default | What it does |
|---|---|---|
| `Enabled` | `true` | Master on/off for the repair feature. |
| `QuestMode` | `Questline` | `Questline` = the full Donna -> Ming -> Marco story. `Simple` = just talk to Marco and pay. |
| `RepairPrice` | `1500` | Simple-mode price, and the price floor the questline can never go below. |
| `BasePriceNoReferral` | `50000` | Questline price if you walk straight to Marco. |
| `BasePriceWithReferral` | `10000` | Questline price after you drop Mrs. Ming's name. |
| `MinSampleDiscount` | `100` | Smallest price cut a single packaged sample can give. |
| `MaxSampleDiscount` | `500` | Largest price cut a single packaged sample can give. |

The language lives in its own shared section (`DooDesch` - all DooDesch mods with
translations read the same value):

| Setting | Default | What it does |
|---|---|---|
| `Language` | `auto` | `auto` = follow the OS language (German -> German, otherwise English), or force a code like `en` / `de` / `fr`. Takes effect after a restart. |

### Add your own translation

Want the quest in a language the mod doesn't ship, or prefer different wording? After
one game start you'll find `UserData/DooDesch/Localization/RVRepairVan/_template.en.json`
listing every translatable line. Copy it, rename the copy to your language code (e.g.
`fr.json`), translate the values, restart. Your file wins over the built-in texts.
Step-by-step guide: the [ScheduleOne-L10n wiki](https://github.com/DooDesch-Mods/ScheduleOne-L10n/wiki).

Debug-only toggles (Destroy RV, Add $10,000, Dump state, Play repair cinematic) exist only
in development builds and are not shipped in the release.

## Usage

1. Play through the intro until the RV is wrecked and the early quests have begun.
2. The **"Back on the Road"** quest appears and points you to the **motel manager**.
3. Follow it: Donna -> **Mrs. Ming** (accept her errand, grab the crate from the dead drop,
   bring it back) -> **Marco** at the body shop.
4. Ask Marco to fix the RV, drop Ming's name to cut the price, optionally do his pickup and
   hand over packaged samples for further discounts.
5. **Pay** - enjoy the repair cinematic - then go **check on the RV**.

Prefer no story? Set `QuestMode = Simple` and just pay Marco directly.

## Compatibility

- Disable other RV-repair or Marco-dialogue mods to avoid duplicate or conflicting choices.
- IL2CPP build only (current Steam public branch). A Mono build for the alternate branch is
  planned.

## Credits

- **DooDesch** - mod author.
- **[ifBars/S1API](https://github.com/ifBars/S1API)** - the modding API this is built on.
- **Prowiler** - Mod Manager & Phone App (in-game settings UI).

## License

Provided as-is under the [MIT License](LICENSE.md).
