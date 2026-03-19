using LetheAISharp.Agent.Tools;
using LetheAISharp.API;
using LetheAISharp.Files;
using LetheAISharp.LLM;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LetheAISharp
{
    internal class ChatPromptBuilder : IPromptBuilder
    {
        private readonly List<Message> _prompt = [];
        private OpenAI.JsonSchema? _currentSchema = null;
        private List<string> imagefilepath = [];

        public int Count => _prompt.Count;

        public int AddMessage(AuthorRole role, string message)
        {
            var single = new SingleMessage(role, message);
            var msg = FormatSingleMessage(single);
            _prompt.Add(msg);
            return GetTokenCount(single);
        }

        public int AddMessage(SingleMessage message)
        {
            var msg = FormatSingleMessage(message);
            _prompt.Add(msg);
            var cost = GetTokenCount(message);
            return cost;
        }

        public object GetFullPrompt()
        {
            return _prompt;
        }

        public int GetTokenUsage()
        {
            var total = 0;
            if (LLMEngine.UseToolCallsInPrompt)
                total += LLMEngine.ToolManager.EstimatedTokenCost();
            foreach (var message in _prompt)
            {
                total += GetTokenCountMsg(message);
            }
            return total;
        }

        public int InsertMessage(int index, AuthorRole role, string message)
        {
            if (index == _prompt.Count)
            {
                return AddMessage(new SingleMessage(role, message));
            }
            var msg = FormatSingleMessage(new SingleMessage(role, message));
            _prompt.Insert(index, msg);
            return GetTokenCountMsg(msg);
        }

        public int InsertMessage(int index, SingleMessage message)
        {
            if (index == _prompt.Count)
            {
                return AddMessage(message);
            }
            var msg = FormatSingleMessage(message);
            _prompt.Insert(index, msg);
            var cost = GetTokenCount(message);
            return cost;
        }

        private string GetResponseStart(BasePersona talker, bool? overridePrefill = null)
        {
            var addname = LLMEngine.NamesInPromptOverride ?? LLMEngine.Instruct.AddNamesToPrompt;
            var res = string.Empty;
            if (addname && (!LLMEngine.Instruct.IsThinkFormat || LLMEngine.Settings.DisableThinking))
                res += talker.Name + ":";
            if (talker.IsUser)
                return res;
            var doprefill = overridePrefill ?? LLMEngine.Instruct.PrefillThinking;
            if (doprefill)
            {
                res += LLMEngine.Instruct.GetThinkPrefill();
                if (addname)
                {
                    res += $" It's {talker.Name} turn to talk.";
                }
            }
            return res;
        }

        public object PromptToQuery(AuthorRole responserole = AuthorRole.Assistant, double tempoverride = -1, int responseoverride = -1, bool? overridePrefill = null, bool forceAltRoles = false)
        {
            var finalprompt = new List<Message>(_prompt);
            var cleanimages = !LLMEngine.SupportsVision || LLMEngine.Settings.MaxImageCount > 0;
            var maxallowed = LLMEngine.SupportsVision ? LLMEngine.Settings.MaxImageCount : 0;

            if (cleanimages)
            {
                // traverse the list and remove oldest images until we are within the limit
                var count = 0;
                for (int i = finalprompt.Count - 1; i >= 0; i--)
                {
                    var mess = finalprompt[i];
                    if (mess.Content is List<Content> lst)
                    {
                        var hasimage = false;
                        foreach (var content in lst)
                        {
                            if (content.Type == ContentType.ImageUrl)
                            {
                                hasimage = true;
                                break;
                            }
                        }
                        if (hasimage)
                        {
                            count++;
                            if (count > LLMEngine.Settings.MaxImageCount)
                            {
                                lst.RemoveAll(c => c.Type == ContentType.ImageUrl);
                            }
                        }
                    }
                }
            }

            var prefill = overridePrefill ?? LLMEngine.Instruct.PrefillThinking;
            if (LLMEngine.Client?.AllowPrefill == false)
                prefill = false;
            // prefilling is not available when using tool calls in prompt or when a structured output schema is set,
            // as it would interfere with the format of the response
            if (prefill && (!LLMEngine.UseToolCallsInPrompt || !LLMEngine.ToolManager.HasTools()) && _currentSchema is null)
            {
                var info = GetResponseStart(LLMEngine.Bot);
                if (!string.IsNullOrWhiteSpace(info))
                {
                    finalprompt.Add(new Message(role: Role.Assistant, content: info, name: "prefix"));
                }
            }

            var dooverride = (LLMEngine.Client is LlamaCppAdapter) && LLMEngine.Settings.BackendLLamaCppAllowAllSamplers;
            double? temp = tempoverride >= 0 ? tempoverride : (LLMEngine.ForceTemperature >= 0) ? LLMEngine.ForceTemperature : LLMEngine.Sampler.Temperature;
            int? setseed = LLMEngine.Sampler.Sampler_seed != -1 ? LLMEngine.Sampler.Sampler_seed : null;
            if (dooverride)
            {
                temp = null;
                setseed = null;
            }

            if (LLMEngine.UseToolCallsInPrompt && LLMEngine.ToolManager.HasTools() && _currentSchema is null)
            {
                return new ChatRequest(finalprompt,
                    tools: LLMEngine.ToolManager.GetToolList(),
                    toolChoice: "auto",
                    topP: dooverride ? null : LLMEngine.Sampler.Top_p,
                    frequencyPenalty: dooverride ? null : LLMEngine.Sampler.Rep_pen - 1,
                    seed: setseed,
                    user: LLMEngine.NamesInPromptOverride ?? LLMEngine.Instruct.AddNamesToPrompt ? LLMEngine.User.Name : null,
                    stops: [.. LLMEngine.Instruct.GetStoppingStrings(LLMEngine.User, LLMEngine.Bot)],
                    responseFormat: TextResponseFormat.Auto,
                    parallelToolCalls: LLMEngine.Client?.SupportParallelToolCall ?? false,
                    maxTokens: responseoverride == -1 ? LLMEngine.Settings.MaxReplyLength : responseoverride,
                    temperature: temp);
            }
            else
            {
                return new ChatRequest(finalprompt,
                    topP: dooverride ? null : LLMEngine.Sampler.Top_p,
                    frequencyPenalty: dooverride ? null : LLMEngine.Sampler.Rep_pen - 1,
                    seed: setseed,
                    user: LLMEngine.NamesInPromptOverride ?? LLMEngine.Instruct.AddNamesToPrompt ? LLMEngine.User.Name : null,
                    stops: [.. LLMEngine.Instruct.GetStoppingStrings(LLMEngine.User, LLMEngine.Bot)],
                    responseFormat: _currentSchema is not null ? TextResponseFormat.JsonSchema : TextResponseFormat.Auto,
                    jsonSchema: _currentSchema,
                    parallelToolCalls: LLMEngine.Client?.SupportParallelToolCall ?? false,
                    maxTokens: responseoverride == -1 ? LLMEngine.Settings.MaxReplyLength : responseoverride,
                    temperature: temp);
            }

        }

        public void Clear()
        {
            _prompt.Clear();
        }

        public int GetTokenCount(AuthorRole role, string message)
        {
            return GetTokenCount(new SingleMessage(role, message));
        }

        public int GetTokenCount(SingleMessage message)
        {
            var total = LLMEngine.GetTokenCount(message.ToTextCompletion());

            if (message.ImagePath is not null && File.Exists(message.ImagePath) && LLMEngine.SupportsVision)
            {
                total += LLMEngine.Settings.ImageEmbeddingSize;
            }
            if (message.Role == AuthorRole.Assistant && message.ToolCalls.Count > 0)
            {
                total += LLMEngine.GetTokenCount(message.ToolCallToString());
            }
            return total;
        }

        private int GetTokenCountMsg(Message msg)
        {
            var total = 0;
            if (msg.Content is List<Content> lst)
            {
                foreach (var content in lst)
                {
                    if (content.Type == ContentType.ImageUrl)
                    {
                        if (LLMEngine.SupportsVision)
                            total += LLMEngine.Settings.ImageEmbeddingSize;
                    }
                    else
                        total += LLMEngine.GetTokenCount(content.ToString());
                }
            }
            else
            {
                total += LLMEngine.GetTokenCount(msg.Content as string ?? string.Empty);
            }
            if (msg.ToolCalls?.Count > 0 && (msg.Role == Role.Assistant || msg.Role == Role.Tool))
            {
                foreach (var toolCall in msg.ToolCalls)
                {
                    var approxstring = $"[ToolCall: {toolCall.Name}-{toolCall.CallId}]\n[Param: {toolCall.Arguments.ToJsonString()}]\n";
                    total += LLMEngine.GetTokenCount(approxstring);
                }
            }
            return total + 8;
        }

        public string PromptToText()
        {
            var sb = new StringBuilder();
            foreach (var message in _prompt)
            {
                if (message.Role == OpenAI.Role.User)
                    sb.AppendLine(LLMEngine.User.Name + ": " + message.Content.ToString());
                else if (message.Role == OpenAI.Role.Assistant)
                    sb.AppendLine(LLMEngine.Bot.Name + ": " + message.Content.ToString());
                else
                    sb.AppendLine("SYSTEM" + ": " + message.Content.ToString());
            }
            return sb.ToString();
        }

        private static Message FormatSingleMessage(SingleMessage message)
        {
            // Tool result messages: skip all name/image logic
            if (message.Role == AuthorRole.Tool && message.ToolCalls.Count > 0)
            {
                return new Message(Role.Tool, message.Message);
            }

            // Assistant tool-call-only messages: skip all name/image logic
            if (message.Role == AuthorRole.Assistant && message.ToolCalls?.Count > 0 && string.IsNullOrEmpty(message.Message))
            {
                var tc = new ToolCall(message.ToolCalls[0].CallId, message.ToolCalls[0].FunctionName, JsonNode.Parse(message.ToolCalls[0].ArgumentsJson));
                return new Message(tc, message.ToolCallToString());
            }

            var realprompt = message.Message;
            var addname = LLMEngine.NamesInPromptOverride ?? LLMEngine.Instruct.AddNamesToPrompt;

            // In group conversations, ALWAYS add names so the LLM knows which persona is speaking
            if (message.Bot is GroupPersonaBase)
                addname = true;

            if (message.Role != AuthorRole.Assistant && message.Role != AuthorRole.User)
                addname = false;
            string? selname = null;
            if (addname)
            {
                if (message.Role == AuthorRole.Assistant)
                {
                    selname = message.Bot.Name;
                }
                else if (message.Role == AuthorRole.User)
                {
                    selname = message.User.Name;
                }
            }

            if (!LLMEngine.SupportsVision || string.IsNullOrEmpty(message.ImagePath) || !File.Exists(message.ImagePath))
                return new Message(TokenTools.InternalRoleToChatRole(message.Role), message.Bot.ReplaceMacros(realprompt, message.User), selname);

            var content = new List<Content>();

            var extension = Path.GetExtension(message.ImagePath).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    content.Add(new(ContentType.ImageUrl, $"data:image/jpeg;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                case ".png":
                    content.Add(new(ContentType.ImageUrl, $"data:image/png;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                case ".gif":
                    content.Add(new(ContentType.ImageUrl, $"data:image/gif;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                case ".bmp":
                    content.Add(new(ContentType.ImageUrl, $"data:image/bmp;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                case ".webp":
                    content.Add(new(ContentType.ImageUrl, $"data:image/webp;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                case ".tiff ":
                    content.Add(new(ContentType.ImageUrl, $"data:image/tiff;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
                default:
                    content.Add(new(ContentType.ImageUrl, $"data:image/gif;base64,{ImageUtils.ImageToBase64(message.ImagePath, 1024)!}"));
                    break;
            }
            content.Add(message.Bot.ReplaceMacros(realprompt, message.User));

            return new Message(TokenTools.InternalRoleToChatRole(message.Role), content);
        }

        public async Task SetStructuredOutput<ClassToConvert>()
        {
            _currentSchema = typeof(ClassToConvert);
            await Task.Delay(1).ConfigureAwait(false);
        }

        public async Task SetStructuredOutput(object classToConvert)
        {
            _currentSchema = classToConvert.GetType();
            await Task.Delay(1).ConfigureAwait(false);
        }

        public void UnsetStructuredOutput()
        {
            _currentSchema = null;
        }

        public void VLM_ClearImages()
        {
            imagefilepath.Clear();
        }

        public void VLM_AddImage(string imagePath, int size = 1024)
        {
            if (File.Exists(imagePath) && !imagefilepath.Contains(imagePath))
                imagefilepath.Add(imagePath);
        }

        public int VLM_GetImageCount() => imagefilepath.Count;
    }
}
