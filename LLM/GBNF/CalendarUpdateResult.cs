using LetheAISharp.GBNF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LetheAISharp.LLM.GBNF
{
    public class CalendarUpdateResult : LLMExtractableBase<CalendarUpdateResult>
    {
        [Required]
        public string Sunday { get; set; } = string.Empty;
        [Required]
        public string Monday { get; set; } = string.Empty;
        [Required]
        public string Tuesday { get; set; } = string.Empty;
        [Required]
        public string Wednesday { get; set; } = string.Empty;
        [Required]
        public string Thursday { get; set; } = string.Empty;
        [Required]
        public string Friday { get; set; } = string.Empty;
        [Required]
        public string Saturday { get; set; } = string.Empty;

        public CalendarUpdateResult() : base() { }

        public CalendarUpdateResult(string[] days) : base()
        {
            if (days.Length != 7)
                throw new ArgumentException("Input array must have exactly 7 elements, one for each day of the week.");
            Sunday = days[0];
            Monday = days[1];
            Tuesday = days[2];
            Wednesday = days[3];
            Thursday = days[4];
            Friday = days[5];
            Saturday = days[6];
        }

        public string ScheduleToString()
        {
            var res = new StringBuilder();
            res.AppendLine($"Sunday: {Sunday}");
            res.AppendLine($"Monday: {Monday}");
            res.AppendLine($"Tuesday: {Tuesday}");
            res.AppendLine($"Wednesday: {Wednesday}");
            res.AppendLine($"Thursday: {Thursday}");
            res.AppendLine($"Friday: {Friday}");
            res.AppendLine($"Saturday: {Saturday}");
            return res.ToString();
        }

        public override string GetQuery()
        {
            var requestedTask = """
                Update the calendar above using the transcript provided in the prompt. Respond using a JSON format containing the following format:

                {
                    "Sunday": "Schedule for Sunday",
                    "Monday": "Schedule for Monday",
                    ...
                }

                If nothing is mentioned about a specific day, just put the existing schedule for that day without changing it. If a day is mentioned but no schedule is provided (or the schedule is explicitly stated as clear), just put "no schedule" for that day.
                """;
                
            requestedTask = LLMEngine.Bot.ReplaceMacros(requestedTask);
            return requestedTask;
        }
    }
}
