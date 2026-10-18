using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LearnLanguages.Models
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }

        // Wyświetla sam numer dnia, np. "15"
        public string DayNumber => Date.Day.ToString();

        // Liczba zaplanowanych powtórek
        public int WordCount { get; set; }

        // Żeby wyszarzyć dni z poprzedniego/następnego miesiąca (np. końcówka maja widoczna w czerwcu)
        public bool IsCurrentMonth { get; set; }

        // Czy w danym dniu są jakieś słówka do powtórki? (Użyjemy tego do ukrycia "0 słówek" w XAML)
        public bool HasWords => WordCount > 0;
    }
}
