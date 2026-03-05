using CommunityToolkit.HighPerformance;
using LetheAISharp.API;
using LetheAISharp.Files;
using LetheAISharp.LLM;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LetheAISharp
{
    internal class TextPromptBuilder : IPromptBuilder
    {
        private List<string> vlm_pictures = [];
        private readonly List<SingleMessage> _prompt = [];
        private string grammar = string.Empty;

        public int Count => _prompt.Count;

        public int AddMessage(AuthorRole role, string message)
        {
            var msg = LLMEngine.Instruct.FormatSinglePrompt(role, LLMEngine.User, LLMEngine.Bot, message);
            var res = LLMEngine.GetTokenCount(msg);
            _prompt.Add(new SingleMessage(role, message));
            return res;
        }

        public int AddMessage(SingleMessage message)
        {
            var msg = LLMEngine.Instruct.FormatSinglePrompt(message.Role, message.User, message.Bot, message.Message);
            var res = LLMEngine.GetTokenCount(msg);
            _prompt.Add(message);
            return res;
        }

        public object GetFullPrompt()
        {
            var fullprompt = new StringBuilder();
            foreach (var prompt in _prompt) 
            {
                fullprompt.Append(LLMEngine.Instruct.FormatSingleMessage(prompt));
            }
            return fullprompt.ToString();
        }

        public async Task SetStructuredOutput(object classToConvert)
        {
            // Highest priority: extractable => let it provide grammar (handles caching/special cases)
            if (classToConvert is ILLMExtractableBase extract)
            {
                grammar = await extract.GetGrammar().ConfigureAwait(false);
                return;
            }

            // If a Type representing a class was provided
            Type? targetType = classToConvert as Type;
            if (targetType is null && classToConvert is not null)
            {
                var rt = classToConvert.GetType();
                if (rt.IsClass) targetType = rt;
            }

            if (targetType is not null && targetType.IsClass)
            {
                grammar = await InvokeEngineGetGrammarForType(targetType).ConfigureAwait(false);
                return;
            }

            // Fallback: nothing to set
            grammar = string.Empty;
        }

        public async Task SetStructuredOutput<ClassToConvert>()
        {
            // This blocks intentionally to respect the IPromptBuilder signature
            await SetStructuredOutput(typeof(ClassToConvert));
        }

        private static async Task<string> InvokeEngineGetGrammarForType(Type type)
        {
            var mi = typeof(LLMEngine).GetMethod(nameof(LLMEngine.GetGrammar), BindingFlags.Public | BindingFlags.Static);
            if (mi == null) return string.Empty;

            var generic = mi.MakeGenericMethod(type);
            var task = (Task<string>)generic.Invoke(null, null)!;
            return await task.ConfigureAwait(false);
        }

        public void UnsetStructuredOutput()
        {
            grammar = string.Empty;
        }

        public object PromptToQuery(AuthorRole responserole = AuthorRole.Assistant, double tempoverride = -1, int responseoverride = -1, bool? overridePrefill = null, bool forceAltRoles = false)
        {
            string fullquery;
            if (!forceAltRoles)
            {
                fullquery = (string)GetFullPrompt();
            }
            else 
            {
                // Use alternate roles for group conversations so it needs to end with User if responserole is Assistant
                var fullprompt = new StringBuilder();
                var currentrole = responserole == AuthorRole.Assistant ? AuthorRole.User : AuthorRole.Assistant;
                // let's go in reverse to flip roles
                for (int i = _prompt.Count - 1; i >= 0; i--)
                {
                    var prompt = _prompt[i];
                    // System prompts are always added as-is
                    if (prompt.Role == AuthorRole.System || prompt.Role == AuthorRole.SysPrompt)
                    {
                        fullprompt.Insert(0, LLMEngine.Instruct.FormatSingleMessage(prompt));
                        continue;
                    }
                    var roleToUse = currentrole;
                    
                    var userID = prompt.UserID;
                    var charID = prompt.CharID;

                    // If the original message was Assistant but we are flipping roles, we need to swap user and bot personas
                    if (prompt.Role == AuthorRole.Assistant && roleToUse == AuthorRole.User)
                    {
                        userID = prompt.CharID;
                        charID = prompt.UserID;
                    }
                    fullprompt.Insert(0, LLMEngine.Instruct.FormatSingleMessage(new SingleMessage(roleToUse, DateTime.Now, prompt.Message, charID, userID)));
                    // flip role for next message
                    currentrole = currentrole == AuthorRole.User ? AuthorRole.Assistant : AuthorRole.User;
                }
                fullquery = fullprompt.ToString();
            }


            if (responserole == AuthorRole.User)
            {
                fullquery += LLMEngine.Instruct.GetResponseStart(LLMEngine.User, overridePrefill);
            }
            else
            {
                fullquery += LLMEngine.Instruct.GetResponseStart(LLMEngine.Bot, overridePrefill);
            }
            fullquery = fullquery.TrimEnd();

            GenerationInput genparams = LLMEngine.Sampler.GetCopy();
            if (tempoverride >= 0)
                genparams.Temperature = tempoverride;
            else if (LLMEngine.ForceTemperature >= 0)
                genparams.Temperature = LLMEngine.ForceTemperature;
            genparams.Max_context_length = LLMEngine.MaxContextLength;
            genparams.Max_length = responseoverride == -1 ? LLMEngine.Settings.MaxReplyLength : responseoverride;
            genparams.Stop_sequence = LLMEngine.Instruct.GetStoppingStrings(LLMEngine.User, LLMEngine.Bot);
            genparams.Prompt = fullquery;
            genparams.Images = [.. vlm_pictures];
            if (!string.IsNullOrWhiteSpace(grammar))
                genparams.Grammar = grammar;
            return genparams;
        }

        public int InsertMessage(int index, AuthorRole role, string message)
        {
            if (index == _prompt.Count)
            {
                return AddMessage(role, message);
            }
            if (index > _prompt.Count)
                return -1;

            var msg = LLMEngine.Instruct.FormatSinglePrompt(role, LLMEngine.User, LLMEngine.Bot, message);
            var res = LLMEngine.GetTokenCount(msg);
            _prompt.Insert(index, new SingleMessage(role, message));
            return res;
        }

        public int InsertMessage(int index, SingleMessage message)
        {
            if (index == _prompt.Count)
            {
                return AddMessage(message);
            }
            if (index > _prompt.Count)
                return -1;

            var msg = LLMEngine.Instruct.FormatSinglePrompt(message.Role, message.User, message.Bot, message.Message);
            var res = LLMEngine.GetTokenCount(msg);
            _prompt.Insert(index, message);
            return res;
        }

        public void Clear()
        {
            _prompt.Clear();
        }

        public int GetTokenUsage()
        {
            return LLMEngine.GetTokenCount((string)GetFullPrompt()) + vlm_pictures.Count * LLMEngine.Settings.ImageEmbeddingSize;
        }

        public int GetTokenCount(AuthorRole role, string message)
        {
            var msg = LLMEngine.Instruct.FormatSinglePrompt(role, LLMEngine.User, LLMEngine.Bot, message);
            return LLMEngine.GetTokenCount(msg);
        }

        public int GetTokenCount(SingleMessage message)
        {
            var msg = LLMEngine.Instruct.FormatSinglePrompt(message.Role, message.User, message.Bot, message.Message);
            return LLMEngine.GetTokenCount(msg);
        }

        public string PromptToText()
        {
            return (string)GetFullPrompt();
        }

        public void VLM_ClearImages()
        {
            vlm_pictures = [];
        }

        public void VLM_AddImage(string imagePath, int size = 1024)
        {
            var res = ImageUtils.ImageToBase64(imagePath, size);
            if (!string.IsNullOrEmpty(res))
                vlm_pictures.Add(res);
        }

        public int VLM_GetImageCount() => vlm_pictures.Count;

    }
}
