using System.Text.Json.Nodes;

namespace Uta.Server.Application.Common;

public static class GameLogJson
{
    public static JsonArray Ints(IEnumerable<int> values) => new JsonArray(values.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());

    public static JsonArray Strings(IEnumerable<string> values) => new JsonArray(values.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
}
