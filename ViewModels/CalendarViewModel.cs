using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class CalendarViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private DateTime _currentDate;
        private string _monthYearText;
        private bool _showLowReviewsBanner;
        private string _lowReviewsMessage;

        public ObservableCollection<CalendarDay> Days { get; } = new();

        public string MonthYearText
        {
            get => _monthYearText;
            set => SetProperty(ref _monthYearText, value);
        }

        public bool ShowLowReviewsBanner
        {
            get => _showLowReviewsBanner;
            set => SetProperty(ref _showLowReviewsBanner, value);
        }

        public string LowReviewsMessage
        {
            get => _lowReviewsMessage;
            set => SetProperty(ref _lowReviewsMessage, value);
        }

        public ICommand PrevMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand GoToTopicsCommand { get; }
        public ICommand BackCommand { get; }

        public CalendarViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _currentDate = DateTime.Today;

            PrevMonthCommand = new RelayCommand(PrevMonth, CanGoPrevMonth);
            NextMonthCommand = new RelayCommand(NextMonth);
            GoToTopicsCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new TopicsViewModel(_mainViewModel)));
            BackCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel)));

            LoadCalendar();
            UpdateLowReviewsBanner();
        }

        private bool CanGoPrevMonth()
        {
            DateTime today = DateTime.Today;
            return !(_currentDate.Year < today.Year ||
                     (_currentDate.Year == today.Year && _currentDate.Month <= today.Month));
        }

        private void PrevMonth()
        {
            if (!CanGoPrevMonth()) return;
            _currentDate = _currentDate.AddMonths(-1);
            LoadCalendar();
        }

        private void NextMonth()
        {
            _currentDate = _currentDate.AddMonths(1);
            LoadCalendar();
        }

        private void LoadCalendar()
        {
            DateTime today = DateTime.Today;

            string rawMonthYear = _currentDate.ToString("MMMM yyyy");
            MonthYearText = char.ToUpper(rawMonthYear[0]) + rawMonthYear.Substring(1);

            DateTime firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int offset = (int)firstDayOfMonth.DayOfWeek;
            if (offset == 0) offset = 7;

            DateTime startDate = firstDayOfMonth.AddDays(-(offset - 1));

            Dictionary<DateTime, int> wordCountsFromDb =
                _mainViewModel.DatabaseService.GetReviewCountsForMonth(_currentDate.Year, _currentDate.Month);
            int overdueCount = _mainViewModel.DatabaseService.GetOverdueReviewCount();

            Days.Clear();
            for (int i = 0; i < 42; i++)
            {
                DateTime dayDate = startDate.AddDays(i);
                int wordsToReview = wordCountsFromDb.ContainsKey(dayDate) ? wordCountsFromDb[dayDate] : 0;

                Days.Add(new CalendarDay
                {
                    Date = dayDate,
                    IsCurrentMonth = dayDate.Month == _currentDate.Month,
                    WordCount = wordsToReview,
                    OverdueCount = (dayDate.Date == today) ? overdueCount : 0
                });
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateLowReviewsBanner()
        {
            int upcomingCount = _mainViewModel.DatabaseService.GetUpcomingReviewsCount(3);
            bool hasUnselectedTopics = _mainViewModel.DatabaseService.HasUnselectedTopics();

            if (upcomingCount < 10 && hasUnselectedTopics)
            {
                LowReviewsMessage = $"Masz {upcomingCount} fiszek do powtórki w najbliższych 3 dniach. Dodaj nowy zestaw.";
                ShowLowReviewsBanner = true;
            }
            else
            {
                ShowLowReviewsBanner = false;
            }
        }
    }
}