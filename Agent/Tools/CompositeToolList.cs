using OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace LetheAISharp.Agent.Tools
{
    public class CompositeToolList(params IToolList[] toolLists) : IToolList
    {
        public string Id => string.Join("+", toolLists.Select(t => t.Id));
        public IReadOnlyList<Tool> GetToolList() => [.. toolLists.SelectMany(t => t.GetToolList())];

        public void LoadTools(bool clearExisting = false)
        {
            foreach (var toolList in toolLists)
            {
                toolList.LoadTools(clearExisting);
                clearExisting = false; // Only clear for the first tool list (otherwise it makes no sense to have multiple tool lists)
            }
        }

        public void UnloadTools()
        {
            foreach (var toolList in toolLists)
            {
                toolList.UnloadTools();
            }
        }

        public bool RequiresConfirmation(string functionName) => toolLists.Any(t => t.RequiresConfirmation(functionName));
    }

    public class ToolManager
    {
        private readonly Dictionary<string, IToolList> _toolLists = new();

        public void RegisterToolList(IToolList toolList)
        {
            if (toolList == null || string.IsNullOrWhiteSpace(toolList.Id))
                throw new ArgumentException("Tool list must have a valid ID.");
            _toolLists[toolList.Id] = toolList;
            toolList.LoadTools();
        }

        public bool UnregisterToolList(string id)
        {
            if (_toolLists.ContainsKey(id))
            {
                _toolLists[id].UnloadTools();
                _toolLists.Remove(id);
                return true;
            }
            else
            {
                return false;
            }
        }

        public IReadOnlyList<Tool> GetToolsForIds(params string[] ids)
        {
            var tools = new List<Tool>();
            foreach (var id in ids)
            {
                if (_toolLists.TryGetValue(id, out var toolList))
                {
                    tools.AddRange(toolList.GetToolList());
                }
                else
                {
                    throw new KeyNotFoundException($"No tool list found with ID: {id}");
                }
            }
            return tools;
        }

        public bool RequiresConfirmation(string functionName)
        {
            return _toolLists.Values.Any(tl => tl.RequiresConfirmation(functionName));
        }
    }
}
