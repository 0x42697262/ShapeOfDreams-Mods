using System;
using Mirror;
using UnityEngine;

namespace DreamArena;

/// <summary>
/// Entry point. Host-only PvP arena proof of concept.
/// </summary>
/// <remarks>
/// Everything this mod does is server-side state that Mirror replicates, so
/// other players do not need to install it.
/// </remarks>
public class DreamArenaMod : ModBehaviour
{
	/// <summary>
	/// Persisted settings. Public field of a <see cref="ModConfig"/> subtype,
	/// which is how the game discovers it: it loads the JSON before Start,
	/// renders the mod-manager config screen, and saves on Apply.
	/// </summary>
	public DreamArenaConfig config = new DreamArenaConfig();

	public static DreamArenaMod Instance { get; private set; }

	private readonly ReviveController _revives = new ReviveController();
	private readonly ArenaPhase _phase = new ArenaPhase();

	/// <summary>Where the players currently are: camp, arena, or not in a run.</summary>
	public PhaseKind Phase => _phase.Current;


	/// <summary>True while the run should be prevented from concluding.</summary>
	public static bool ShouldBlockRunEnd =>
		Instance != null && Instance.config.arenaEnabled && Instance.config.neverEndRun;

	private void Awake()
	{
		// Config is not loaded yet here; do not read it.
		Instance = this;
	}

	private void Start()
	{
		// Required by the modding terms of service for server-side mods that
		// change gameplay. Adds the MOD badge and warns joining players.
		instance.isAlteringGameplay = true;

		base.harmony.PatchAll();

		// Re-assert the rules whenever the game manager comes up, which covers
		// starting a run and restarting one.
		CallOnNetworkedManager<GameManager>(OnGameManagerStarted);

		// Keep relations correct as people join and leave the lobby.
		DewPlayer.onHumanPlayerAdded += OnRosterChanged;
		DewPlayer.onHumanPlayerRemoved += OnRosterChanged;

		Debug.Log($"{ModIdentity.LogPrefix}loaded: {base.mod.metadata.name} " +
		          $"v{base.mod.metadata.modVer} by {base.mod.metadata.author} " +
		          $"(arena {(config.arenaEnabled ? "ON" : "off")}, {config.teamMode})");
	}

	private void OnDestroy()
	{
		DewPlayer.onHumanPlayerAdded -= OnRosterChanged;
		DewPlayer.onHumanPlayerRemoved -= OnRosterChanged;

		// Leave the session playable if the mod is unloaded mid-run.
		if (NetworkServer.active)
		{
			RelationController.ClearAll();
			ArenaRules.Restore();
		}

		InjectedUi.CleanupAll();
		base.harmony.UnpatchAll(base.harmony.Id);

		if (Instance == this)
		{
			Instance = null;
		}

		Debug.Log($"{ModIdentity.LogPrefix}unloaded.");
	}

	/// <summary>Called by the game after the mod-manager config screen is applied.</summary>
	public override void OnConfigChanged()
	{
		ApplyNow();
	}

	private void OnGameManagerStarted(GameManager game)
	{
		if (!NetworkServer.active)
		{
			// Client-side instance of the manager; nothing for us to do.
			return;
		}

		_revives.Reset();
		ArenaStage.Reset();
		_phase.Reset();
		PveSuppressor.Reset();
		CampRift.Reset();
		ApplyNow();

		// Deferred: the world is not generated yet on the frame the manager starts.
		Dew.CallDelayed(() => ArenaRules.ApplyArenaRoom(config));
	}

	/// <summary>Drives the revive timers. Runs on the host only.</summary>
	private void Update()
	{
		float dt = Time.deltaTime;
		_phase.Tick(config);
		_revives.Tick(dt, config);
		ArenaStage.Tick(dt, config);
		PveSuppressor.Tick(dt, config, _phase.Current);
		CampRift.Tick(dt, config, _phase.Current);
	}

	private void OnRosterChanged(DewPlayer _)
	{
		if (NetworkServer.active && config.arenaEnabled)
		{
			// A late joiner must land on the right side of the camp/arena line.
			_phase.Reapply(config);
		}
	}

