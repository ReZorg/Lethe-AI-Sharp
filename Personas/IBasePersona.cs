using LetheAISharp.Memory;
using LetheAISharp.Files;

namespace LetheAISharp.LLM
{
    public interface IBasePersona
    {
        bool AgentMode { get; set; }
        List<string> AgentTasks { get; set; }
        string Bio { get; set; }
        Brain Brain { get; set; }
        bool DatesInSessionSummaries { get; set; }
        bool DisableBotGuidance { get; set; }
        List<string> ExampleDialogs { get; set; }
        List<string> FirstMessage { get; set; }
        Chatlog History { get; set; }
        bool IsUser { get; set; }
        List<WorldInfo> MyWorlds { get; }
        string Name { get; set; }
        List<string> Plugins { get; set; }
        string Scenario { get; set; }
        string SelfEditField { get; set; }
        int SelfEditTokens { get; set; }
        bool SenseOfTime { get; set; }
        string SystemPrompt { get; set; }
        List<string> Worlds { get; set; }

        void BeginChat();
        void ClearChatHistory(string path, bool deletefile = true);
        void EndChat(bool backup = false);
        string GetBio(string othername);
        string GetDialogExamples(string othername);
        string GetIdentifier();
        string GetScenario(string othername);
        string GetWelcomeLine(string othername);
        void LoadChatHistory();
        string ReplaceMacros(string inputText);
        string ReplaceMacros(string inputText, BasePersona user);
        void SaveChatHistory(bool backup = false);
        void SaveToFile(string path, string? fileName = null);
        Task UpdateSelfEditSection();
    }
}