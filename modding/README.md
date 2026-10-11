# Modding Shape of Dreams: a methods handbook

This is a technical guide to building mods for **Shape of Dreams**, written
while building the **Last Stand** mod. Paths such as `src/...` and `docs/...`
are Last Stand's, under [`last-stand/`](../last-stand/). It does not walk through Last Stand's
code. It teaches the methods: how to get the game's code out of its DLLs,
how to find the one method that matters among thousands, how to prove
whether a value reaches a player who doesn't have your mod, how to pick a
hook, how to drive the game's own systems, and how to keep a mod alive
through reloads, restarts and real play.

Every chapter works the same way. It explains a technique, applies it step
by step to a real problem met while building Last Stand (with the commands,
the decompiled excerpts, the dead ends and the conclusion), then lists other
places the same technique applies. Once you have the method, the next
problem is yours.

## Who it's for

You know C# basics. You have never modded a game. You may have heard of
dnSpy or ILSpy. Unity, Mirror (the game's networking library), Harmony
(runtime patching) and IL are explained the first time they're needed.

## Chapters

| # | Chapter | What you learn to do |
|---|---|---|
| 01 | [Project setup](01-project-setup.md) | Tell what kind of build the game is, set up a project from the official template, read the mod loader to know when your code runs, load and unload cleanly, find the log |
| 02 | [Reading the game](02-reading-the-game.md) | Decompile with ILSpy or `ilspycmd`, search a few thousand types, scan IL with Cecil when text search misses, read compiler and Mirror generated code, spot decompiler artifacts, record findings |
| 03 | [Hooking with Harmony](03-hooking-with-harmony.md) | Pick the right method to patch (the funnel every path goes through, on the server, once per event), write prefixes, postfixes and finalizers, target private, overloaded, virtual and generated methods, prove a patch applied |
| 04 | [Host-only networking](04-host-only-networking.md) | Trace what a vanilla client shows back to its data, classify every input as synced or local, write synced state from the host, survive shutdown, handle late joiners |
| 05 | [Assets and data](05-assets-and-data.md) | Read the real values stored in prefabs and scenes (at runtime and offline with UnityPy), map assets to C# types, find localization keys, decide between JSON overrides and code |
| 06 | [The game world API](06-game-world-api.md) | Know the managers, actors and entities, create things in the right order, spawn monsters, change stats, add room modifiers live, subscribe to the right events, and find the game's own way of doing anything |
| 07 | [Mod architecture](07-mod-architecture.md) | Split a mod into thin patches, pure rules, game adapters and config, gate it on a mode flag that survives restarts, run a throttled tick loop, never throw, undo everything, survive reloads |
| 08 | [Config, UI and commands](08-config-ui-and-commands.md) | Save settings that keep up with new defaults, extend the stock config screen, inject buttons into game screens, add a game mode entry, register console commands, send chat, show text on vanilla clients' HUD |
| 09 | [Debugging and testing](09-debugging-and-testing.md) | Read `Player.log` and Mono stack traces, write log lines that measure, check formulas against the decompile, make states reachable with test commands, test with vanilla clients, compare builds |
| 10 | [Shipping](10-shipping.md) | Build a clean pack, upload to the Workshop without shipping your source, keep the item id, avoid id conflicts, prepare the art and description, follow the modding rules |
| 11 | [Adding new things](11-adding-new-things.md) | The full procedure from idea to shipped feature, worked end to end twice, plus a field guide: for each kind of feature, where to search, which chapter, and the usual trap |

## How to read it

- New to modding: read 01, 02, 03 and 04 in order. Everything later builds on
  them.
- Adding a feature to an existing mod: start at 11, follow its links back.
- Something works for you but not for your friends: 04.
- A number in the game isn't in the code: 05.

## Ground rules used throughout

- **The decompiled game code is the source of truth.** The official docs are
  short; every claim here was checked in the decompile or in Last Stand's
  source, and short excerpts are quoted. Game updates can change any of it,
  so record the game build next to what you learn (chapter 02).
- **Only the host runs the mod.** Last Stand is host-only: friends join with
  an unmodded game. That constraint shapes most chapters, and it's the
  hardest one to get right (chapter 04).
- **Placeholders:** `<GameFolder>` is the game's install folder,
  `com.example.mymod` stands for your mod's id.

## Official resources

| Resource | Link |
|---|---|
| Mod docs and starter guide | https://github.com/LizardSmoothie/ShapeOfDreamsModDocs |
| API reference (`Dew.Core`) | https://lizardsmoothie.com/sod/moddoc/api/Global.html |
| Modding Terms of Service | https://shape-of-dreams.gitbook.io/shape-of-dreams-modding-terms-of-service/ |
| Shape of Dreams Workshop | https://steamcommunity.com/app/2444750/workshop/ |
