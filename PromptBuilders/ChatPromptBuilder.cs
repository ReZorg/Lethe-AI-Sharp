using LetheAISharp.Files;
using LetheAISharp.LLM;
using CommunityToolkit.HighPerformance;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI;

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
            var msg = FormatSingleMessage(new SingleMessage(role, message));
            _prompt.Add(msg);
            return LLMEngine.GetTokenCount(msg);
        }

        public int AddMessage(SingleMessage message)
        {
            var msg = FormatSingleMessage(message);
            _prompt.Add(msg);
            return LLMEngine.GetTokenCount(msg);
        }

        private int GetTokenCountMsg(Message msg)
        {
            if (msg.Content is List<Content> lst)
            {
                var total = 0;
                foreach (var content in lst)
                {
                    total += LLMEngine.GetTokenCount(content.ToString()) + 4;
                }
                return total + 4;
            }
            return LLMEngine.GetTokenCount(msg.Content.ToString()) + 4;
        }

        public object GetFullPrompt()
        {
            return _prompt;
        }

        public int GetTokenUsage()
        {
            var total = 0;
            foreach (var message in _prompt)
            {
                total += LLMEngine.GetTokenCount(message);
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
            return LLMEngine.GetTokenCount(msg.Content.ToString()) + 4;
        }

        public int InsertMessage(int index, SingleMessage message)
        {
            if (index == _prompt.Count)
            {
                return AddMessage(message);
            }
            var msg = FormatSingleMessage(message);
            _prompt.Insert(index, msg);
            return LLMEngine.GetTokenCount(msg.Content.ToString()) + 4;
        }

        public object PromptToQuery(AuthorRole responserole = AuthorRole.Assistant, double tempoverride = -1, int responseoverride = -1, bool? overridePrefill = null)
        {
            var chatrq = new ChatRequest(_prompt,
                topP: LLMEngine.Sampler.Top_p,
                frequencyPenalty: LLMEngine.Sampler.Rep_pen - 1,
                seed: LLMEngine.Sampler.Sampler_seed != -1 ? LLMEngine.Sampler.Sampler_seed : null,
                user: LLMEngine.NamesInPromptOverride ?? LLMEngine.Instruct.AddNamesToPrompt ? LLMEngine.User.Name : null,
                stops: [.. LLMEngine.Instruct.GetStoppingStrings(LLMEngine.User, LLMEngine.Bot)],
                responseFormat: _currentSchema is not null ? OpenAI.TextResponseFormat.JsonSchema : OpenAI.TextResponseFormat.Auto,
                jsonSchema: _currentSchema,
                maxTokens: responseoverride == -1 ? LLMEngine.Settings.MaxReplyLength : responseoverride,
                temperature: tempoverride >= 0 ? tempoverride : (LLMEngine.ForceTemperature >= 0) ? LLMEngine.ForceTemperature : LLMEngine.Sampler.Temperature);

            return chatrq;
        }

        public void Clear()
        {
            _prompt.Clear();
        }

        public int GetTokenCount(AuthorRole role, string message)
        {
            var msg = FormatSingleMessage(new SingleMessage(role, message));
            return LLMEngine.GetTokenCount(msg.Content.ToString());
        }

        public int GetTokenCount(SingleMessage message)
        {
            var msg = FormatSingleMessage(message);
            return LLMEngine.GetTokenCount(msg.Content.ToString());
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

            if (string.IsNullOrEmpty(message.ImagePath) || !File.Exists(message.ImagePath))
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
