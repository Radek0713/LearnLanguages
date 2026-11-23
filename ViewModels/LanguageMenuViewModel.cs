using System.Windows.Input;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class LanguageMenuViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public string ProfileDisplayName =>
            _mainViewModel.CurrentProfile != null
                ? $"👤 {_mainViewModel.CurrentProfile.Name}"
                : "👤 Profil";

        public ICommand OpenCalendarCommand { get; }
        public ICommand OpenTopicsCommand { get; }
        public ICommand OpenFlashcardsCommand { get; }
        public ICommand OpenTasksCommand { get; }
        public ICommand BackCommand { get; }

        public LanguageMenuViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            OpenCalendarCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new CalendarViewModel(_mainViewModel)));

            OpenTopicsCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new TopicsViewModel(_mainViewModel)));

            OpenFlashcardsCommand = new RelayCommand(OpenFlashcards);

            OpenTasksCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new TasksViewModel(_mainViewModel)));

            BackCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new StartMenuViewModel(_mainViewModel)));
        }

        private void OpenFlashcards()
        {
            int wordsToReviewToday = _mainViewModel.DatabaseService.GetReviewCountForToday();

            if (wordsToReviewToday == 0)
            {
                _mainViewModel.DialogService.ShowInfo(
                    "Nie masz na dzisiaj żadnych zaplanowanych fiszek do powtórzenia.\nWybierz nowy temat z zakładki 'Tematy'!",
                    "Brak słówek na dziś");
            }
            else
            {
                _mainViewModel.NavigateTo(new FlashcardsTopicsViewModel(_mainViewModel));
            }
        }
    }
}