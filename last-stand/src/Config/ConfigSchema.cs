using Newtonsoft.Json.Linq;

namespace LastStand.Config;

internal static class ConfigSchema
{
	public const int Current = 3;

	public static bool IsCurrent(string json)
	{
		JToken version = JObject.Parse(json)[nameof(LastStandConfig.schemaVersion)];
		return version != null && version.Type == JTokenType.Integer && version.Value<int>() == Current;
	}
}
