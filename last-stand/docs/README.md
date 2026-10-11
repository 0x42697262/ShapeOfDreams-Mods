# Last Stand design docs

How every part of the mod works, and why. The code has no comments, so these
files are where the design and its reasons live. Each file covers one topic,
so read only the one you need.

For what the mod is and how to play it, see the [project README](../README.md).
For working notes, hard constraints and the in-game test list, see
[`AGENTS.md`](../AGENTS.md).

## Start of a run

| File | Covers |
|---|---|
| [Picking the mode](start/picking-the-mode.md) | The solo button, the lobby dropdown option, and how the mode is remembered across restarts |
| [Arena](start/arena.md) | Leaving camp with no world map, and how the boss room loads cleared, lit and plain |
| [Starting world](start/starting-world.md) | The difficulty setting: which world's vanilla scaling the run uses |
| [Starting gold and dream dust](start/starting-gold-and-dust.md) | The one-time grant in camp |

## Waves and monsters

| File | Covers |
|---|---|
| [Waves](waves/waves.md) | Wave size, clearing, breaks, the alive cap, per-wave scaling, minibosses |
| [How monsters are spawned](waves/spawning.md) | Which game spawner each monster kind goes through |
| [Monsters from other worlds](waves/other-worlds.md) | New monster types every 5 waves |
| [Hunters and hunter zones](waves/hunters.md) | Hunters from wave 100, hunt levels and artillery |
| [Mirage skins](waves/mirage-skins.md) | Skins from wave 30, every monster from 149 |
| [Pain minibosses](waves/pain-minibosses.md) | The damage aura and Corrupted Chaos rewards |
| [Heroic bosses](waves/heroic-bosses.md) | Zone bosses on waves 49, 99…, their scaling and rewards |
| [Environments](waves/environments.md) | Room modifiers that last one wave |
| [Dark urge](waves/dark-urge.md) | The co-op curse that lets heroes hurt each other |
| [Curse to start](waves/curse-to-start.md) | Difficulty option: each wave waits for a curse on everyone |

## Rewards

| File | Covers |
|---|---|
| [Loot](rewards/loot.md) | Monster item drops, rarity tables, dust and gold per tier |
| [Chaos rewards](rewards/chaos-rewards.md) | Rarity of miniboss chaos rewards |
| [Gold prices](rewards/gold-prices.md) | Prices up 5% every wave, compounding, and the optional free gold shrines |

## Shrines

| File | Covers |
|---|---|
| [Edge shrines](shrines/edge-shrines.md) | How the shrines are placed on a ring by the arena's edge |
| [Dream Well](shrines/dream-well.md) | Unlimited upgrades |
| [Altar of Cleansing](shrines/altar-of-cleansing.md) | Unlimited cleansing at vanilla price |
| [Shrine of Ascension](shrines/shrine-of-ascension.md) | Locked until wave 1 is cleared, uses back every wave |
| [Maw of Doom](shrines/maw-of-doom.md) | Fixed 20 gold and 8% risk |
| [Shrine of Destiny](shrines/shrine-of-destiny.md) | Essence trades, 2 claims per player each wave |
| [Hatred Remnant](shrines/hatred-remnant.md) | A curse for a reward, once per player each wave |
| [Curses](shrines/curses.md) | Curse goals rewritten to wave-sized kill counts |
| [Reminiscence of Luck](shrines/reminiscence-of-luck.md) | A free pick each wave |

## HUD

| File | Covers |
|---|---|
| [Wave tracker](hud/wave-tracker.md) | The wave shown in the quest tracker, and why the wave count survives reloads |
| [Top-right label](hud/top-right-label.md) | Why the location label can't show the wave |
| [Modifier panel](hud/modifier-panel.md) | The "Mysterious Space" default panel that replaces ended modifiers |

## Reference

| File | Covers |
|---|---|
| [Settings](reference/settings.md) | The settings window, config versioning and every default |
| [Console commands](reference/console-commands.md) | Every `laststand_*` command |
| [Code layout](reference/code-layout.md) | What each `src/` folder does, and what comes from the shared library |
| [Networking](reference/networking.md) | Host-only design and what clients see |
