using Newtonsoft.Json.Linq;

namespace DreamArena.Config;

internal static class ConfigSchema
{
	public const int Current = 2;

	public static bool IsCurrent(string json)
	{
		JToken version = JObject.Parse(json)[nameof(DreamArenaConfig.schemaVersion)];
		return version != null && version.Type == JTokenType.Integer && version.Value<int>() == Current;
	}
}
