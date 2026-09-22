using System.Text.Json;

namespace KnowledgeAssistant.Application.DTOs.API
{
    public class ApiResult
    {
        public bool IsSuccessful { get; set; }
        public object? Data { get; set; }
        public string? Message { get; set; }

        public T? GetData<T>()
        {
            if (Data is null)
            {
                return default;
            }

            if (Data is T typedData)
            {
                return typedData;
            }

            if (Data is JsonElement jsonData)
            {
                return jsonData.Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }

            return default;
        }
    }
}
