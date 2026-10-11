# 08. Settings files, settings UI, console commands, chat and HUD text

This chapter covers every way a mod talks to people: the settings file on
disk, the settings screens, extra buttons in the game's own menus, console
commands, chat lines, and text on the HUD that unmodded ("vanilla") clients
can see. Each section starts from the game's code, shows how the technique
was found and used for Last Stand, and ends with other places it applies.

Before you add any player-facing surface, sort it by **where it runs**. That
decides what is possible:

| Surface | Runs on | Reaches a vanilla client? |
|---|---|---|
| Config file, config screen | Each machine, for its own copy of the mod | No. Only the host's copy matters for a host-only mod |
| Injected menu buttons | The machine that has the mod | No |
| Console commands | The machine where they are typed | No, but the host's command can change synced state |
| Chat notices (`ChatManager.BroadcastMessage`) | Sent by the server, drawn by every client | Yes |
| Quest tracker text (`DewQuest`) | Synced object, drawn by every client | Yes, within limits (section 7) |
| Location label, room-modifier panel | Built by each client from its own data | No free text |

Networking terms (SyncVar, ClientRpc, Command) are explained in
`04-host-only-networking.md`. Harmony patching of private methods is in
`03-hooking-with-harmony.md`. Mod lifecycle and reloads are in
`07-mod-architecture.md`.

---

## 1. Config files

### 1.1 Trace the load path before you write a config class

The official docs don't describe configs at all, so the decompiled code is
the only reference. Start from the loader and follow the calls.

1. Search the decompiled `Dew.Core` for the loader's call into your
   behaviour:
   ```sh
   grep -n "LoadConfigsToDisk\|ConsoleCommandAttribute\|AddComponent(item)" Dew.Core/DewMod.cs
   ```
2. Read `DewMod.Load(string id)`. The order matters:
   ```csharp
   GameObject gameObject = new GameObject(modItem.metadata.id);
   gameObject.SetActive(value: false);
   ...
   ModBehaviour modBehaviour = (ModBehaviour)gameObject.AddComponent(item);
   modBehaviour.instance = loadedModInstance;
   if (modBehaviour.modConfigFields.Length != 0)
   {
       try { modBehaviour.LoadConfigsToDisk(); }
       catch (Exception exception) { Debug.LogException(exception); }
   }
   ...  // console commands are registered here (section 5)
   gameObject.SetActive(value: true);
   ```
   The container is **inactive** while components are added. Unity doesn't
   call `Awake` on an inactive object, so the config is loaded *before* your
   `Awake` and `Start` run. By `Start`, the config field holds the file's
   values.
3. Read `ModBehaviour` (one file, `Dew.Core/ModBehaviour.cs`). The config
   fields are found by reflection:
   ```csharp
   public FieldInfo[] modConfigFields => (from f in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
       where f.FieldType.IsSubclassOf(typeof(ModConfig))
       select f).ToArray();
   ```
   So a config is a **public instance field** of a type derived from the
   abstract `ModConfig`, declared on a `ModBehaviour`. A behaviour can hold
   several.
4. The file path:
   ```csharp
   public virtual string GetModConfigFilePath(FieldInfo modConfigInfo)
   {
       string text = Path.Join(Path.Join(Application.persistentDataPath, "QuickSave", "Mods"), mod.metadata.id);
       return Path.Join(path2: GetType().Name + "." + modConfigInfo.Name + ".json", path1: text);
   }
   ```
   The file name is `<BehaviourType>.<fieldName>.json` inside a folder named
   after the mod id. Renaming the behaviour class or the field moves the
   file. Players lose their settings and the old file stays behind.
5. The load itself:
   ```csharp
   if (!File.Exists(modConfigFilePath))
   {
       string contents = DewPersistence.ToJson(fieldInfo.GetValue(this));
       File.WriteAllText(modConfigFilePath, contents);
   }
   else
   {
       object value = DewPersistence.FromJson(File.ReadAllText(modConfigFilePath), fieldInfo.FieldType);
       fieldInfo.SetValue(this, value);
   }
   ```
   `SaveConfigsToDisk()` is the same loop with an unconditional write. All
   three methods are `virtual`.

Where `persistentDataPath` points:

| Platform | Config file |
|---|---|
| Windows | `%USERPROFILE%\AppData\LocalLow\Lizard Smoothie\Shape of Dreams\QuickSave\Mods\<mod id>\<Type>.<field>.json` |
| Linux (Proton) | `steamapps/compatdata/2444750/pfx/drive_c/users/steamuser/AppData/LocalLow/Lizard Smoothie/Shape of Dreams/QuickSave/Mods/<mod id>/<Type>.<field>.json` |

Last Stand declares one config on its main behaviour, so its file is
`LastStandMod.config.json`:

```csharp
public class LastStandMod : ModBehaviour
{
    public LastStandConfig config = new LastStandConfig();
    ...
}

public class LastStandConfig : ModConfig
{
    [HideInInspector]
    public int schemaVersion = ConfigSchema.Current;

    [LabelText(ConfigText.StartingGoldLabel)]
    [Description(ConfigText.StartingGoldDescription)]
    public int startingGold = 100;
    ...
}
```

### 1.2 What the JSON round trip actually does

`DewPersistence.FromJson` is a thin wrapper:

```csharp
public static object FromJson(string json, Type type, JsonSerializerSettings settings = null)
{
    if (settings == null) settings = _defaultSettings;
    return JsonConvert.DeserializeObject(json, type, settings);
}

public static JsonSerializerSettings GetNewSettings()
{
    return new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Converters = new List<JsonConverter> { new NetworkBehaviourConverter(), new AssetConverter() }
    };
}
```

From this, plus Newtonsoft.Json's documented defaults:

- **Missing keys keep the field initializer.** Newtonsoft builds the object
  with its parameterless constructor, so `= 100` runs, then sets only the
  keys present in the file.
- **Unknown keys are ignored.** `MissingMemberHandling` is left at
  `Ignore`, so a renamed or removed field leaves a dead key in the file
  without an error.
- **Every public field is written**, including ones marked
  `[HideInInspector]`. That attribute only hides a field from the screen
  (section 2). This is how Last Stand keeps a `schemaVersion` in the file
  without showing it.
- **The file is one line.** No `Formatting.Indented` is set.
- **Collections are reused, not replaced.** `ObjectCreationHandling` is left
  at `Auto`. For a `List<T>` field that already has items from its
  initializer, Newtonsoft adds the file's items to the existing list. A
  default list of three entries becomes six after one save and load. Last
  Stand avoids this by storing every multi-choice setting as one
  comma-separated `string` (section 2.4). A string also survives the
  shallow `Clone()` in section 2.1.

### 1.3 Saved files keep old defaults: add a schema version

The load semantics above have one consequence that bit Last Stand: **a
changed default never reaches a player who already has a file**. The file
holds every field, including the ones the player never touched, so the
old values win. The first build's file kept minibosses at wave 4, Pain
every 3 waves and a 24-monster cap through several later releases that
changed all three defaults.

There is no per-field "user changed this" flag in the file. The simplest
reliable fix is a version number. When the version doesn't match, the file
is set aside and the game writes fresh defaults.

1. Add a hidden version field whose initializer is the current version:
   `[HideInInspector] public int schemaVersion = ConfigSchema.Current;`.
2. Check the raw JSON before the game reads it:
   ```csharp
   internal static class ConfigSchema
   {
       public const int Current = 40;

       public static bool IsCurrent(string json)
       {
           JToken version = JObject.Parse(json)[nameof(LastStandConfig.schemaVersion)];
           return version != null && version.Type == JTokenType.Integer && version.Value<int>() == Current;
       }
   }
   ```
3. Move an outdated or unreadable file aside:
   ```csharp
   public static void ResetIfOutdated(string path)
   {
       if (!File.Exists(path) || IsCurrent(path)) return;
       string backup = path + BackupSuffix;          // ".old"
       File.Copy(path, backup, true);
       File.Delete(path);
       Log.Warn(LogText.ConfigReset(ConfigSchema.Current, backup));
   }
   ```
