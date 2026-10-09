using Newtonsoft.Json.Linq;

namespace LastStand.Config;

internal static class ConfigSchema
{
	public const int Current = 40;

	public static bool IsCurrent(string json)
	{
		JToken jToken = JObject.Parse(json)["schemaVersion"];
		if (jToken != null && jToken.Type == JTokenType.Integer)
		{
			return jToken.Value<int>() == 40;
		}
		return false;
	}
}
