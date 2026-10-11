namespace KnowledgeAssistant.Application.DTOs.Dashboard
{
    public enum FeedKind
    {
        Neutral,
        Info,
        Success,
        Warning
    }

    public class MonthlyCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }

        public static List<MonthlyCount> LastMonths(params int[] counts)
        {
            var now = DateTime.UtcNow;
            var list = new List<MonthlyCount>();

            for (var i = 0; i < counts.Length; i++)
            {
                var month = now.AddMonths(-(counts.Length - 1 - i));
                var isCurrent = i == counts.Length - 1;
                list.Add(new MonthlyCount
                {
                    Label = month.ToString("MMM") + (isCurrent ? " (so far)" : ""),
                    Count = counts[i]
                });
            }

            return list;
        }
    }

    public static class DashboardTime
    {
        public static string ToRelative(this DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} min ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} h ago";
            if (span.TotalDays < 2) return "Yesterday";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
            return utc.ToLocalTime().ToString("dd/MM/yyyy");
        }
    }
}