	/// <summary>Pushes the current config onto the live session.</summary>
	public void ApplyNow()
	{
		if (!NetworkServer.active)
		{
			return;
		}

		ArenaRules.Apply(config);
		ArenaRules.ApplyArenaRoom(config);

		// Hostility depends on where the players are, not just on the mode.
		if (config.arenaEnabled)
		{
			_phase.Reapply(config);
		}
		else
		{
			RelationController.Apply(TeamMode.Peaceful);
		}
	}

	/// <summary>Applies the config and writes it to disk. Used by the in-lobby window.</summary>
	public void ApplyAndPersist()
	{
		ApplyNow();

		try
		{
			SaveConfigsToDisk();
		}
		catch (Exception e)
		{
			Debug.LogError($"{ModIdentity.LogPrefix}failed to save config: {e}");
		}

		Debug.Log($"{ModIdentity.LogPrefix}config applied: arena={config.arenaEnabled}, " +
		          $"mode={config.teamMode}, neverEnd={config.neverEndRun}, " +
		          $"noMonsters={config.suppressMonsters}, room={config.arenaRoom}, " +
		          $"revive={config.autoReviveSeconds}s@{config.reviveHealthPercent}%");
	}

	// ---- console commands (host, developer mode: F1) ----

	[ConsoleCommand("Open the Dream Arena config window", "arena")]
	public void Cmd_Arena()
	{
		ArenaConfigWindow.Toggle();
	}

	[ConsoleCommand("Turn arena mode on and apply it", "arena_on")]
	public void Cmd_ArenaOn()
	{
		config.arenaEnabled = true;
		ApplyAndPersist();
	}

	[ConsoleCommand("Turn arena mode off and restore stock rules", "arena_off")]
	public void Cmd_ArenaOff()
	{
		config.arenaEnabled = false;
		ApplyNow();
		ArenaRules.Restore();
		ApplyAndPersist();
	}

	[ConsoleCommand("Set team mode: FreeForAll, TwoTeams or Peaceful", "arena_mode")]
	public void Cmd_ArenaMode(string mode)
	{
		if (!Enum.TryParse(mode, ignoreCase: true, out TeamMode parsed))
		{
			Debug.Log($"{ModIdentity.LogPrefix}unknown mode '{mode}'. " +
			          "Use FreeForAll, TwoTeams or Peaceful.");
			return;
		}

		config.teamMode = parsed;
		ApplyAndPersist();
	}

	[ConsoleCommand("Report the current arena state", "arena_status")]
	public void Cmd_ArenaStatus()
	{
		GameManager game = NetworkedManagerBase<GameManager>.instance;

		Debug.Log($"{ModIdentity.LogPrefix}arena={config.arenaEnabled} mode={config.teamMode} " +
		          $"neverEnd={config.neverEndRun} noMonsters={config.suppressMonsters} " +
		          $"room={config.arenaRoom} revive={config.autoReviveSeconds}s " +
		          $"phase={_phase.Current} " +
		          $"| server={NetworkServer.active} " +
		          $"players={RelationController.GetHumanPlayers().Count} " +
		          $"gameOverEnabled={(game != null ? game.isGameOverEnabled.ToString() : "n/a")}");
	}


	[ConsoleCommand("Teleport everyone into the arena, as the boss trigger would", "arena_start")]
	public void Cmd_ArenaStart()
	{
		ArenaStage.StartFight();
	}

	[ConsoleCommand("Sweep all monsters, props and pickups from this room", "arena_purge")]
	public void Cmd_ArenaPurge()
	{
		int removed = PveSuppressor.Sweep();
		Debug.Log($"{ModIdentity.LogPrefix}purge removed {removed} object(s).");
	}

	[ConsoleCommand("Set the arena room, e.g. Room_Primus_Boss_0", "arena_room")]
	public void Cmd_ArenaRoom(string room)
	{
		config.arenaRoom = room;
		ApplyAndPersist();
	}

	[ConsoleCommand("Set auto-revive delay in seconds, 0 to disable", "arena_revive")]
	public void Cmd_ArenaRevive(int seconds)
	{
		config.autoReviveSeconds = Mathf.Max(0, seconds);
		ApplyAndPersist();
	}
}
