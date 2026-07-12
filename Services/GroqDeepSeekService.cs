using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HospitalFlow.Services;

public class GroqDeepSeekService : IAiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string ModelName = "llama-3.3-70b-versatile";

    public GroqDeepSeekService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:GroqApiKey"] ?? string.Empty;
    }

    public async Task<string> GetChatResponseAsync(string userPrompt, string hospitalContextData)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "AI Assistant configuration missing. Please verify the Groq API key.";
        }

        var systemPrompt = $"You are the HospitalFlow AI Operations Assistant. You only help healthcare workers manage room capacity and equipment tracking status. " +
                           $"Do not answer unrelated queries. Here is the current live database state:\n{hospitalContextData}";

        // Use explicit KeyValuePair string assignments to force exact casing compliance
        var requestData = new Dictionary<string, object>
    {
        { "model", ModelName },
        { "messages", new[]
            {
                new Dictionary<string, string> { { "role", "system" }, { "content", systemPrompt } },
                new Dictionary<string, string> { { "role", "user" }, { "content", userPrompt } }
            }
        },
        { "temperature", 0.6 }
    };

        var jsonPayload = JsonSerializer.Serialize(requestData);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                // This will display the exact reason Groq threw a 400 bad request error
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"Error connecting to AI backend. Status: {response.StatusCode} - Details: {errorContent}";
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            return $"An error occurred: {ex.Message}";
        }
    }
}