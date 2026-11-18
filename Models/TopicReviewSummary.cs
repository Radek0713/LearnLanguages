using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearnLanguages.Models
{
    public class TopicReviewSummary
    {
        public int TopicId { get; set; }
        public string TopicName { get; set; }
        public string CategoryName { get; set; }

        public int TodayCount { get; set; }
        public int OverdueCount { get; set; }
        public int TotalCount => TodayCount + OverdueCount;

        public bool HasToday => TodayCount > 0;
        public bool HasOverdue => OverdueCount > 0;
    }
}
