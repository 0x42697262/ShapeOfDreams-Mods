namespace LastStand.Waves;

internal readonly struct WaveContent
{
	public int ForeignTypes { get; }

	public int MirageSkins { get; }

	public WaveContent(int foreignTypes, int mirageSkins)
	{
		ForeignTypes = foreignTypes;
		MirageSkins = mirageSkins;
	}
}