4. Hook it in by overriding the virtual loader, then call the base, which
   now finds no file and writes defaults:
   ```csharp
   public override void LoadConfigsToDisk()
   {
       foreach (FieldInfo field in modConfigFields)
           ConfigFile.ResetIfOutdated(GetModConfigFilePath(field));
       base.LoadConfigsToDisk();
   }
   ```

Rules that come with it:

- **Bump the version whenever a default changes or a field is renamed or
  removed.** Last Stand's repository rules make this mandatory, and the
  version has reached 40.
- The test is equality, not "older than". A file from a *newer* build
  (after a downgrade) is reset too, which is the safe direction.
- A reset also discards settings the player changed on purpose. The `.old`
  copy and the log line tell them where to copy their values back from.
  This is a deliberate trade: losing a few custom values is better than
  keeping stale defaults without anyone knowing.
- A file that fails to parse is treated as outdated. Without this step,
  `JsonConvert` would throw inside `DewMod`'s `try` and the field would keep
  its initializer. The bad file would stay on disk and fail again on every
  load.

### 1.4 Validate once, into an immutable snapshot

The config object is a bag of public fields that four sources can write:
the JSON file (hand-edited or not), the stock config screen, your own
window, and `CopyTo` calls. None of them validate. The stock integer field
reports a new value after every keystroke that parses (section 2.2), so
`0` while the player is typing `0.5`, or a negative number, is a normal
input.

Last Stand never reads `LastStandConfig` from game logic. It turns it into
a `SessionSettings`, a class with only getters, built by one static factory
that clamps and converts every field:

```csharp
public static SessionSettings From(LastStandConfig config)
{
    return new SessionSettings(config.arenaRoom, Math.Max(0f, config.firstWaveDelaySeconds),
        config.showWaveInQuestTracker, Math.Max(Tuning.MinStartingWorld, config.startingWorld), ...
```

The conversions use a handful of reusable helpers:

```csharp
private static double ToChance(float percent)
{
    return Math.Max(0f, Math.Min(Tuning.PercentScale, percent)) / Tuning.PercentScale;
}

private static Cadence CadenceFrom(int firstWave, int intervalWaves)
{
    return new Cadence(Math.Max(Tuning.FirstWave, firstWave), Math.Max(MinimumStep, intervalWaves));
}

private static IReadOnlyList<string> KnownChoices(string text, ChoiceSet set)
{
    IReadOnlyList<Choice> choices = ConfigChoices.For(set);
    return ChoiceList.Split(text).FindAll(item => ChoiceList.Contains(choices, item));
}
```

- Percentages typed by people become 0..1 chances once, in one place.
- Interval fields can't be 0, which would mean a division by zero or an
  endless loop later.
- Names typed into the file are filtered against a fixed list, so a hand
  edit can't add a room modifier the mod never planned for.
- Settings that combine are combined here. For example, `curseToStartWaves`
  forces the Hatred shrine on: `config.hatredRemnant || config.curseToStartWaves`.

The snapshot is rebuilt at three points, and game code only ever sees a
whole, valid snapshot:

```csharp
private void Start()                       { Session = new LastStandSession(SessionSettings.From(config)); ... }
public override void OnConfigChanged()     { Session?.UseSettings(SessionSettings.From(config)); }
private void OnGameManagerStarted(GameManager game) { Session?.Reset(); Session?.UseSettings(SessionSettings.From(config)); }
```

`OnConfigChanged` is the game's virtual hook. The mod manager calls it
after Apply (section 2.1), and Last Stand's own window calls it too.

**Where else this applies**

- Any mod setting that feeds a formula: clamp it in the factory, not at
  every use.
- Config-driven content lists (monster pools, banned items): store type
  names as strings and filter them against `DewResources` or a fixed list
  when you build the snapshot.
- Mid-run changes: swapping a snapshot reference applies them live with no
  half-updated state.

---

## 2. The stock config screen

### 2.1 How the mod manager builds and applies it

With Developer Mode on, the mod manager's gear button calls
`DewMod.OpenConfigWindow(LoadedModInstance mod)`. Reading it shows the
whole edit cycle:

```csharp
ModConfig config = (ModConfig)fieldInfo.GetValue(i);
ModConfig localState = config.Clone();
ModConfig defaultState = (ModConfig)Activator.CreateInstance(fieldInfo.FieldType);
localState.BuildWidgets(scrollRect.content.transform, out var onChanged, out var requestUpdate);
onReset.Add(delegate { defaultState.CopyTo(localState); requestUpdate.Invoke(); });
onRevert.Add(delegate { config.CopyTo(localState); requestUpdate.Invoke(); });
onBeforeApply.Add(delegate { localState.CopyTo(config); });
...
onAfterApply.Add(delegate { i.OnConfigChanged(); i.SaveConfigsToDisk(); });
```

The base class supplies the copy helpers:

```csharp
public virtual ModConfig Clone() { return (ModConfig)MemberwiseClone(); }

public virtual void CopyTo(ModConfig other)
{
    foreach (FieldInfo fieldInfo in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        fieldInfo.SetValue(other, fieldInfo.GetValue(this));
}

public virtual void BuildWidgets(Transform parent, out SafeAction onChanged, out SafeAction requestUpdate)
{
    DewGUI.CreateWidgetsForObject(GetType(), this, parent, out onChanged, out requestUpdate);
}
```

What follows from this:

- The screen edits a **copy**. Apply copies it back, calls
  `OnConfigChanged()`, then saves. Back discards it.
- `Clone()` is `MemberwiseClone`, a **shallow** copy. A `List<T>` or array
  field is shared between the copy and the live config, so editing it on
  screen changes the live config before Apply, and Undo can't bring it
  back. `CopyTo` also copies references. Keep config fields to value types
  and strings, or override `Clone`.
- **Reset** copies a freshly constructed config, including its
  `schemaVersion`, then calls `requestUpdate` so every widget re-reads
  its field. Any custom widget must refresh fully from `setValue`
  (section 2.3).
- `BuildWidgets` is virtual. Overriding it replaces the whole layout
  (section 2.5).

### 2.2 How `DewGUI.CreateWidgetsForObject` turns fields into rows

`Dew.Core/DewGUI.cs`, about 70 lines. Read it once and you know every rule
of the stock screen:

```csharp
FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
foreach (FieldInfo fieldInfo in fields)
{
    FieldInfo f = fieldInfo;
    if ((!f.IsPublic && f.GetCustomAttribute<SerializeField>() == null) || f.GetCustomAttribute<HideInInspector>() != null)
        continue;
    foreach (Attribute customAttribute3 in f.GetCustomAttributes())
    {
        if (customAttribute3 is HeaderAttribute headerAttribute) { /* header text widget */ }
        if (customAttribute3 is SpaceAttribute spaceAttribute)   { /* LayoutElement of that height */ }
    }
    FieldBuilder item2 = fieldBuilders.Find(item => item.Item1(f.FieldType, f)).Item2;
    if (item2 == null) continue;
    ModConfig.LabelTextAttribute customAttribute = f.GetCustomAttribute<ModConfig.LabelTextAttribute>();
    string text = (customAttribute != null) ? customAttribute.text : f.Name.NicifyVariableName();
    ...
    FieldBuildResult result = item2(f.FieldType, f, horizontalLayoutGroup.transform);
    requestUpdate.Add(delegate { result.setValue(f.GetValue(state)); });
    result.onChanged += (Action<object>)delegate(object obj) { f.SetValue(state, obj); _onChanged.Invoke(); };
    ModConfig.DescriptionAttribute customAttribute2 = f.GetCustomAttribute<ModConfig.DescriptionAttribute>();
    ...
}
requestUpdate.Invoke();
```

The attributes, and where each one comes from:

