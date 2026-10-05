namespace LastStand.Waves;

internal readonly struct WaveContent
{
	public int ForeignTypes { get; }

	public int MirageSkins { get; }

	public int HeroicBossTypes { get; }

	public WaveContent(int foreignTypes, int mirageSkins, int heroicBossTypes)
	{
		ForeignTypes = foreignTypes;
		MirageSkins = mirageSkins;
		HeroicBossTypes = heroicBossTypes;
	}
}
