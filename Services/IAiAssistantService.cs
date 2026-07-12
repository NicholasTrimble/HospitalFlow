namespace HospitalFlow.Services;

public interface IAiAssistantService
{
    Task<string> GetChatResponseAsync(string userPrompt, string hospitalContextData);
}