| Attribute | Defined in | Effect |
|---|---|---|
| `[HideInInspector]` | `UnityEngine` | Field skipped on screen, still saved to JSON |
| `[SerializeField]` | `UnityEngine` | Lets a non-public field show |
| `[Header("...")]` | `UnityEngine` | Header text above the row (shown even when the field itself has no builder) |
| `[Space(h)]` | `UnityEngine` | Vertical gap |
| `[LabelText("...")]` | `ModConfig.LabelTextAttribute`, nested in the game's `ModConfig` | Row label; default is `NicifyVariableName()` of the field name |
| `[Description("...")]` | `ModConfig.DescriptionAttribute`, nested in `ModConfig` | Smaller gray text under the row |

`LabelText` and `Description` are **nested classes of `ModConfig`**, not
Odin's or `System.ComponentModel`'s. Inside a class that derives from
`ModConfig` the short names resolve through inheritance, which is why
Last Stand's config writes `[LabelText(...)]`. Code outside that class has
to write `ModConfig.LabelTextAttribute`, as Last Stand's panel code does.
If a `using System.ComponentModel;` is in scope, `[Description]` becomes
ambiguous.

The builder list is filled in `DewGUI.OnInit`, marked
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`:

1. `KeyCode` → a key-binding widget (`widgetBindItem`).
2. Any `enum` → a dropdown of `Enum.GetNames`.
3. One entry per type in `SupportedBasicTypes`: `string`, `bool`, `float`,
   `double`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`,
   `Vector2/3/4`, `Vector2Int/3Int`, `Quaternion`. `bool` is a checkbox
   (`widgetToggleCheck`). The others are text inputs
   (`GenericInputFieldBuilder<T>`).

Facts that matter in practice:

- A field whose type has no builder (a `List<string>`, a custom class) is
  **silently skipped**, but its `[Header]` still shows.
- `GenericInputFieldBuilder` calls `onChanged` on every keystroke that
  parses. Text that doesn't parse turns the field red and changes nothing.
  The config therefore holds intermediate values while someone types. This
  is the reason for section 1.4.
- `float.TryParse` uses the current culture. On a machine with a decimal
  comma, `0.5` doesn't parse.
- `fieldBuilders.Find` returns the **first** match, so an entry inserted at
  index 0 overrides the stock widget for the fields it claims.

### 2.3 Overriding one widget: a builder at index 0

Last Stand needed a room picker and a "which bosses" picker. As stock
strings they were free-text boxes where a typo silently broke the setting.
The fix replaces the widget only for marked fields and leaves every other
field alone.

The contract, from `DewGUI.cs` and `FieldBuildResult.cs`:

```csharp
public delegate bool FieldBuilderCondition(Type type, FieldInfo info);
public delegate FieldBuildResult FieldBuilder(Type type, FieldInfo info, Transform parent);
public static List<(FieldBuilderCondition, FieldBuilder)> fieldBuilders;

public class FieldBuildResult
{
    public Func<object> getValue;
    public Action<object> setValue;               // called by requestUpdate: show this value
    public SafeAction<object> onChanged = new SafeAction<object>();  // raise when the user edits
    public GameObject root;
}
```

Steps:

1. **Mark the fields with your own attribute.** The condition receives the
   `FieldInfo`, so it can test for anything:
   ```csharp
   [AttributeUsage(AttributeTargets.Field)]
   internal sealed class PickOneAttribute : Attribute
   {
       public PickOneAttribute(ChoiceSet set) { Set = set; }
       public ChoiceSet Set { get; }
   }

   [PickOne(ChoiceSet.ArenaRooms)]
   [LabelText(ConfigText.ArenaRoomLabel)]
   public string arenaRoom = GameNames.DefaultArenaRoom;

   [PickMany(ChoiceSet.HeroicBosses)]
   public string heroicBossTypes = ...;
   ```
2. **Keep the conditions in static fields** so you can find your entries
   again for removal:
   ```csharp
   private static readonly DewGUI.FieldBuilderCondition PickOneCondition = (type, info) =>
       type == typeof(string) && info != null && info.GetCustomAttribute<PickOneAttribute>() != null;
   ```
3. **Register at the front, idempotently, and unregister on unload.** The
   list is static and is cleared only at `SubsystemRegistration`, once per
   game launch. A mod that is unloaded or reloaded mid-session leaves its
   entries behind unless it removes them:
   ```csharp
   public static void Register()
   {
       Unregister();
       DewGUI.fieldBuilders.Insert(0, (PickOneCondition, BuildDropdown));
       DewGUI.fieldBuilders.Insert(0, (PickManyCondition, BuildChecklist));
   }

   public static void Unregister()
   {
       DewGUI.fieldBuilders.RemoveAll(entry => entry.Item1 == PickOneCondition || entry.Item1 == PickManyCondition);
   }
   ```
   `Register()` runs in `Start`, `Unregister()` in `OnDestroy`.
4. **Build from the game's widget prefabs.** `DewGUI` exposes them as
   properties that call `Resources.Load` each time: `widgetDropdown`
   (`TMP_Dropdown`), `widgetToggleCheck` (`UI_Toggle`), `widgetTextLabel`,
   `widgetTextBody`, `widgetTextHeader`, `widgetButton`, `widgetInputField`,
   `widgetSlider`, `widgetScrollRect`, `widgetWindow`. Layout helpers:
   `CreateHorizontalLayoutGroup`, `CreateVerticalLayoutGroup`,
   `SetExpandWidth`, `SetWidth`, `SetHeight`, `SetText`.
5. **Write `setValue` so it can be called any number of times**, because
   Reset and Undo call it again. The dropdown rebuilds its options on each
   call and changes the selection *without notifying*, so showing a value
   isn't reported as an edit:
   ```csharp
   result.setValue = value =>
   {
       current = value as string;
       values.Clear();
       List<string> labels = new List<string>();
       foreach (Choice choice in choices) { values.Add(choice.Value); labels.Add(Label(choice)); }
       if (!string.IsNullOrEmpty(current) && !values.Contains(current))
       {
           values.Add(current);
           labels.Add(ConfigText.UnknownChoiceLabel(current));
       }
       dropdown.ClearOptions();
       dropdown.AddOptions(labels);
       dropdown.SetValueWithoutNotify(Math.Max(0, values.IndexOf(current)));
       dropdown.RefreshShownValue();
   };
   ```
   A saved value that isn't in the list is added as an extra option instead
   of being replaced. Opening the screen should never change a setting by
   itself.
6. **Guard against your own events.** The checkbox list sets
   `UI_Toggle.isChecked` from code. Reading the setter shows it raises the
   event no matter who sets it:
   ```csharp
   set
   {
       if (_isChecked == value) return;
       ...
       _isChecked = value;
       ...
       try { onIsCheckedChanged?.Invoke(value); }
       catch (Exception exception) { Debug.LogException(exception, this); }
   }
   ```
   Without a guard, `setValue` would set each box, each box would raise
   `onChanged`, and the screen would mark itself dirty and write half-built
   values back into the config. Last Stand uses a flag:
   ```csharp
   updating = true;
   try
   {
       for (int i = 0; i < choices.Count; i++)
           toggles[i].isChecked = picked.Contains(choices[i].Value);
   }
   finally { updating = false; }
   ```
   The toggle listener returns early while `updating` is set. Entries in the
   saved string that aren't in the list are kept in `extras` and appended
   back when the string is rebuilt.

The value stays a plain `string` of asset names
(`"Mon_Forest_BossDemon, Mon_DarkCave_BossSeeker"`). Files from before the
widget existed still load, the JSON avoids the list-reuse problem from
1.2, and the shallow `Clone()` from 2.1 is harmless.

### 2.4 Labels the player understands

The choice data is a fixed table of `(asset name, detail)` pairs in one
file (`ConfigChoices`). The visible label comes from the game's own
localized name where one exists (`<type>_Name`, see
`05-assets-and-data.md`) plus a short detail such as the world. The saved
value is always the asset name, so a language change never affects the file.

### 2.5 Replacing the whole layout: override `BuildWidgets`

The stock screen is one long column. Last Stand has about 135 fields, so
it overrides the virtual method and builds a sidebar of pages instead:

```csharp
public override void BuildWidgets(Transform parent, out SafeAction onChanged, out SafeAction requestUpdate)
{
    SettingsPanel.Build(this, parent, 0f, out onChanged, out requestUpdate);
}
```

