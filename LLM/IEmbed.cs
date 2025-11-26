using Newtonsoft.Json;

namespace LetheAISharp.LLM
{
    public interface IEmbed
    {
        Guid Guid { get; set; }
        float[] EmbedSummary { get; set; }

        Task EmbedText();
    }
}