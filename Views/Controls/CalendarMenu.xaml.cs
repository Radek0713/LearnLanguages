using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LearnLanguages.Models;
using LearnLanguages.Services;
using LearnLanguages.Views;
using LearnLanguages.Views.Controls; // Ścieżka do  DatabaseService

namespace LearnLanguages.Views.Controls
{
    public partial class CalendarMenu : UserControl
    {
        private DateTime _currentDate;

        public CalendarMenu()
        {
            InitializeComponent();
            _currentDate = DateTime.Today; // Zaczynamy od obecnego miesiąca
            LoadCalendar();
        }

        private void LoadCalendar()
        {
            DateTime today = DateTime.Today;

            // Blokujemy przycisk "< Poprzedni", jeśli oglądamy bieżący (lub wcześniejszy) miesiąc
            bool isCurrentOrPastMonth = (_currentDate.Year < today.Year) ||
                                        (_currentDate.Year == today.Year && _currentDate.Month <= today.Month);
            BtnPrevMonth.IsEnabled = !isCurrentOrPastMonth;

            // Zaktualizuj napis na górze, np. "Listopad 2026"
            string rawMonthYear = _currentDate.ToString("MMMM yyyy");
            TxtMonthYear.Text = char.ToUpper(rawMonthYear[0]) + rawMonthYear.Substring(1);

            // Określamy pierwszy dzień obecnego miesiąca
            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);

            // Sprawdzamy, w jaki dzień tygodnia wypada 1. dzień miesiąca
            int offset = (int)firstDayOfMonth.DayOfWeek;
            if (offset == 0) offset = 7;

            // Cofamy się do ostatniego poniedziałku z poprzedniego miesiąca
            DateTime startDate = firstDayOfMonth.AddDays(-(offset - 1));

            // Pobieramy liczby zaplanowanych słówek na obecny miesiąc
            Dictionary<DateTime, int> wordCountsFromDb = DatabaseService.GetReviewCountsForMonth(_currentDate.Year, _currentDate.Month);

            // Pobieramy liczbę zaległych słówek (wyświetlimy je tylko na dzisiejszej dacie)
            int overdueCount = DatabaseService.GetOverdueReviewCount();

            var days = new List<CalendarDay>();

            for (int i = 0; i < 42; i++)
            {
                DateTime dayDate = startDate.AddDays(i);
                int wordsToReview = 0;

                if (wordCountsFromDb.ContainsKey(dayDate))
                {
                    wordsToReview = wordCountsFromDb[dayDate];
                }

                days.Add(new CalendarDay
                {
                    Date = dayDate,
                    IsCurrentMonth = dayDate.Month == _currentDate.Month,
                    WordCount = wordsToReview,
                    OverdueCount = (dayDate.Date == today) ? overdueCount : 0
                });
            }

            CalendarItemsControl.ItemsSource = days;
        }

        private void Btn_PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            if (_currentDate.Year == today.Year && _currentDate.Month <= today.Month)
            {
                return; // Dodatkowe zabezpieczenie przed cofnięciem do przeszłych miesięcy
            }

            _currentDate = _currentDate.AddMonths(-1);
            LoadCalendar();
        }

        private void Btn_NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _currentDate.AddMonths(1);
            LoadCalendar();
        }

        private void Btn_Back_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new LanguageMenu();
            }
        }
    }
}