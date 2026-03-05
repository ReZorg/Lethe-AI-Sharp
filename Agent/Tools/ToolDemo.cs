using LetheAISharp.Agent.Actions;
using LetheAISharp.GBNF;
using LetheAISharp.LLM;
using OpenAI;
using System;
using System.Collections.Generic;
using System.Text;

namespace LetheAISharp.Agent.Tools
{
    internal class ToolDemo : IToolList
    {
        public string Id => "demo_tools";
        private List<Tool> toolList = [];

        public IReadOnlyList<Tool> GetToolList() => toolList;

        public void LoadTools(bool clearExisting = false)
        {
            toolList.Clear();
            if (clearExisting) 
            {
                Tool.ClearRegisteredTools();
            }
            toolList.Add(Tool.GetOrCreateTool(this, nameof(GetWeather), "Gets the current weather for a given city and country."));
            toolList.Add(Tool.GetOrCreateTool(this, nameof(WebSearch), "Performs a web search for the given query and returns a summary of the results."));
        }

        public void UnloadTools()
        {
            foreach (var tool in toolList)
            {
                Tool.TryUnregisterTool(tool);
            }
            toolList.Clear();
        }

        public async Task<string> GetWeather(string country, string city)
        {
            await Task.Delay(5).ConfigureAwait(false);
            // This is a placeholder implementation. In a real implementation, you would call a weather API to get the actual weather data.
            return $"The current weather in {city}, {country} is sunny with a temperature of {LLMEngine.RNG.Next(10, 40)}°C.";
        }

        public async Task<string> WebSearch(string query)
        {
            await Task.Delay(5).ConfigureAwait(false);
            var searchaction = new WebSearchAction();
            var param = new TopicSearch()
            {
                Topic = query,
                Reason = "To find the latest news on the topic.",
                Urgency = 5,
                SearchQueries = [query]
            };
            var serchresults = await searchaction.Execute(param, CancellationToken.None).ConfigureAwait(false);
            var mergeaction = new MergeSearchResultsAction();
            var mergeparams = new MergeSearchParams("This is a search done regarding the currently active discussion.", query, "{{char}} is assisting the user with a web search", serchresults);

            var mergedResult = await mergeaction.Execute(mergeparams, CancellationToken.None).ConfigureAwait(false);

            // This is a placeholder implementation. In a real implementation, you would call a news API to get the actual news data.
            return $"Result of the web search query: {query} \n\n {mergedResult}";
        }

        public bool RequiresConfirmation(string functionName)
        {
            // StartWith is used here because many backends append random strings to the function name to avoid name collisions,
            // so we want to check if the functionName starts with the base name of the function. 
            // You should take this into account when naming functions so yours don't accidentally collide with each other for confirmation purposes.
            if (functionName.StartsWith(nameof(GetWeather)))
            {
                // This is for demonstration purpose, here getWeather doesn't need a confirmation.
                // but you can set it to true if you want to require confirmation before calling this tool.
                return false; 
            }
            else if (functionName.StartsWith(nameof(WebSearch)))
            {
                // Web search might be a more impactful action, so we require confirmation before allowing the agent to call it.
                return true;
            }
            // Tools not in the toolset should return false for RequiresConfirmation.
            // They either aren't called, or are handled by another toolset that may or may not require confirmation.
            return false;
        }

    }
}
