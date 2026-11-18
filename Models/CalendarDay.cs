using System;

namespace LearnLanguages
{
    public class CalendarDay
    {
        public DateTime Date { get; set; }

        // Wyświetla sam numer dnia, np. "15"
        public string DayNumber => Date.Day.ToString();

        // Liczba zaplanowanych powtórek na ten dzień
        public int WordCount { get; set; }

        // Liczba zaległych powtórek z przeszłości (przypisywana tylko do dnia dzisiejszego)
        public int OverdueCount { get; set; }

        // Żeby wyszarzyć dni z poprzedniego/następnego miesiąca
        public bool IsCurrentMonth { get; set; }

        // Czy to dzisiejsza data (do wyróżnienia obwódką)
        public bool IsToday => Date.Date == DateTime.Today;

        // Widoczność niebieskiego i pomarańczowego paska
        public bool HasWords => WordCount > 0;
        public bool HasOverdueWords => OverdueCount > 0;
    }
}