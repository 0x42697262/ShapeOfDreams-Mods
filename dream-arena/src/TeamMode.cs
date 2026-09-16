namespace DreamArena;

/// <summary>
/// How players are divided when arena mode is on.
/// </summary>
/// <remarks>
/// Rendered as a dropdown in the config window: DewGUI has an enum field
/// builder, so no custom widget is needed.
/// </remarks>
public enum TeamMode
{
	/// <summary>Everyone hostile to everyone.</summary>
	FreeForAll,

	/// <summary>Players split into two teams by join order (even/odd).</summary>
	TwoTeams,

	/// <summary>Relations left alone. Useful for turning PvP off without unloading the mod.</summary>
	Peaceful,
}
