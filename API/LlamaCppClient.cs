using LetheAISharp.LLM;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using OpenAI.Responses;
using OpenAI.Threads;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetheAISharp.API
{

    public class LlamaCpp_APIClient : OpenAI_APIClient
    {
        public LlamaCpp_APIClient(HttpClient httpclient) : base(httpclient)
        {
            _httpClient = httpclient;
            _httpClient.DefaultRequestHeaders.ConnectionClose = false;
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
            var settings = new OpenAISettings(LLMEngine.Settings.BackendUrl);
            API = new OpenAIClient(new OpenAIAuthentication("123"), settings, _httpClient);
        }

        public override async Task<string> GetBackendInfo()
        {
            return await Task.FromResult("Llama.cpp Backend").ConfigureAwait(false);
        }

        public async Task<TokenList> TokenizeAsync(TokenRequest body, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<TokenList>(_httpClient!, HttpMethod.Post, "/tokenize", body, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public TokenList TokenizeSync(TokenRequest body)
        {
            // Using a new task and ConfigureAwait(false) to avoid deadlocks
            return Task.Run(() => TokenizeAsync(body)).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task<TokenCountResponse> GetTokenCountAsync(MessageListQuery body, CancellationToken cancellationToken = default)
        {
            return await SendRequestAsync<TokenCountResponse>(_httpClient!, HttpMethod.Post, "/v1/messages/count_tokens", body, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public TokenCountResponse GetTokenCountSync(MessageListQuery body)
        {
            // Using a new task and ConfigureAwait(false) to avoid deadlocks
            return Task.Run(() => GetTokenCountAsync(body)).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    public class TokenCountResponse
    {
        public int input_tokens { get; set; } = 0;
    }

    public class MessageListQuery
    {
        public string model { get; set; } = "gpt-4";
        public List<MessageQuery> messages { get; set; } = [];
    }

    public class MessageQuery(string role, string content)
    {
        public string role { get; set; } = role;
        public string content { get; set; } = content;

        public MessageQuery() : this("user", string.Empty) { }
    }

    public class TokenRequest
    {
        public string content { get; set; } = string.Empty;
        public bool add_special { get; set; } = false;
        public bool parse_special { get; set; } = true;
        public bool with_pieces { get; set; } = false;
    }

    public class TokenList
    {
        public List<int> tokens { get; set; } = [];
        
        public int GetTokenCount() => tokens.Count;
    }
}
