using System.Text.Json;

namespace HY.ApiService.Models
{
    public class Response
    {
        public bool IsSucc { get; set; } = false;
        public string? Msg { get; set; } = null;
        public Dictionary<string, object?>? Data { get; set; } = null;

        public Response()
        {
            
        }

        public Response(bool isSucc)
        {
            IsSucc = isSucc;
        }

        public Response(bool isSucc, string? msg)
        {
            IsSucc = isSucc;
            Msg = msg;
        }

        public Response(bool isSucc, string msg, Dictionary<string, object?> data)
        {
            IsSucc = isSucc;
            Msg = msg;
            Data = data;
        }


        public T? GetValue<T>(string key)
        {
            // 1. 检查 Data 是否为 null
            if (Data == null || !Data.TryGetValue(key, out object? value) || value == null)
            {
                return default;
            }

            // 2. 直接类型匹配（如 value 是 T 类型）
            if (value is T typedValue)
            {
                return typedValue;
            }

            // 3. 处理 System.Text.Json 的 JsonElement
            if (value is JsonElement jsonElement)
            {
                return GetValueFromJsonElement<T>(jsonElement);
            }

            // 4. 尝试 JSON 反序列化（统一使用 System.Text.Json）
            var json = JsonSerializer.Serialize(value);
            return JsonSerializer.Deserialize<T>(json);
        }

        private static T? GetValueFromJsonElement<T>(JsonElement jsonElement)
        {
            try
            {
                return jsonElement.ValueKind switch
                {
                    JsonValueKind.True => (T)(object)true,
                    JsonValueKind.False => (T)(object)false,
                    JsonValueKind.Number when typeof(T) == typeof(int) => (T)(object)jsonElement.GetInt32(),
                    JsonValueKind.Number when typeof(T) == typeof(double) => (T)(object)jsonElement.GetDouble(),
                    JsonValueKind.String => (T)(object)jsonElement.GetString()!,
                    _ => JsonSerializer.Deserialize<T>(jsonElement.GetRawText())
                };
            }
            catch
            {
                return default;
            }
        }
    }
}
