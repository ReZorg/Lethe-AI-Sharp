# Lethe AI - A C# Middleware LLM Library

## 🚀 What Is It?

Lethe AI Sharp is a modular, object‑oriented C# library that connects local or remote Large Language Model (LLM) backends to your applications (desktop tools, game engines, services).

It unifies: chat personas, conversation/session management, streaming inference, long‑term memory, RAG (retrieval augmented generation), background agentic tasks, web search tools, TTS, and structured output generation.

It is extensible, documented, backend-agnostic (you write the same code no matter which backend is being used), and written 100% in C#.

## 🔥 Minimal Example

```csharp
// 1. Setup (choose backend style)
LLMEngine.Setup("http://localhost:1234", BackendAPI.OpenAICompatible);

// 2. Connect
await LLMEngine.Connect();
if (LLMEngine.Status != SystemStatus.Ready)
    throw new Exception("Backend not ready");

// 3. One-shot generation
var pb = LLMEngine.GetPromptBuilder();
pb.AddMessage(AuthorRole.SysPrompt, "You're an helpful and friendly bot!");
pb.AddMessage(AuthorRole.User, "Explain gravity in one friendly paragraph.");
var query = pb.PromptToQuery();
var reply = await LLMEngine.SimpleQuery(query);
Console.WriteLine(reply.Text);

// 4. Streaming variant (with cancellation)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
LLMEngine.OnInferenceStreamed += (_, token) => Console.Write(token);
await LLMEngine.SimpleQueryStreaming(query, cts.Token);
```

## 🧩 Compatible Backends
- **Kobold API:** Used by [KoboldCpp](https://github.com/LostRuins/koboldcpp).
- **OpenAI API:** Used by [LM Studio](https://lmstudio.ai/), [Text Generation WebUI](https://github.com/oobabooga/text-generation-webui), and many more.

Remote endpoints should work but primary focus remains local / LAN latency.

| Capability | Kobold API | OpenAI-Compatible |
|------------|------------|-------------------|
| Basic text generation | ✅ | ✅ |
| Streaming | ✅ | ✅ |
| Structured output | ✅ GBNF Grammar | ✅ JSON Schema |
| CoT / “thinking” prompts | ✅ | ✅ |
| Personas & chat sessions | ✅ | ✅ |
| Memory summaries | ✅ | ✅ |
| RAG integration | ✅ | ✅ |
| Web search integration | ✅ | ✅ |
| Text To Speech | ✅ (if loaded) | ❌ |
| VLM (image input)* | ✅ (if loaded) | ✅ |

\* VLM support depends entirely on underlying server and LLM capabilities. KoboldAPI has notoriously bad image input support.

## ⭐ Core Features

- Prompt + Persona system (bot & user role objects, instruction formats)
- Session chatlog with automated summarization
- LLM message streaming support
- Long‑term memory system + world info triggers
- RAG with vector search (HNSW) + embeddings
- Extensible background “agentic tasks” (search, summarization)
- Structured output (GBNF / JSON schema) for tool pipelines
- Web search integration (DuckDuckGo, Brave API)
- Text utilities (token counting, segmentation, summarization helpers)
- Experimental group chat (multi‑AI personas)
- Visual language model support (depending on the back-end)

## 📝 Long Term Memory and RAG
- Summaries of recent chat sessions into the system prompt
- Keyword-triggered text insertions (also known as "world info" in many frontends)
- Automatic and configurable insertion of relevant chat summaries into the context
- Customizable RAG system using the Small World implementation

## 🧠 Agentic System
- Customizable tasks can run in the background (while the user is AFK for instance)
- Includes 2 default tasks that run relevant web searches and mention results in following chat session
- Write your own tasks easily to boost your bot's abilities

## 🛠️ Advanced Features (Work in progress / experimental)
- Group chat functionalities (one user and multiple AI characters)
- Sentiment analysis

## 👀 See it in action

To demonstrate how powerful **Lethe AI** can be, check out [Lethe AI Chat](https://github.com/SerialKicked/Lethe-AI-Chat/). This is a powerful AI chat program for _Windows_ that uses most of the features present in the library. It comes with its own integrated editors, extended agentic tasks, and extensive settings. It can rival with most of the dedicated AI chat programs currently available.

## 📦 Installation

Right now, the best way to use the library is to add this repo as a submodule or project reference in your C# solution. NuGet package coming soon.

**Optional Models & Data Files**
Place them into `data/classifiers/` (configure their *build action* to “Copy if newer”):
| File | Purpose | Required? |
|---------|------|-----------|
| [gte-large.Q6_K.gguf](https://huggingface.co/SerialKicked/Lethe-AI-Repo/resolve/main/gte-large.Q6_K.gguf?download=true) | Embeddings for RAG & Memory similarity | Yes for everything memory or RAG related |
| [emotion-bert-classifier.gguf](https://huggingface.co/SerialKicked/Lethe-AI-Repo/resolve/main/emotion-bert-classifier.gguf?download=true) | Sentiment / emotion (experimental) | No |


## 🔎 Usage and Documentation

**New users**: Start with the [Quick Start Guide](Docs/QUICKSTART.md) to get running in 5 minutes!

For comprehensive documentation, check the `Docs/` folder:
- [LLM System Documentation](Docs/LLMSYSTEM.md) - Core LLMEngine functionality, personas, and chat management
- [Instruction Format Guide](Docs/INSTRUCTFORMAT.md) - Configuring message formatting for different models
- [Memory System](Docs/MEMORY.md) - Understand the various memory systems and how they interact
- [Examples](Docs/Examples/) - Working code samples and tutorials

## 🤝 Third Party Libraries

*Lethe AI Sharp* relies on the following libraries and tools to work.
- [LlamaSharp](https://github.com/SciSharp/LLamaSharp/) - Used as a backend-agnostic embedding system
- [General Text Embedding - Large](https://huggingface.co/thenlper/gte-large) - Embedding model used as our default (works best in english)
- [HNSW.NET](https://github.com/curiosity-ai/hnsw-sharp) - Used for everything related to RAG / Vector Search
- [Newtonsoft Json](https://www.newtonsoft.com/json) - Practically all the classes can be imported and exported in Json
- [OpenAI .Net API Library](https://github.com/openai/openai-dotnet) - Used for OpenAI API backend compatibility
