using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearnLanguages.Models
{
    public class Topic
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        public int WordsCount { get; set; }
        public string DisplayName => $"{Name} ({WordsCount})";
    }
}