For this to work in the mod manager:

1. **Keep the same field filter** as `CreateWidgetsForObject` (public or
   `[SerializeField]`, not `[HideInInspector]`). Otherwise the two screens
   disagree about which fields exist.
2. **Still ask `DewGUI.fieldBuilders` for each control**, so your index-0
   builders and the stock ones both work:
   ```csharp
   foreach ((DewGUI.FieldBuilderCondition condition, DewGUI.FieldBuilder builder) in DewGUI.fieldBuilders)
       if (condition(field.FieldType, field)) return builder;
   ```
3. **Return working `onChanged` and `requestUpdate`.** The mod manager adds
   its dirty flag to `onChanged` and its Reset/Undo to `requestUpdate`. Each
   row adds `() => result.setValue(field.GetValue(_state))` to
   `requestUpdate` and writes the field in `result.onChanged`, just like the
   stock code.
4. **Group with attributes, not code.** A custom `[Page(SettingsPage.Waves)]`
   on the first field of each page starts a new sidebar page, and the
   standard `[Header]` starts a card inside it. Fields are declared in page
   order, so adding a setting means adding one field.

### 2.6 Your own settings window

The mod manager's screen is reachable only with Developer Mode on. Last
Stand opens the same panel from a lobby button (section 3) and a console
command, in a window it builds itself:

```csharp
UI_Window window = UnityEngine.Object.Instantiate(DewGUI.widgetWindow, DewGUI.canvasTransform);
window.isDraggable = true;
window.enableBackdrop = true;
_current = window.gameObject.AddComponent<ConfigWindow>();
...
_draft = (LastStandConfig)mod.config.Clone();
SettingsPanel.Build(_draft, content.transform, ConfigWindowLayout.PageHeight, out SafeAction _, out SafeAction _);
```

Apply follows the same order as the game's screen:

```csharp
internal void ApplyConfig(LastStandConfig edited)
{
    edited.CopyTo(config);
    try { SaveConfigsToDisk(); Log.Info(LogText.ConfigApplied); }
    catch (Exception e) { Log.Error(LogText.ConfigSaveFailed, e); }
    OnConfigChanged();
}
```

**Escape and the gamepad Back button.** The game routes Back through
`GlobalUIManager`'s back handlers. `AddBackHandler(owner, priority,
Func<bool>)` keeps the list sorted by priority. `GoBack()` walks it from
the highest priority down and stops at the first handler that returns
`true`. Handlers whose `owner` was destroyed are dropped. The lobby
settings window registers at priority 100, the tutorial highlight at
99999. Last Stand's window registers at `int.MaxValue` so Escape closes
it first, and removes its handler in `OnDestroy`:

```csharp
_backHandler = _globalUIManager.AddBackHandler(this, int.MaxValue, OnBack);
...
private bool OnBack() { Close(); return true; }
```

**Where else this applies**

- Any picker over game content (zones, heroes, items): a builder at index 0
  claimed by an attribute, value saved as the asset name.
- Sliders for 0..100 values: `DewGUI.widgetSlider` in a builder.
- A settings window opened from anywhere: `widgetWindow` on
  `DewGUI.canvasTransform`, a `Clone()` draft, `CopyTo` + save +
  `OnConfigChanged` on Apply, plus a back handler.

---

## 3. Injecting UI into the game's own screens

### 3.1 Find the class behind a screen

Mod UI that lives only in the mod manager is hard to find. Players expect
a button where they already look. To add one, you first need the screen's
MonoBehaviour.

1. **Start from visible text.** Find a word on the screen in the game's
   localization (`05-assets-and-data.md`) to get its key. Lobby game-mode
   names are `GameMode_ShapeOfDreams` and `GameMode_Limbo`.
2. **Search the UI assembly for the key:**
   ```sh
   grep -rln "GameMode_Limbo" Dew.UI/ Dew.Core/
   ```
   This gives `UI_Title_TitleView` (solo mode list) and
   `UI_Title_CreateLobbyView` (lobby creation).
3. **When the text comes from a prefab rather than code**, search class
   names instead. The UI classes follow `UI_<Scene>_<Thing>`:
   `ls Dew.UI | grep -i lobby` lists `UI_Lobby_SettingsWindow`, the panel
   that slides out next to the lobby.
4. **Read the class for three things:**
   - which method fills or refreshes the content (that's your hook),
   - which fields are public (references to child objects you can use),
   - which private state the game acts on (you'll reach it by reflection).

What the three classes gave:

| Class | Refresh method | Useful public fields | Private state |
|---|---|---|---|
| `UI_Lobby_SettingsWindow` (`MonoBehaviour`) | `Start` (back handler), `OnEnable` (position) | `followParent` | none |
| `UI_Title_TitleView` (`View`) | `OnStateChanged()` (private) | `limboModeText` (`TextMeshProUGUI`) | `state` (`StateType`) |
| `UI_Title_CreateLobbyView` (`View`) | `OnShow()` (protected), `UpdateUIState()` (private) | `gameModeDropdown` (`UI_Dropdown`), `lobbyNameInputField`, `gameModeDescriptionText` | `_state` (`DewNetworkStartSettings`) |

### 3.2 Pick the hook: when is the screen (re)built?

- **Unity messages** such as `Start` and `OnEnable` are private methods.
  Patch them by name with `[HarmonyPatch("Start")]`. `UI_Lobby_SettingsWindow`
  is one object that is toggled on and off, so `OnEnable` runs on every
  open and `Start` only once, after the first `OnEnable`. Last Stand
  postfixes both and makes the work idempotent, so the order and the
  number of calls don't matter:
  ```csharp
  [HarmonyPatch(typeof(UI_Lobby_SettingsWindow))]
  internal static class LobbySettingsPatch
  {
      [HarmonyPostfix]
      [HarmonyPatch(GameNames.LobbySettingsStart)]       // "Start"
      private static void Start_Postfix(UI_Lobby_SettingsWindow __instance) { TryInject(__instance); }

      [HarmonyPostfix]
      [HarmonyPatch(GameNames.LobbySettingsEnable)]      // "OnEnable"
      private static void OnEnable_Postfix(UI_Lobby_SettingsWindow __instance) { TryInject(__instance); }
  }
  ```
- **`View.OnShow`** runs every time a game view opens. `UI_Title_CreateLobbyView.OnShow`
  rebuilds its dropdown from scratch (`ClearOptions`, then `AddOption` twice),
  so anything added there must be added again on every show.
- **State-change methods** such as `UI_Title_TitleView.OnStateChanged` run
  when a sub-panel becomes visible. That's the moment the template you
  want to clone is guaranteed to exist and be configured.
- **Re-render methods** such as `UpdateUIState` overwrite text you set. A
  postfix on the re-render method puts your text back after every one.
- Wrap the injection in `try/catch` and log. An exception in a UI postfix
  breaks the game's screen. See `03-hooking-with-harmony.md`.

### 3.3 Make injection idempotent and removable

Postfixes run many times, and mods can be reloaded mid-session. Every
injected object needs a name you can find again:

```csharp
public const string OwnedSuffix = " (" + Tag + ")";
public const string LobbyButtonName = "Last Stand Settings Button" + OwnedSuffix;

