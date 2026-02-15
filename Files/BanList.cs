using System.Text;
using LetheAISharp.LLM;

namespace LetheAISharp.Files
{
    /// <summary>
    /// BanList is a file that contains a list of words that are not allowed during web search query generation. 
    /// This is used to prevent the AI from generating queries that contain inappropriate or harmful content. 
    /// The BanList can be customized by the user to include any words they wish to ban from query generation.
    /// </summary>
    public class BanList : BaseFile
    {
        public List<string> BannedWords { get; set; } = [];

        public bool IsAllowed(string query)
        {
            foreach (var word in BannedWords)
            {
                if (query.Contains(word, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }

}
