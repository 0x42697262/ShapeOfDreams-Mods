namespace LastStand.Waves;

internal sealed class EnvironmentTally
{
	private const int NoWave = 0;

	private int _block = EnvironmentRule.NoBlock;

	private int _lastWave = NoWave;

	private int _count;

	public int CountIn(int block)
	{
		return block != EnvironmentRule.NoBlock && block == _block ? _count : 0;
	}

	public void Record(int wave, int block)
	{
		if (block == EnvironmentRule.NoBlock || wave == _lastWave)
		{
			return;
		}

		if (block != _block)
		{
			_block = block;
			_count = 0;
		}

		_count++;
		_lastWave = wave;
	}

	public void Reset()
	{
		_block = EnvironmentRule.NoBlock;
		_lastWave = NoWave;
		_count = 0;
	}
}