public static bool IsOurs(string objectName)
{
    return objectName != null && objectName.EndsWith(OwnedSuffix, StringComparison.Ordinal);
}
```

`Ensure` looks for the object first and only creates it when missing:

```csharp
Transform existing = panel.Find(ModIdentity.LobbyButtonName);
if (existing != null)
{
    existing.gameObject.SetActive(NetworkServer.active);
    return;
}
if (!NetworkServer.active) return;
Button button = Object.Instantiate(DewGUI.widgetButton, panel);
button.name = ModIdentity.LobbyButtonName;
button.GetComponentInChildren<TMP_Text>(includeInactive: true).text = ConfigText.LobbyButton;
button.onClick.AddListener(ConfigWindow.Toggle);
```

On unload, remove every object with the suffix, including inactive ones:

```csharp
foreach (Button button in Resources.FindObjectsOfTypeAll<Button>())
{
    if (button != null && button.gameObject.scene.IsValid() && ModIdentity.IsOurs(button.name))
        Object.DestroyImmediate(button.gameObject);
}
```

- `FindObjectsOfType` skips inactive objects, and the lobby panel is
  usually inactive, so `Resources.FindObjectsOfTypeAll` is the right call.
- `FindObjectsOfTypeAll` also returns prefab assets. `scene.IsValid()` is
  false for those, so the check keeps the code away from assets.
- Event subscriptions made on game objects (`gameModeDropdown.onValueChanged
  += ...`) are not destroyed with your buttons. Keep the handler in a
  static method and remove it in `OnDestroy` (`LobbyModeOption.Detach()`).

### 3.4 Clone a widget: prefab widgets vs scene widgets

There are two sources of templates.

**`DewGUI` prefabs** (`widgetButton`, `widgetToggleCheck`, ...) are
neutral, style-matched widgets loaded from `Resources/DewGUI/`. Use them
for anything new, such as the lobby's **Last Stand** button.

**A sibling already in the scene** is the right template when the new item
must look and navigate exactly like its neighbors. Last Stand's solo
button is a copy of the Limbo button. Copying a scene widget has three
traps:

1. **Find the template from a public field.** `limboModeText` is public,
   and the button is its parent:
   `view.limboModeText.GetComponentInParent<Button>(true)`.
2. **Localization components overwrite your text.** `DewLocalizedText`
   sets its `TextMeshProUGUI` from its key in `Start` and again on every
   language change:
   ```csharp
   private void Start() { UpdateText(); }
   public void UpdateText() { ... _text.text = DewLocalization.GetUIValue(key); }
   ```
   The clone's `Start` runs on the next frame and would replace your label.
   Remove the components with `DestroyImmediate` before setting the text:
   ```csharp
   foreach (DewLocalizedText localized in clone.GetComponentsInChildren<DewLocalizedText>(true))
       Object.DestroyImmediate(localized);
   ```
3. **Click listeners set in the editor survive `RemoveAllListeners()`.**
   That method only removes listeners added at runtime. Persistent
   listeners from the scene stay, so the clone would still start Limbo.
   Replace the whole event:
   ```csharp
   Button button = clone.GetComponent<Button>();
   button.onClick = new Button.ButtonClickedEvent();
   button.onClick.AddListener(ModeSelection.StartSolo);
   ```

To find the label in the clone, Last Stand records the sibling-index path
from the template root to `limboModeText` and follows the same path in the
clone. Child names can repeat, but sibling indices don't change when a
hierarchy is duplicated. `clone.transform.SetSiblingIndex(template.GetSiblingIndex() + 1)`
places the clone directly under Limbo inside the `VerticalLayoutGroup`.

### 3.5 Host-only visibility

A host-only mod's settings only matter on the host. A friend who has the
mod loaded and joins your lobby would otherwise see the button and edit
their own file, which never reaches your run. Check the role every time
the panel opens, not once. The same panel object exists before and after
you host:

- `NetworkServer.active` true: you are the host (or solo).
- `NetworkClient.active && !NetworkServer.active`: you joined someone else.
- Neither: title screen.

The lobby button is created only with `NetworkServer.active`, and an
existing one is hidden when it's false. The `laststand_config` command
refuses only in the joined case, so it still works on the title screen:

```csharp
if (NetworkClient.active && !NetworkServer.active)
{
    Log.Warn(LogText.ConfigHostOnly);
    return;
}
ConfigWindow.Open();
```

The mod manager's own config screen belongs to the game and can't be
hidden this way. It only ever edits the local file.

### 3.6 Gamepad focus

Gamepad navigation in this game does not use Unity's `Selectable`
navigation. `GlobalUIManager` tracks objects that implement
`IGamepadFocusable`, mainly the `UI_GamepadFocusable` component, which
registers itself in `OnEnable` (`AddGamepadFocusable(this)`). When the title
view switches state with a gamepad active, it calls
`SetFocusOnFirstFocusable(mainSection)`, which sorts the focusables by
`transform.GetSiblingIndex()`. Consequences:

- A clone of a stock button carries the template's focus component, so it
  can be reached like the original.
- Sibling order decides which item gets focus first.
- A `Button` built from `new GameObject(...)` has no focus component. Add
  `UI_GamepadFocusable` if pad users must reach it.

**Where else this applies**

- A button on the pause menu, the hero select screen or the result screen:
  same steps (find the class, hook its show/refresh method, `Ensure` by
  name, remove by suffix).
- Read-only info panels in menus: a `DewGUI.widgetTextBody` added in a
  postfix.
- Hiding a stock option: find it the same way and `SetActive(false)` it in
  the same postfix.

---

## 4. Adding a game mode option

### 4.1 Find how the game carries a mode choice

The game already has a second mode, Limbo. Following how Limbo is chosen
tells you where a mode lives and what survives a restart.

1. `UI_Title_CreateLobbyView.Awake` (the dropdown handler):
   ```csharp
   gameModeDropdown.onValueChanged += (Action<object>)delegate(object o)
   {
       if ((string)o == "Limbo")
       {
           _state.addedGameMods = new List<string> { "GameMod_Limbo" };
           _state.customGameSettingsSaveKey = "limbo";
       }
       else
       {
           _state.addedGameMods = new List<string>();
           _state.customGameSettingsSaveKey = null;
       }
       PreferredGameSettings preferredGameSettings = DewSave.profileMain.GetPreferredGameSettings(_state.customGameSettingsSaveKey);
       ...
   };
   createLobbyButton.onClick.AddListener(delegate { ManagerBase<TransitionManager>.instance.PlayGame(_state); Hide(); });
   ```
   A mode is two things in `DewNetworkStartSettings`: a list of game
   modifiers and a **save key** string.
2. Follow the key. `GameSettingsManager.Awake`:
   ```csharp
   if (!string.IsNullOrEmpty(DewNetworkManager.startSettings.customGameSettingsSaveKey))
       Network_003CgameSettingsSaveKey_003Ek__BackingField = DewNetworkManager.startSettings.customGameSettingsSaveKey;
   ```
   `gameSettingsSaveKey` is a SyncVar, also marked `[SaveVar]`, default
   `"default"`.
3. Follow the restart. `DewNetworkManager` copies it back before
   restarting:
   ```csharp
   startSettings.addedGameMods = NetworkedManagerBase<GameSettingsManager>.instance.addedGameMods.ToList();
   startSettings.customGameSettingsSaveKey = NetworkedManagerBase<GameSettingsManager>.instance.gameSettingsSaveKey;
   ```
4. Follow the preferences. `DewProfile.GetPreferredGameSettings(key)` keeps
   one `PreferredGameSettings` (difficulty, lucid dreams, lobby name...) per
   key, with `null` mapped to `"default"`.

Result: a custom save key is a mode flag the game already keeps for you.
It is set from the start settings, synced, saved with the run, carried
through restarts, and keeps its own lobby preferences. Last Stand uses
`"laststand"` with no added game modifiers, and asks only that field:

```csharp
public static bool IsSelected()
{
    GameSettingsManager settings = NetworkedManagerBase<GameSettingsManager>.softInstance;
    return settings != null && settings.gameSettingsSaveKey == ModIdentity.SaveKey;
}

