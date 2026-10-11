# 10. Packing and the Workshop

A mod that works in your `Mods/` folder is not yet a release. This chapter
reads the game's own uploader to learn exactly what it sends to Steam, then
builds a release folder that contains only what the loader needs. It also
covers the `about/` assets, versioning and the rules that apply to a public
mod. The method is the same as everywhere else in this guide: find the code
path that does the work, read it, and design around what it really does
rather than what the button label says.

Prerequisites: a mod that loads (`01-project-setup.md`) and a way to read
the decompiled `Dew.Core` and `Dew.UI` (`02-reading-the-game.md`).

---

## 1. Read the uploader before you trust it

**Technique: trace a UI button down to the platform API.** Upload code is
rarely documented in detail, and the details decide what ends up public.
Start from the platform call, because that's a fixed name you can search for,
then walk up to the button.

1. Search the decompile for the Steamworks.NET namespace calls:
   ```sh
   grep -rln "SetItemContent\|IdConflict\|publishedfileid" Dew.Core Dew.UI
   ```
   Three files match: `Dew.Core/DewMod.cs`, `Dew.Core/ModItemState.cs`,
   `Dew.UI/UI_ModManager.cs`.
2. In `UI_ModManager.cs`, `Awake` wires `uploadButton.onClick` to
   `UploadMod`. `UploadMod` refuses any mod whose `state != ModItemState.Ok`
   (message key `ModManager_Message_Upload_ModStateInvalid`), asks for
   confirmation, and calls `UploadMod_Imp`.
3. `UploadMod_Imp` creates the item if needed, then updates it:
   ```csharp
   if (mod.publishedFileId == 0L)
   {
       isNew = true;
       createRes = await DewMod.CreateItem();
       ...
       string path = Path.Join(mod.path, "about", "publishedfileid.txt");
       mod.publishedFileId = createRes.m_nPublishedFileId.m_PublishedFileId;
       await File.WriteAllTextAsync(path, mod.publishedFileId.ToString());
   }
   ...
   SubmitItemUpdateResult_t r = await DewMod.UpdateItem(id, mod, isNew);
   ...
   SteamFriends.ActivateGameOverlayToWebPage($"steam://url/CommunityFilePage/{mod.publishedFileId}");
   ```
4. `DewMod.UpdateItem` is the whole payload:
   ```csharp
   UGCUpdateHandle_t handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), id);
   SteamUGC.SetItemTitle(handle, item.metadata.name);
   SteamUGC.SetItemDescription(handle, item.description);
   SteamUGC.SetItemContent(handle, item.path);
   if (!string.IsNullOrEmpty(item.previewPath))
       SteamUGC.SetItemPreview(handle, item.previewPath);
   if (isNew)
       SteamUGC.SetItemVisibility(handle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
   SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(handle, null);
   ```
5. Follow each argument back to where `ModItem` is filled
   (`DewMod.AddMod`): `item.path = Path.GetFullPath(modPath)`, the mod's
   folder; `item.description` is the raw text of `about/description.txt`
   (or `"No description provided."`); `item.previewPath` is
   `about/preview.png` if it exists; the title is `metadata.name`.

What this tells you:

| Call | Source | Consequence |
|---|---|---|
| `SetItemContent(item.path)` | the whole mod folder, recursively | Everything in the folder ships: `.git/`, `src/`, `obj/`, `bin/Debug`, notes, screenshots. |
| `SetItemTitle` | `metadata.name` | The Workshop title is reset on every upload. Edit it in `metadata.json`, not on the web page. |
| `SetItemDescription` | `about/description.txt`, raw | The web description is overwritten on every upload. Same rule. |
| `SetItemPreview` | `about/preview.png` | Only set when the file exists. |
| `SetItemVisibility(Private)` | only when `isNew` | A new item is Private (Steam's web page calls this "Hidden"). |
| `SubmitItemUpdate(handle, null)` | no change note | Add change notes on the item's web page. |

No tags are set, and nothing filters the folder.

**Failure path.** In the `catch` of `UploadMod_Imp`, if the item was created
in this call (`createRes.m_eResult == k_EResultOK`) the game calls
`DewMod.DeleteItem` and deletes `about/publishedfileid.txt` again, so a failed
first upload leaves nothing behind. `k_EResultFail` and
`k_EResultAccessDenied` show the hint `ModManager_ItemUploadFailed_Hint_NotMine`
(the id belongs to someone else's item); `k_EResultItemDeleted` shows
`..._Hint_Deleted`.

**Docs versus code.** The official docs say uploaded mods are set to
**Hidden**, and that "updating a mod will also set the visibility to hidden."
In the code above only a new item gets a visibility call. Treat the docs as
the safe assumption: after every upload, open the item page (the game opens
it in the overlay) and check the visibility before announcing a release.

**The upload button** appears in the mod manager when Developer Mode is on
(official docs). The label comes from
`ModManager_UploadToWorkshop_Update` when `publishedFileId != 0`, otherwise
`..._New`, so the label tells you which item the game will write to.

### Where else this applies

- Finding what a save or config writer puts on disk (`DewSave`,
  `ModBehaviour.GetModConfigFilePath`).
- Finding which lobby attributes a host advertises
  (`LobbyServiceProvider.SetLobbyAttribute`, see section 6).
- Any "the button does X" claim in a doc: find the platform call and read
  its arguments.

### Worked example: the missing change note

Reading the arguments in step 3 shows one that's always empty:

```csharp
SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(handle, null);
```

The second parameter of `SubmitItemUpdate` is the change note, the text
on the item's Change Notes page. The game never sends one, so every
update shows up with no notes unless you paste them on the page by hand.
Your mod is loaded while the mod manager uploads it, so it can patch the
uploader:

1. **Find a hook that knows which mod is uploading.** `SubmitItemUpdate`
   only gets a handle. `DewMod.UpdateItem(PublishedFileId_t id, ModItem
   item, bool isNew)` gets the `ModItem`, with `item.path` (the folder
   being uploaded) and `item.metadata.id`.
2. **Check the timing.** `UpdateItem` is `async`, but everything up to its
   first `await` runs inside the call, and `SubmitItemUpdate` comes before
   it. So a prefix on `UpdateItem` can store the note, a prefix on
   `SubmitItemUpdate` can swap it in for the `null`, and a finalizer on
   `UpdateItem` clears it, all within one call.
3. **Only touch your own mod.** Compare `item.metadata.id` with
   `ModBehaviour.mod.metadata.id`. Without that check, your patch would
   attach your note to someone else's upload.
4. **Read the note from the uploaded folder.** Last Stand reads
   `about/changenote.txt` from `item.path`, so the pack script copies it
   into the pack. Steam renders BBCode in change notes.

```csharp
[HarmonyPatch(typeof(DewMod), nameof(DewMod.UpdateItem))]
internal static class WorkshopUploadPatch
{
	[HarmonyPrefix]
	private static void Prefix(ModItem item)
	{
		WorkshopChangeNote.Begin(item);
	}

	[HarmonyFinalizer]
	private static void Finalizer()
	{
		WorkshopChangeNote.End();
	}
}

[HarmonyPatch(typeof(SteamUGC), nameof(SteamUGC.SubmitItemUpdate))]
internal static class WorkshopSubmitPatch
{
	[HarmonyPrefix]
	private static void Prefix(ref string pchChangeNote)
	{
		WorkshopChangeNote.Apply(ref pchChangeNote);
	}
}
```

`SteamUGC` lives in `com.rlabrecque.steamworks.net.dll` (Steamworks.NET)
in the `Managed` folder; reference it with `Private` set to `False`, like
the game's other assemblies. These patches must not check your game
mode: uploads happen from the title screen.

---

## 2. Build a clean pack

**Technique: derive the minimum file set from the loader, then generate it.**
Since the uploader sends the folder as is, the release folder must be
built, not edited by hand.

### What the loader reads

From `DewMod.AddMod` and `DewMod.Load`:

| Path in the mod folder | Required | Read by |
|---|---|---|
| `about/metadata.json` | yes; without it the folder is skipped | `AddMod` (`JsonUtility.FromJson<ModMetadata>`) |
| files matching `metadata.assemblies` | yes for code mods | `AddMod` fills `assemblyPaths`; `Load` loads every one ending in `.dll` |
| `overrides/` | only for JSON overrides | `Load`: `Path.Join(modItem.path, "overrides")`, also sets `isAlteringGameplay` |
| `about/icon.png`, `about/preview.png`, `about/description.txt` | no | mod manager list, details pane, upload |
| `about/publishedfileid.txt` | no | `AddMod`, upload (section 3) |

A folder that contains `.template.config/` is skipped, which is how the
official `ModTemplate` stays out of the list.

`ModMetadata` has exactly five fields: `id`, `name`, `author`, `modVer`,
`assemblies`. `AddMod` marks the mod `InvalidMetadata` if `id`, `name` or
`author` is missing, or if parsing throws.

**The `assemblies` patterns.** `AddMod` lists every file under the mod folder
(`SearchOption.AllDirectories`), turns each into a relative forward-slash
path, and keeps those matching any pattern with `EqualsWildcard`
(`Dew.Core/WildcardMatch.cs`). In that matcher `?` is one character and `*`
is any run of characters, **including `/`**, and matching is case-sensitive.
So a pattern like `*.dll` in a dev folder matches
`bin/Debug/netstandard2.1/MyMod.dll`, `bin/Release/...` and anything under
`obj/`, and `Load` loads all of them. Use exact paths.

`Load` reads each DLL as bytes, renames the assembly with
`DateTime.Now.Ticks` (Mono.Cecil) and calls `Assembly.Load(byte[])`. No
`.pdb` is read, so a release needs only the `.dll`.

### The two `metadata.json` files

The template's `metadata.json` points at the Debug output so the dev folder
loads straight from the build:

```json
{
  "id": "com.example.mymod",
  "name": "My Mod",
  "author": "Example",
  "modVer": "0.1.0",
  "supportedGameVer": [ "*" ],
  "assemblies": [ "bin/Debug/netstandard2.1/MyMod.dll" ]
}
```

The pack holds the Release DLL at its root, so its copy must say
`"assemblies": [ "MyMod.dll" ]`. Generate that copy; don't keep two
hand-edited files.

### Last Stand's `pack.sh`, step by step

Last Stand's `pack.sh` turns the Release build into `dist/LastStand/`. The
same steps work for any code mod:

1. **Require the Release build.** Fail with a message if
   `bin/Release/netstandard2.1/<Name>.dll` is missing
   (`dotnet build -c Release`). Packing a stale or Debug DLL is the
   most likely mistake.
2. **Save the Workshop id before deleting anything.** If
   `dist/<Name>/about/publishedfileid.txt` exists and differs from the
   source's `about/publishedfileid.txt` (`cmp -s`), copy it back and print
   "Commit it." This must run *before* step 3: the first upload writes the
   id into the pack folder (section 3), and wiping the pack first would lose
   it.
3. **Recreate the pack from scratch.** `rm -rf dist/<Name>`, then
   `mkdir -p dist/<Name>/about`. Nothing from an earlier pack survives.
4. **Copy the allow-list.** The DLL to the root; `icon.png`, `preview.png`,
   `description.txt` to `about/`; `publishedfileid.txt` if the source has
   one. An allow-list (copy these files) is safer than a deny-list (copy
   everything except these): a new file in the repo can't leak into a
   release by accident. If the mod has `overrides/`, add it here.
5. **Rewrite `assemblies`.**
   ```sh
   sed -E 's|"assemblies": *\[[^]]*\]|"assemblies": [ "'"$name"'.dll" ]|' \
     "$root/about/metadata.json" > "$out/about/metadata.json"
   grep -q "\"$name.dll\"" "$out/about/metadata.json" || { echo "..." >&2; exit 1; }
   ```
   The `grep` is the check: if the source JSON is ever reformatted so the
   regex misses, the script stops instead of shipping a pack that points
   at `bin/Debug/...` and loads nothing.
6. **Print the result.** `find . -type f | sort` inside the pack. For Last
   Stand it's six files:
   ```
   LastStand.dll
   about/description.txt
   about/icon.png
   about/metadata.json
   about/preview.png
   about/publishedfileid.txt
   ```

`.gitignore` covers `bin/`, `obj/` and `dist/`, so the pack is never committed,
while `about/` (including `publishedfileid.txt`) is.

### Where else this applies

- Any generated artifact that a tool consumes as is: build it from an
  allow-list into a fresh folder, then assert the one property that matters
  (here, that `assemblies` points at the packed DLL).
- Debugging "mod loads twice" or "old code still runs": list
  `assemblyPaths` matches by hand with the wildcard rules above.

---

## 3. `publishedfileid.txt`: the item's identity

The link between a folder and a Workshop item is one text file holding the
item number.

- **Written** by `UploadMod_Imp` into `<mod.path>/about/` right after
  `CreateItem` succeeds, so it lands in the folder you uploaded from (the
  pack, not your source).
- **Read** by `AddMod`:
  ```csharp
  string path6 = Path.Join(modPath, "about", "publishedfileid.txt");
  if (File.Exists(path6) && item.publishedFileId == 0L)
      item.publishedFileId = ulong.Parse(File.ReadAllText(path6));
  ```
  A Steam-subscribed copy already gets its id from Steam
  (`AddSteamSubscribedMods` passes `array[i].m_PublishedFileId`), so the file
  matters only for local folders.
- **Used** by the upload: a non-zero id means "update this item", zero means
  "create a new one". The button label shows which.

Consequences:

1. **Commit it.** If the file is lost (a clean checkout, a fresh pack
   without the copy-back step), the next upload silently creates a second
   Workshop item. Subscribers stay on the old one.
2. **Copy it back after the first upload.** `pack.sh` does this on its next
   run (section 2, step 2); run it once right after the first upload so the
   id is never only in `dist/`.
3. **`ulong.Parse` is strict.** Write only the number. A trailing newline is
   fine for `ulong.Parse`; any other text makes `AddMod` throw and the mod
   shows as `InvalidMetadata`.
4. **The file ships inside the item.** Subscribers get it too. It's
   harmless for them, since their copy already has the id from Steam.

---

## 4. `IdConflict`: one folder per id

**Technique: read the discovery loop to learn which copy wins.** When two
folders claim the same mod, the scan order decides which one loads and
which one uploads.

`DewMod.Refresh` builds `allMods` in this order:

1. `GetAllLocalModDirectories()`: the `-moddir <dir>` launch option if it
   names an existing folder (then **only** that folder), otherwise `../Mods`
   and `Mods`, relative to the game's working directory.
2. `AddAllModsInDirectory` calls `AddMod` for each subfolder, in
   `Directory.GetDirectories` order.
3. `AddSteamSubscribedMods`: every subscribed, installed Workshop item.

The end of `AddMod`:

```csharp
if (item.state == ModItemState.Ok &&
    allMods.Find((ModItem m) => m.metadata.id == item.metadata.id && m != item) != null)
{
    item.state = ModItemState.IdConflict;
}
```

So the **first** folder found with an id is `Ok`, and every later one is
`IdConflict`. `DewMod.Load(id)` takes the first `Ok` item with that id, and
`UploadMod` refuses any item that isn't `Ok`.

What goes wrong:

- **Dev folder and pack both in `Mods/`.** Whichever folder the directory
  listing returns first wins. If it's the dev folder, the manager offers
  *that* one for upload, and the upload sends your sources.
- **Subscribed to your own item while the dev folder is in `Mods/`.** Local
  folders are scanned first, so the dev folder loads and the Workshop copy
  shows as a conflict. To test the published build, take the dev folder
  out.

The active-mods list (`DewSave.platformSettings.activeMods`) stores **ids**,
and config files live under
`<persistentDataPath>/QuickSave/Mods/<id>/` (`ModBehaviour.GetModConfigFilePath`).
Swapping folders that share an id keeps the mod enabled and keeps its
settings. Changing the id loses both, so pick it once.

### Uploading from the pack

1. Build Release and run the pack script.
2. Close the game.
3. Move the dev folder out of `<GameFolder>/Mods/` (renaming it inside
   `Mods/` is not enough: it's still scanned).
4. Copy the pack folder to `<GameFolder>/Mods/<Name>/`.
   Alternative: launch with `-moddir <path-to-dist>`; the game then scans
   only that folder and ignores `Mods/` entirely.
5. Start the game, open **Mods** (Developer Mode on), select the mod, and
   check that the details pane shows the expected id and that no entry
   shows a conflict.
6. Click Upload and confirm. The overlay opens the item page.
7. On the item page: check visibility and set Public when ready. Add a
   change note by hand unless your mod attaches one (section 1).
8. If this was the first upload, copy
   `Mods/<Name>/about/publishedfileid.txt` back to the source's `about/`
   (or copy the folder back to `dist/` and rerun the pack script) and
   commit it.
9. Close the game, remove the pack folder from `Mods/`, put the dev folder
   back (or drop `-moddir`).

---

## 5. The `about/` assets

| File | In game | On the Workshop |
|---|---|---|
| `icon.png` | mod manager list (`Texture2D.LoadImage`) | not sent separately; it's inside the content |
| `preview.png` | details pane, scaled to 300 px high at its aspect ratio | the item's preview image (`SetItemPreview`) |
| `description.txt` | details pane, after `ConvertBBToRichText` | the item description, raw BBCode |
| `metadata.json` `name` | list and details | the item title |

**Sizes.** The game enforces no size. The template ships a 128×128 icon and
a 636×358 preview. Last Stand uses a 512×512 icon and a 1024×576 (16:9)
preview. Steam rejects a preview of 1 MB or more (`SubmitItemUpdate` returns
`k_EResultLimitExceeded`), which reaches you as the generic upload error.
Last Stand's preview is 628,567 bytes.

**Text limits** (Steamworks.NET `Constants`, in the game's
`com.rlabrecque.steamworks.net.dll`): `k_cchPublishedDocumentTitleMax = 129`,
`k_cchPublishedDocumentDescriptionMax = 8000`. Keep `description.txt` under
8000 characters.

**One BBCode file, two renderers.** Steam renders the full Workshop BBCode.
The in-game pane goes through `DewMod.ConvertBBToRichText`, a chain of
regexes into TextMeshPro rich text:

- `[h1]`–`[h6]` become white, bold, scaled text (150% down to 95%).
- `[b]`, `[i]`, `[u]`, `[strike]` become their TMP tags. Ordered and
  unordered lists are converted.
- `[url=...]text[/url]` keeps only the text. `[url]x[/url]` and
  `[spoiler]` keep the inner text.
- `[hr]`, tables and `[img]` are removed.
- Then `\[/?[^\]]*\]` strips every remaining tag.

Write the description with headings, bold and lists, and it reads well in
both places. The template's own `description.txt` warns that tables, links
and images don't survive in game.

### Generating the art reproducibly

Binary art that only exists as a PNG can't be reviewed or changed.
Last Stand keeps the source of both images in `art/` and renders them with
one script:

```sh
python3 icon/icon.py
rsvg-convert -w 512 -h 512 icon/icon.svg -o ../about/icon.png
python3 preview/preview.py
rsvg-convert -w 1024 -h 576 preview/preview.svg \
  | magick - -alpha off -define png:compression-level=9 ../about/preview.png
rm preview/preview.svg
```

- The Python scripts write SVG from code. Any randomness is seeded
  (`random.seed(7)` in `icon.py`), so the output is fixed.
- Screenshots are cut by a separate `crop.sh`
  (`magick raw/x.png -alpha off -crop WxH+X+Y +repage -resize WxH! tile.png`),
  and the cropped tiles are committed next to the generator, so the build
  doesn't depend on the raw captures.
- `-alpha off` drops the alpha channel (both PNGs are 8-bit RGB) and
  `compression-level=9` keeps the preview well under 1 MB.

**Checking that the build reproduces the shipped files.** Render into a
scratch copy and compare pixels, not bytes:

```sh
cp -r art /tmp/check/ && mkdir /tmp/check/about
(cd /tmp/check/art && bash build.sh)
magick compare -metric AE /tmp/check/about/preview.png about/preview.png null:
```

`-metric AE` prints the count of differing pixels; `0 (0)` means identical.
For Last Stand the icon matches byte for byte, but the preview's bytes
differ while its pixels match: ImageMagick writes `date:create` and
`date:modify` text chunks into every PNG. `identify -verbose about/preview.png
| grep date:` shows them. Add `-define png:exclude-chunk=date,time` to the
`magick` call if you want byte-identical output and a clean `git status`
after every rebuild.

### Where else this applies

- Any generated binary in a repo (fonts, atlases, localization tables):
  keep the generator and its inputs, seed randomness, and compare decoded
  content rather than bytes.

---

## 6. Versioning and the rules for a public mod

### What the game does with version fields

- **`modVer`** is a `ModMetadata` field, but nothing in `Dew.Core` or
  `Dew.UI` reads it (`grep -rn modVer` finds only the declaration). It's for
  people. Log it on start so a player's `Player.log` tells you which build
  they ran. Last Stand does this in `Start`:
  `Log.Info(LogText.Loaded(mod.metadata.name, mod.metadata.modVer))`.
  Bump it with every upload.
- **`supportedGameVer`** is in the template's JSON but is not a field of
  `ModMetadata`; `JsonUtility` ignores unknown keys, and no code checks game
  versions. A game patch can break a mod with no warning, so retest after
  each game update.
- **`AssemblyVersion`** doesn't matter to the loader, which renames the
  assembly on every load.

### The gameplay flag

`LoadedModInstance.isAlteringGameplay` feeds
`DewMod.isGameplayAltered` (any loaded instance with the flag), which
`LobbyServiceProvider` publishes as the lobby attribute `isModded`. The
official docs: every server-side mod that alters gameplay must set
`instance.isAlteringGameplay = true`, which adds the MOD icon to the lobby
and a notice to joining clients. Last Stand sets it first thing in
`Start`. A mod with an `overrides/` folder gets it automatically in
`DewMod.Load`.

### Rules that affect a release

From the official Code of Conduct and the Modding Terms of Service:

1. Mods may heavily alter gameplay, and may even cheat progress.
2. A mod must not hurt other players' experience without their consent.
   For server-side mods the consent mechanism is the gameplay flag above.
3. A mod must not grant illicit access to exclusive, server-generated
   items: "things you cannot buy in-game using stardusts." Stardust itself
   isn't such an item, but keep it at the rates the game gives
   (`06-game-world-api.md`).
4. Mods must be non-commercial: no selling, no donation prompts, no forced
   paid content.
5. No malicious code, no unauthorized communication with external servers
   or data collection, no obfuscation; you should be able to explain your
   code on request.
6. No assets from other games or IP you don't have rights to, and no NSFW
   content. Workshop uploads must also follow Steam's Workshop rules.

### Config defaults reach players only with a schema bump

`ModBehaviour.LoadConfigsToDisk` loads the saved JSON *over* the field
initializers, so changing a default in code does nothing for anyone who
already has a config file. Last Stand stores a `[HideInInspector] int
schemaVersion = ConfigSchema.Current` in its config and overrides
`LoadConfigsToDisk` to move an outdated file to `<file>.old` before the base
call writes fresh defaults. Every release that changes a default, renames
or removes a field bumps `ConfigSchema.Current`. The mechanism is described
in `08-config-ui-and-commands.md`.

---

## 7. Distributing outside the Workshop

The pack folder is already a complete manual install.

1. Zip the pack folder itself so the archive's top level is `<Name>/`:
   `cd dist && zip -r MyMod-0.1.0.zip MyMod`.
2. Players extract it into `<GameFolder>/Mods/`, giving
   `Mods/MyMod/about/metadata.json`, and enable it in the mod manager.

Notes:

- A player who also subscribes to the Workshop item gets `IdConflict` on
  one copy (local folders win). Tell them to keep one.
- The included `publishedfileid.txt` makes the in-game Upload button say
  "update". If they press it, Steam refuses (`AccessDenied`, the "not mine"
  hint). Leave the file out of the zip if you want to avoid the confusion.
- Nothing about loading differs between a local and a Workshop copy: both
  go through `AddMod` and `Load`.

---

## 8. Release checklist

**Before packing**

1. Clean Release build with 0 warnings, 0 errors.
2. `modVer` bumped in `about/metadata.json`.
3. Config schema bumped if any default, name or removal changed.
4. `isAlteringGameplay = true` set if the mod changes gameplay.
5. `description.txt` updated and under 8000 characters; art rebuilt and
   pixel-compared if its sources changed; `preview.png` under 1 MB.

**Pack**

6. Run the pack script. The file list holds only the DLL, `about/` (and
   `overrides/` if used).
7. `about/metadata.json` in the pack points at the packed DLL.
8. `publishedfileid.txt` is present (for an update) and matches the
   committed one.

**Test the pack**

9. Dev folder out of `Mods/`, pack in. The mod manager shows one entry, `Ok`.
10. The log shows your load line with the new `modVer`; one solo run and one
    lobby run with a vanilla client reach the feature you changed
    (`09-debugging-and-testing.md`).

**Upload**

11. Write the change note (`about/changenote.txt` for Last Stand) before
    packing, or add it on the item page after the upload. Check
    visibility, set Public when ready.
12. First upload only: copy `publishedfileid.txt` back and commit it.
13. Restore the dev folder.
14. Optional: zip the pack for manual installs.
