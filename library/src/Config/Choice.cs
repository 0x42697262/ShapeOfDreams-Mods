namespace SodLib.Config;

internal sealed class Choice
{
	public Choice(string value, string detail)
	{
		Value = value;
		Detail = detail;
	}

	public string Value { get; }

	public string Detail { get; }
}
