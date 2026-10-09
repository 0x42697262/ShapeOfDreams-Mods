namespace LastStand.Combat;

internal sealed class CurseSettings
{
	public double MildWaves { get; }

	public double PotentWaves { get; }

	public double PowerfulWaves { get; }

	public CurseSettings(double mildWaves, double potentWaves, double powerfulWaves)
	{
		MildWaves = mildWaves;
		PotentWaves = potentWaves;
		PowerfulWaves = powerfulWaves;
	}

	public double WavesFor(HatredStrengthType strength)
	{
		switch (strength)
		{
			case HatredStrengthType.Mild:
				return MildWaves;
			case HatredStrengthType.Potent:
				return PotentWaves;
			default:
				return PowerfulWaves;
		}
	}
}
