using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class FlashcardsTopicsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<TopicReviewSummary> Topics { get; } = new();

        public string ReviewAllText { get; }
        public string ReviewOverdueText { get; }
        public bool CanReviewAll { get; }
        public bool CanReviewOverdue { get; }

        public ICommand ReviewAllCommand { get; }
        public ICommand ReviewOverdueCommand { get; }
        public ICommand SelectTopicCommand { get; }
        public ICommand BackCommand { get; }

        public FlashcardsTopicsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            var topics = _mainViewModel.DatabaseService.GetTopicReviewSummariesForToday();
            foreach (var topic in topics)
            {
                Topics.Add(topic);
            }

            int totalToday = topics.Sum(t => t.TodayCount);
            int totalOverdue = topics.Sum(t => t.OverdueCount);
            int totalAll = totalToday + totalOverdue;

            ReviewAllText = $"Powtórz wszystkie ({totalAll})";
            ReviewOverdueText = $"Tylko zaległe ({totalOverdue})";
            CanReviewAll = totalAll > 0;
            CanReviewOverdue = totalOverdue > 0;

            ReviewAllCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new FlashcardsViewModel(_mainViewModel, null, false)));

            ReviewOverdueCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new FlashcardsViewModel(_mainViewModel, null, true)));

            SelectTopicCommand = new RelayCommand(param =>
            {
                if (param is TopicReviewSummary summary)
                {
                    _mainViewModel.NavigateTo(new FlashcardsViewModel(_mainViewModel, summary.TopicId, false));
                }
            });

            BackCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel)));
        }
    }
}