public static bool IsHosting() { return NetworkServer.active && IsSelected(); }
```

No separate "enabled" flag exists that could drift from the game's state.
Every patch begins with `if (!LastStandMode.IsHosting()) return;`.

### 4.2 The solo button

`UI_Title_TitleView.EnterSingleplayerOptions`:

```csharp
ManagerBase<TitleManager>.instance.CheckTutorial(delegate
{
    if (!Dew.IsGameModifierIncludedInGame("GameMod_Limbo"))
        ManagerBase<TitleManager>.instance.EnterSingleplayerShapeOfDreams();
    else
        SetState(StateType.GameMode);
});
```

The mode list (Shape of Dreams, Limbo, Go Back) only appears when Limbo is
in the build. Two patches:

1. **Prefix** `EnterSingleplayerOptions`: when Limbo isn't included, do
   what the else-branch would do and skip the original. Last Stand then
   always gets a list to live in:
   ```csharp
   if (SoloModeButton.IsLimboAvailable()) return true;
   TitleManager title = ManagerBase<TitleManager>.softInstance;
   if (title == null) return true;
   title.CheckTutorial(() => __instance.SetState(UI_Title_TitleView.StateType.GameMode));
   return false;
   ```
   In that case the Limbo button itself is hidden
   (`template.gameObject.SetActive(IsLimboAvailable())`), and only Shape of
   Dreams and Last Stand show.
2. **Postfix** the private `OnStateChanged` when `state == StateType.GameMode`:
   clone the Limbo button as in 3.4.

The click starts the run the same way `TitleManager.EnterSingleplayerLimbo`
does, with Last Stand's key:

```csharp
DewNetworkStartSettings settings = new DewNetworkStartSettings();   // networkMode = Singleplayer (enum value 0)
settings.addedGameMods = new List<string>();
settings.customGameSettingsSaveKey = ModIdentity.SaveKey;
ManagerBase<TransitionManager>.softInstance.PlayGame(settings);
```

### 4.3 The lobby dropdown

`UI_Dropdown` (the game's wrapper around `TMP_Dropdown`) stores an
`object` value per option and raises `SafeAction<object> onValueChanged`
when the index changes, including when you set `value` from code.

1. **Postfix `OnShow`**, which has just rebuilt the list. Add the option,
   subscribe, and select it:
   ```csharp
   Detach();
   view.gameModeDropdown.AddOption(Messages.ModeName, ModIdentity.LobbyModeValue);
   _view = view;
   view.gameModeDropdown.onValueChanged += (Action<object>)OnModeChanged;
   view.gameModeDropdown.value = ModIdentity.LobbyModeValue;
   ```
2. **Expect the game's handler to run first.** It was subscribed in
   `Awake`, so it sees an unknown value. It takes the `else` branch, which
   clears the key, and then calls `UpdateUIState()`.
3. **Postfix the private `UpdateUIState`** to correct the state after every
   refresh. The game calls it after each change (mode, lobby type, name),
   so the selection is set again each time:
   ```csharp
   private static readonly FieldInfo StateField =
       AccessTools.Field(typeof(UI_Title_CreateLobbyView), GameNames.CreateLobbyState);   // "_state"

   public static void Sync(UI_Title_CreateLobbyView view)
   {
       bool selected = ModIdentity.LobbyModeValue.Equals(view.gameModeDropdown.value as string);
       if (selected && view.gameModeDescriptionText != null)
           view.gameModeDescriptionText.text = Messages.ModeDescription;
       if (!(StateField?.GetValue(view) is DewNetworkStartSettings state)) return;
       if (selected) ModeSelection.Select(state); else ModeSelection.Deselect(state);
   }
   ```
   `_state` is the object `PlayGame` receives when Create is clicked, so
   writing it is all that's needed. `Deselect` clears the key only if it is
   still Last Stand's, so picking Limbo keeps `"limbo"`.
4. **Your own handler runs last** and sets the lobby name. By then the key
   is already `"laststand"`, so the game's name-field listener saves the
   name into the `laststand` preferences. The vanilla lobby name is left
   unchanged.

**Where else this applies**

- Any "variant" mode (a challenge ruleset, a test mode): a custom
  `customGameSettingsSaveKey` as the single mode flag, checked through
  `gameSettingsSaveKey`.
- Adding entries to any `UI_Dropdown` the game rebuilds: postfix the
  rebuild, `AddOption(text, value)`, then fix the private state in a
  postfix on the refresh method.
- Reading or writing private view state: `AccessTools.Field` once, cached
  in a static field, with the name kept in one constants file.

---

## 5. Console commands

### 5.1 How commands are registered

Back in `DewMod.Load`, right after the config load:

```csharp
foreach (MethodInfo methodInfo in item.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
{
    Attribute[] array2 = methodInfo.GetCustomAttributes(typeof(ModBehaviour.ConsoleCommandAttribute)).ToArray();
    if (array2.Length != 0)
    {
        ModBehaviour.ConsoleCommandAttribute consoleCommandAttribute = (ModBehaviour.ConsoleCommandAttribute)array2[0];
        modBehaviour.RegisterConsoleCommand(methodInfo, consoleCommandAttribute.CustomName, consoleCommandAttribute.Description);
    }
}
```

- The attribute is nested in `ModBehaviour`:
  `ConsoleCommandAttribute(string description)` or
  `(string description, string customName)`. Inside a `ModBehaviour`
  subclass, write `[ConsoleCommand(...)]`. Without a custom name, the method
  name becomes the command.
- Any method of any visibility, static or instance, on any `ModBehaviour`
  in the assembly is scanned. Only the **first** attribute on a method is
  used, even though the attribute allows several. A method has one name.
- No attribute, no command. Removing the attribute is a clean way to hide
  a test command without deleting it.
- `RegisterConsoleCommand` checks for an existing command with the same
  name *and the same number of parameters*. If one exists, it appends `0`,
  `1`, ... to the new one. Unique prefixes (`laststand_...`) avoid that.
- On unload, `DewMod.UnloadAll` calls `DebugLogConsole.RemoveCommand` for
  every name the instance registered.

Last Stand keeps its commands on a separate behaviour, `LastStandConsole`,
so the main behaviour stays about lifecycle. Command names and
descriptions are constants in one file:

```csharp
public class LastStandConsole : ModBehaviour
{
    [ConsoleCommand(CommandText.WaveDescription, CommandText.Wave)]   // "laststand_wave"
    public void Cmd_Wave(int wave) { ... }
}
```

### 5.2 How arguments are parsed

The console is the bundled IngameDebugConsole (`Dew.External/IngameDebugConsole/DebugLogConsole.cs`).

- **Parameter types** must be in `parseFunctions`: `string`, `bool`, all
  integer types, `char`, `float`, `double`, `decimal`, Unity vectors,
  `Color`, `Rect`, `Bounds`, `GameObject` and others. A `Component` subclass,
  any `enum` and arrays of these also work. `ref`/`out` parameters and other
  types make registration fail with a logged error.
- **Splitting**: arguments are separated by spaces. `""`, `''`, `{}`, `()`
  and `[]` group an argument that contains spaces.
- **Overloads are chosen by argument count.** `ExecuteCommand` keeps only
  methods whose `parameterTypes.Length == commandArguments.Count - 1`, then
  takes the first one whose arguments all parse. Optional C# parameters
  are not used, so a default value never applies. **Write one overload per
  arity** under the same name:
  ```csharp
  [ConsoleCommand(CommandText.SkipDescription, CommandText.Skip)]
  public void Cmd_Skip() { Cmd_Skip(Tuning.DefaultSkipWaves); }

  [ConsoleCommand(CommandText.SkipDescription, CommandText.Skip)]
  public void Cmd_Skip(int waves) { if (CheatsAllowed()) LastStandMod.Current?.Session?.SkipWaves(waves); }
  ```
  `laststand_essence legendary` and `laststand_essence legendary 200` reach
  `Cmd_Essence(string)` and `Cmd_Essence(string, int)`.
- **Errors** are printed by the console itself: `can't find command`,
  `doesn't take N parameter(s)` with the available signatures, or
  `couldn't parse X to int`.
- `;` separates several commands on one line, and a `delay <seconds>` entry
  between them waits. This helps when you test a sequence.
- A method's non-`void` return value is logged as `Returned: ...`.

### 5.3 Where a command runs, and gating it

`ModBehaviour.RegisterConsoleCommand` calls `DebugLogConsole.AddCommand`
without a `CommandType`, so mod commands are `CommandType.Anywhere`. The
console's own routing of server commands from clients
(`ServerCommandHandler`, `isComingFromRemote`) only applies to the
`NetworkServer`/`GameServer*` types. A mod command typed on a client
**runs on that client**, where server-side calls do nothing or throw.
Gate every command yourself. `ModBehaviour` has protected helpers that
log and return `false`: `MakeSureServer()`, `MakeSureNetworked()`,
`MakeSureClientNonServer()`, `MakeSureInGame()`.

Last Stand uses two levels:

```csharp
private static bool CheatsAllowed()
{
    if (!LastStandMode.IsHosting())
    {
        Log.Warn(LogText.NotHosting);
        return false;
    }
    if (LastStandMod.Current?.Session?.Settings?.CheatCommands != true)
    {
        Log.Warn(LogText.CheatCommandsDisabled);
        return false;
    }
    return true;
}
```

1. **Host of a run in this mode** for anything that changes the run
   (`laststand_wave`, `laststand_next`).
2. **A config flag, off by default**, for anything that hands out
   resources (`laststand_gold`, `laststand_essence`, `laststand_level`...).
   Developer Mode alone is not consent from the other players in a lobby.
   The flag is a setting the host turns on deliberately.

Diagnostic commands with no effect (`laststand_status`) need no gate. The
command body is one line that hands off to the session. Rules don't live
in the console class.

Output goes through `Debug.Log` with the mod's prefix
(`[LastStand] ...`), so it shows in the console's log viewer and in
`Player.log` (`09-debugging-and-testing.md`).

**Where else this applies**

- Test hooks: jump to a state (`laststand_wave 49`) instead of playing up
  to it. See `09-debugging-and-testing.md`.
- Opening mod UI from anywhere (`laststand_config`), including places
  where no button can be injected.
- Name-search arguments (`laststand_essence <part of a name>`): take a
  `string`, match against `DewResources`, and list the candidates when the
  match is ambiguous.

---

## 6. Chat

### 6.1 Sending notices that vanilla clients render

`ChatManager` is a networked manager (`NetworkedManagerBase<ChatManager>`).
The relevant members:

```csharp
public struct Message
{
    public MessageType type;
    public string content;
    public string[] args;
    ...
}

public enum MessageType : byte { Raw, Chat, WorldEvent, Notice, UnlockedAchievement, AdvText }

[ClientRpc] public void BroadcastMessage(Message message)                       // every client
[TargetRpc] public void SendChatMessage(NetworkConnection target, Message message)   // one client
```

To learn what each type does on screen, read the renderer,
`Dew.UI/UI_Common_ChatBox_Item.Setup`:

| Type | Rendered as | Free text? |
|---|---|---|
| `Raw` | `DewAdvText.ConvertAdvToText(msg.content)` as-is, so TMP rich text works | **Yes** |
| `Chat` | `"<player>: content"`, content wrapped in `<noparse>` and profanity-filtered | Player chat only (`args[0]` = sender netId) |
| `WorldEvent` | `DewLocalization.GetUIValue(content)` formatted with `args`, gold color | Only existing keys |
| `Notice` | Same lookup, gray italics | Only existing keys |
| `AdvText` | `ConvertAdvToText(content)` | Yes |
| `UnlockedAchievement` | Localized template + achievement key | No |

`Notice` and `WorldEvent` treat `content` as a localization key. A vanilla
client doesn't have your strings, so it would show `ui.!<your text>`.
`Raw` is the type for mod text. The game uses it the same way: the curse
announcement in `CurseStatusEffect` and ping messages in `PingManager` are
both `Raw`.

Last Stand's whole chat adapter:

```csharp
internal static class ChatNotices
{
    public static void Broadcast(string content)
    {
        ChatManager chat = NetworkedManagerBase<ChatManager>.softInstance;
        if (chat == null) return;
        chat.BroadcastMessage(new ChatManager.Message
        {
            type = ChatManager.MessageType.Raw,
            content = content,
        });
    }
}
```

Colors are TMP rich-text tags built in one place, with a fixed prefix so
players can tell mod lines from game lines:

```csharp
private const string NoticeColor = "#e8a33d";
private const string WarningColor = "#ff4d4d";
private const string SuccessColor = "#5ad45a";

private static string Colored(string color, string text)
{
    return $"<color={color}>[{ModeName}] {text}</color>";
}
```

Notes:

- `BroadcastMessage` is a ClientRpc. Call it on the server only, and use
  `.softInstance`. `.instance` scans the scene when the manager is
  missing (`07-mod-architecture.md`).
- RPCs are not replayed. A player who joins later doesn't see earlier
  lines, so chat is for events, not for state. Use section 7 for state.
- Throttle repeated notices yourself. Nothing limits server-sent lines.

### 6.2 Reading what players type

Mirror generates two methods for every `[Command]`. Which one you patch
decides whether your code runs at all:

- `CmdSendChatMessage(string content, NetworkConnectionToClient sender = null)`
  is the **client-side stub**. It only serializes and sends:
  ```csharp
  writer.WriteString(content);
  SendCommandInternal("System.Void ChatManager::CmdSendChatMessage(System.String,Mirror.NetworkConnectionToClient)", ...);
  ```
  A patch here runs on the sender's machine, and a vanilla client never has
  your patch.
- `UserCode_CmdSendChatMessage__String__NetworkConnectionToClient(string content,
  NetworkConnectionToClient sender)` is the **original body**. It runs on the
  server for every sender:
  ```csharp
  content = content.Trim();
  if (content.Length > 0)
  {
      if (!IncrementRateAndCheck(sender)) { SendChatLockedNotice(sender); return; }
      Message message = default(Message);
      message.type = MessageType.Chat;
      message.content = SanitizeUserInput(content);           // cut to 100 chars, <noparse> removed
      message.args = new string[1] { sender.GetPlayer().netId.ToString() };
      BroadcastMessage(message2);
  }
  ```

So a server-side chat hook is a Harmony prefix on the `UserCode_` method
(it's `protected`, so patch it by name, kept in a constants file). `sender`
identifies the player (`sender.GetPlayer()`), and returning `false`
swallows the line, for example to keep `!command` text out of chat. The
host's own messages arrive through the same path, because in Mirror's
host mode the local client's commands are run by the local server. A
prefix runs before the whole body, so it sees the raw text before the
game's trimming, its 100-character cut (`ChatMessage_MaxLength`,
`SanitizeUserInput`) and its rate limit (`ChatMessage_RateLimitCount` 8
per `ChatMessage_RateLimitTimeframe` 12 s, then an 8 s lock). If you act
on the text, trim it and limit it yourself.

Last Stand ended up with no chat commands. Its host-only controls are
console commands (section 5) and the settings window, which other players
can't trigger. The hook point above is the verified place to start if you
need players without the mod to control something.

**Where else this applies**

- Run announcements (wave start, boss arrival, rewards) as `Raw` lines.
- Per-player notices via the `SendChatMessage` TargetRpc and the player's
  `connectionToClient`.
- Votes or opt-ins typed by vanilla clients, through the `UserCode_` prefix.

---

## 7. HUD text that vanilla clients can see

### 7.1 Method: classify each HUD element by what drives its text

A host-only mod can't add UI to a vanilla client. It can only feed values
into UI the client already has. For each candidate HUD element, open its
`Dew.UI` class and answer one question: **where does the string come
from?**

| Source of the text | Example | Can the host put free text there? |
|---|---|---|
| A synced `string` shown as-is | `DewQuest.currentProgress` | **Yes** |
| A key sent by RPC, looked up locally | `DewQuest.SetLocalizedTitle(key)` | Only existing keys, and not replayed to late joiners |
| Built locally from synced numbers | `UI_InGame_Location` (zone index + zone name) | No |
| Built locally from a networked actor's own data | `UI_InGame_RoomModDisplay` | Only by adding a real game actor |
| Purely local | lighting, fog, post-processing | No |

How to answer it: find the `.text =` assignment in the UI class, then trace
each input back to a SyncVar, an RPC or local data
(`04-host-only-networking.md`).

### 7.2 The quest tracker: a synced string with an icon

Last Stand needed a wave counter that every player sees. Reading
`UI_InGame_QuestDisplay_Item` shows what the tracker draws:

```csharp
titleText.text = quest.questTitleRaw;
descText.text = quest.questShortDescriptionRaw;
bool flag = quest.progressType != QuestProgressType.Hidden;
...
progressKillsObject.SetActive(quest.progressType == QuestProgressType.Kills);
progressTravelObject.SetActive(quest.progressType == QuestProgressType.Travel);
...
progressText.text = quest.currentProgress;
```

Then `DewQuest` shows which of those are synced:

```csharp
[SyncVar(hook = "OnProgressTypeChanged")] private QuestProgressType <progressType>k__BackingField;
[SyncVar(hook = "OnProgressChanged")]     private string <currentProgress>k__BackingField;
private string _questTitleRaw;                  // not synced
private string _questShortDescriptionRaw;       // not synced
```

The title and description are set locally in `OnCreate`
(`GetUIValue(GetType().Name + "_Title")`), or by key through two
ClientRpcs:

```csharp
[ClientRpc] public void SetLocalizedTitle(string key)
...
protected void UserCode_SetLocalizedTitle__String(string key)
{
    if (string.IsNullOrEmpty(key)) key = GetType().Name + "_Title";
    questTitleRaw = DewLocalization.GetUIValue(key);
}
```

and `GetUIValue` returns `"ui.!" + key` for a key it doesn't know.

The design that follows, step by step:

1. **Use a game quest type with no logic.** The client must create the
   quest from a prefab it already has. `QuestManager.StartQuest<T>()`
   spawns `DewResources.GetByType<T>()`, and a quest class from your
   assembly doesn't exist on a vanilla client. `Quest_Limbo` is an empty
   subclass of `DewQuest`, started only by Limbo, which never runs
   together with Last Stand:
   ```csharp
   _quest = quests.StartQuest<Quest_Limbo>(quest =>
   {
       quest.progressType = QuestProgressType.Kills;
       quest.currentProgress = progress;
   });
   ```
   The `beforePrepare` callback sets the SyncVars before the quest
   spawns, so they arrive in the spawn message.
2. **Put the variable text in `currentProgress`.** It's free text, it's
   synced and it's shown as-is: `Wave 12: 34 monsters left`, `Wave 13 in
   5s`. `progressType = Kills` picks the skull icon.
3. **Pick title and description from keys the game already has.** Without
   them a vanilla client shows `Quest_Limbo_Title` text, which reads
   "Limbo". Last Stand sends `InGame_ThreatLevelDisplay_ThreatLevelRising`
   ("Threat Level Rising") and `RoomMod_HarderFightBetterReward_Name`
   ("Powerful Monsters"). Each client translates them into its own
   language. Finding and checking keys is covered in
   `05-assets-and-data.md`. A wrong key shows as `ui.!<key>`, which is
   easy to spot in testing.
4. **Send the keys again when players join.** RPCs aren't stored, and a
   player who joins later gets the SyncVars but not the title. Last Stand
   counts ready connections each tick and sends both RPCs again when the
   count changes:
   ```csharp
   int connections = ReadyConnections();      // NetworkServer.connections where isReady
   if (connections != _labeledConnections)
   {
       _quest.SetLocalizedTitle(GameNames.TrackerTitleKey);
       _quest.SetLocalizedDescription(GameNames.TrackerDescriptionKey);
       _labeledConnections = connections;
       Log.Info(LogText.TrackerLabelSent(connections));
   }
   ```
5. **Write at most once a second, and only on change.** Every change of
   `currentProgress` runs the hook `OnProgressChanged` →
   `ClientQuestEvent_OnQuestUpdated`, which replays the item's update flash
   on every client:
   ```csharp
   if (now < _nextRefreshAt) return;
   _nextRefreshAt = now + Tuning.TrackerRefreshSeconds;
   ...
   if (progress != _shown) { _quest.currentProgress = progress; _shown = progress; }
   ```
6. **Take over a leftover quest instead of starting a second one.** After
   a mod reload or a continued save, the quest from before is still there:
   ```csharp
   if (quests.TryGetQuest(out Quest_Limbo existing) && !existing.IsNullOrInactive())
   {
       _quest = existing;
       ...
       Log.Info(LogText.TrackerAdopted);
       return true;
   }
   ```
7. **Remove it** in camp, on game over and on unload with
   `_quest.Destroy()` (server only, guarded by `NetworkServer.active`).
   Quests are networked actors, so destruction syncs. A quest destroyed
   while in progress disappears with no completion animation.

The same discovery removed the run's goal. `PlayGameManager` starts
`Quest_ShapeOfDreams` on the server, and `GameMod_Limbo` simply destroys
it. Last Stand does the same, checking once a second:

```csharp
if (quests != null && quests.TryGetQuest(out Quest_ShapeOfDreams goal) && !goal.IsNullOrInactive())
{
    goal.Destroy();
    Log.Info(LogText.GoalRemoved);
}
```

### 7.3 Dead ends: the location label and the modifier panel

**Top-right location label (`UI_InGame_Location`).** An early build wrote
the wave into it on the host. Reading the class showed why that could
never reach clients:

```csharp
NetworkedManagerBase<ZoneManager>.instance.ClientEvent_OnRoomLoaded += OnRoomLoaded;
GameManager.CallOnReady(delegate { OnRoomLoaded(default(EventInfoLoadRoom)); });
...
_text.text = string.Format("{0}. {1}", NetworkedManagerBase<ZoneManager>.instance.currentZoneIndex + 1,
    DewLocalization.GetUIValue(NetworkedManagerBase<ZoneManager>.instance.currentZone.name + "_Name"));
```

Each client builds the string itself, from a synced integer and a real
zone's localized name, and only on a room load. Faking a room load to force
a redraw would also reach every other listener of that event, and
`currentZoneIndex` drives prices, rewards and curves across the game. The
host-only overwrite was dropped so all players see the same label.

**Modifier panel (`UI_InGame_RoomModDisplay`).** It appears when a *main*
room modifier actor is added
(`ActorManager.ClientEvent_OnActorAdd`, filtered on `isMain` and
`hiddenOnVisitedNode`). It is hidden only when the next room starts
loading (`ZoneManager.ClientEvent_OnRoomLoadStarted`). The text is the
modifier's own localized name and description. The host can't write it
and can't hide it. A later main modifier replaces it, though. Last Stand
uses this to show a harmless real modifier's text whenever none of its own
is active (`RoomMod_FragmentOfRadiance_MysteriousPlace`, added through
`ZoneManager.AddModifier`; room modifiers are covered in
`06-game-world-api.md`). The lesson for this chapter: an element driven
by networked actors can only show what some real actor says.

**Where else this applies**

- A countdown, score, objective or "next event in Ns" line for every
  player: a logic-free game quest with `currentProgress`.
- Changing the icon (kills, travel, collect, travel-zone) through
  `progressType` to hint at the kind of goal.
- Any other `DewQuest` subclass the game owns: the same reading
  (`currentProgress` and `step` are synced strings, titles are keys) tells
  you what its tracker entry can show.

---

## Checklist

1. Config: a public `ModConfig` field on a `ModBehaviour`. Value types and
   strings only, a hidden `schemaVersion`, and `LoadConfigsToDisk`
   overridden to move outdated files to `.old`.
2. Never read the config from game logic. Build an immutable, clamped
   snapshot in `Start`, `OnConfigChanged` and at run start.
3. Custom widgets: an attribute, a builder inserted at index 0 of
   `DewGUI.fieldBuilders`, a `setValue` that can run again, a guard against
   events from your own `setValue`, and removal in `OnDestroy`.
4. Injected UI: find the class from the visible text, postfix its
   show/refresh method, `Ensure` by name, check `NetworkServer.active` on
   every show, remove by suffix with `Resources.FindObjectsOfTypeAll`.
5. Mode: a custom `customGameSettingsSaveKey`, read back through
   `GameSettingsManager.gameSettingsSaveKey`.
6. Commands: `[ConsoleCommand(desc, name)]`, one overload per argument
   count, and a host and cheat gate inside each one.
7. Text for everyone: `MessageType.Raw` chat for events, a logic-free
   game quest's `currentProgress` for state.
