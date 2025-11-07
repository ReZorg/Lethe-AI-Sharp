using LetheAISharp.Files;
using LetheAISharp.LLM;
using System.Text;

namespace LetheAISharp.Memory
{
    public class PromptInsert
    {
        public MemoryUnit Memory;
        public Guid guid => Memory.Guid;
        public int Location => Memory is ChatSession ? LLMEngine.Settings.RAGIndex : Memory.PositionIndex;
        public bool Sticky => Memory.Sticky;
        public int Duration = 0;

        public PromptInsert(MemoryUnit content)
        {
            Memory = content;
            Duration = content.Duration;
        }

        public string ToContent()
        {
            Memory.Touch();
            if (Memory is ChatSession info)
            {
                return info.ToSnippet(TitleInsertType.Bold, LLMEngine.Bot.DatesInSessionSummaries, false, true);
            }
            else
            {
                var hastitle = Memory.Category == MemoryType.Person || Memory.Category == MemoryType.Location || Memory.Category == MemoryType.Event || Memory.Category == MemoryType.Journal;
                var compress = hastitle;
                var hasdate = Memory.Category == MemoryType.Journal || Memory.Category == MemoryType.Event;
                return Memory.ToSnippet(hastitle ? TitleInsertType.Bold : TitleInsertType.None, hasdate, false, false);
            }
        }
    }

    /// <summary>
    /// Represents an organized collection of MemoryUnit, mostly used for managing prompt inserts during LLM interactions.
    /// Can also be used independently to retrieve all relevant memories for a specific prompt using Bot.Brain.GetRAGandInserts().
    /// </summary>
    /// <remarks>While this is a public class, this is not meant to be used directly outside of very specific scenarios.</remarks>
    public class PromptInserts : List<PromptInsert> 
    { 
        public void DecreaseDuration()
        {
            foreach (var item in this)
            {
                item.Duration--;
            }
            RemoveAll(i => i.Duration <= 0);
        }

        public void AddInsert(MemoryUnit memory)
        {
            // Check if same guid exists, if so, replace the content
            var index = FindIndex(i => i.guid == memory.Guid);
            if (index >= 0)
            {
                this[index] = new PromptInsert(memory);
            }
            else
            {
                Add(new PromptInsert(memory));
            }
        }

        public List<PromptInsert> GetEntriesByPosition(int position)
        {
            return FindAll(i => i.Location == position);
        }

        public string GetContentByPosition(int position)
        {
            var res = new StringBuilder();
            foreach (var item in GetEntriesByPosition(position))
                res.AppendLinuxLine(item.ToContent());
            return LLMEngine.Bot.ReplaceMacros(res.ToString());
        }

        public void AddMemories(List<VaultResult> memories)
        {
            if (memories.Count == 0)
                return;
            foreach (var meminfo in memories)
            {
                AddInsert(meminfo.Memory);
            }
        }

        public HashSet<Guid> GetGuids()
        {
            // Retrieve all guids from the content
            var res = new HashSet<Guid>();
            foreach (var item in this)
                res.Add(item.guid);
            return res;
        }

        public bool Contains(Guid guid) => Find(i => i.guid == guid) != null;
        public bool Contains(MemoryUnit memory) => Find(e => e.Memory == memory) != null;
    }
}
