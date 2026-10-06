namespace LastStand.Config;

internal sealed class Choice
{
	public Choice(string value, string world)
	{
		Value = value;
		World = world;
	}

	public string Value { get; }

	public string World { get; }
}
