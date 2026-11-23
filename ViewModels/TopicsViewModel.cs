using System.Collections.ObjectModel;
using System.Windows.Input;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class TopicsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private Department _selectedDepartment;

        public ObservableCollection<SelectedTopicItem> SelectedTopics { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();
        public ObservableCollection<Topic> Topics { get; } = new();

        public string SelectedHeader => $"Aktualnie wybrane tematy ({SelectedTopics.Count}):";
        public bool HasNoSelectedTopics => SelectedTopics.Count == 0;

        public Department SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                {
                    LoadTopicsForSelectedDepartment();
                }
            }
        }

        public ICommand ToggleTopicCommand { get; }
        public ICommand BackCommand { get; }

        public TopicsViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            ToggleTopicCommand = new RelayCommand(ToggleTopic);
            BackCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel)));

            LoadSelectedTopics();
            LoadDepartments();
        }

        private void LoadSelectedTopics()
        {
            SelectedTopics.Clear();
            foreach (var item in _mainViewModel.DatabaseService.GetSelectedTopics())
            {
                SelectedTopics.Add(item);
            }

            OnPropertyChanged(nameof(SelectedHeader));
            OnPropertyChanged(nameof(HasNoSelectedTopics));
        }

        private void LoadDepartments()
        {
            Departments.Clear();
            foreach (var dept in _mainViewModel.DatabaseService.GetDepartments())
            {
                Departments.Add(dept);
            }

            if (Departments.Count > 0)
            {
                SelectedDepartment = Departments[0];
            }
        }

        private void LoadTopicsForSelectedDepartment()
        {
            Topics.Clear();
            if (SelectedDepartment == null) return;

            foreach (var topic in _mainViewModel.DatabaseService.GetTopicsByDepartment(SelectedDepartment.Id))
            {
                Topics.Add(topic);
            }
        }

        private void ToggleTopic(object parameter)
        {
            if (parameter is not Topic topic) return;

            if (!topic.IsSelected)
            {
                _mainViewModel.DatabaseService.MarkTopicAsSelected(topic.Id);
            }
            else
            {
                bool confirmed = _mainViewModel.DialogService.ShowConfirmation(
                    $"Czy na pewno chcesz usunąć temat „{topic.Name}” z aktywnej nauki?\nDotychczasowe postępy powtórek zostaną zachowane na wypadek powrotu.",
                    "Potwierdzenie usunięcia");

                if (!confirmed) return;

                _mainViewModel.DatabaseService.UnmarkTopicAsSelected(topic.Id);
            }

            LoadSelectedTopics();
            LoadTopicsForSelectedDepartment();
        }
    }
}