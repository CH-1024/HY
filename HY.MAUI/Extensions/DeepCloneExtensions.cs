using System.Text.Json;

namespace HY.MAUI.Extensions
{
    public static class DeepCloneExtensions
    {
        public static T? DeepClone<T>(this T source) where T : class
        {
            if (source == null) return null;

            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(json);
        }
    }

}
