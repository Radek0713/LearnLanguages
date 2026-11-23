using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class QuizOptionItem : BaseViewModel
    {
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
        private string _text;
        private Brush _backgroundBrush = DefaultBrush;
        private bool _isEnabled = true;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            set => SetProperty(ref _backgroundBrush, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public void Reset()
        {
            BackgroundBrush = DefaultBrush;
            IsEnabled = true;
        }
    }

    public class TasksViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly Random _random = new();
        private readonly Queue<ExerciseTaskModel> _taskQueue = new();

        private List<string> _topicWords = new();
        private ExerciseTaskModel _currentTask;
        private int _solvedCount;
        private bool _isGrammarMode;

        // 0 = menu główne zadań, 1 = wybór słówek, 2 = wybór gramatyki, 3 = quiz
        private int _currentStep;
        private Topic _selectedTopic;
        private GrammarModel _selectedGrammarTopic;

        private bool _isLoading;
        private bool _isQuestionVisible;
        private bool _areOptionsVisible;
        private string _questionText;
        private string _feedbackText;
        private Brush _feedbackBrush = Brushes.Black;
        private string _nextButtonText = "Następne zadanie";
        private bool _isNextButtonVisible;

        public ObservableCollection<Topic> AvailableTopics { get; } = new();
        public ObservableCollection<GrammarModel> AvailableGrammarTopics { get; } = new();

        public QuizOptionItem Option1 { get; } = new();
        public QuizOptionItem Option2 { get; } = new();
        public QuizOptionItem Option3 { get; } = new();
        public QuizOptionItem Option4 { get; } = new();

        public int CurrentStep
        {
            get => _currentStep;
            set => SetProperty(ref _currentStep, value);
        }

        public Topic SelectedTopic
        {
            get => _selectedTopic;
            set => SetProperty(ref _selectedTopic, value);
        }

        public GrammarModel SelectedGrammarTopic
        {
            get => _selectedGrammarTopic;
            set => SetProperty(ref _selectedGrammarTopic, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsQuestionVisible
        {
            get => _isQuestionVisible;
            set => SetProperty(ref _isQuestionVisible, value);
        }

        public bool AreOptionsVisible
        {
            get => _areOptionsVisible;
            set => SetProperty(ref _areOptionsVisible, value);
        }

        public string QuestionText
        {
            get => _questionText;
            set => SetProperty(ref _questionText, value);
        }

        public string FeedbackText
        {
            get => _feedbackText;
            set => SetProperty(ref _feedbackText, value);
        }

        public Brush FeedbackBrush
        {
            get => _feedbackBrush;
            set => SetProperty(ref _feedbackBrush, value);
        }

        public string NextButtonText
        {
            get => _nextButtonText;
            set => SetProperty(ref _nextButtonText, value);
        }

        public bool IsNextButtonVisible
        {
            get => _isNextButtonVisible;
            set => SetProperty(ref _isNextButtonVisible, value);
        }

        public ICommand OpenVocabMenuCommand { get; }
        public ICommand OpenGrammarMenuCommand { get; }
        public ICommand BackToLanguageMenuCommand { get; }
        public ICommand BackToMenuCommand { get; }
        public ICommand GenerateVocabTasksCommand { get; }
        public ICommand GenerateGrammarTasksCommand { get; }
        public ICommand SelectAnswerCommand { get; }
        public ICommand NextCommand { get; }

        public TasksViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            OpenVocabMenuCommand = new RelayCommand(OpenVocabMenu);
            OpenGrammarMenuCommand = new RelayCommand(OpenGrammarMenu);
            BackToLanguageMenuCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel)));
            BackToMenuCommand = new RelayCommand(BackToMenu);

            GenerateVocabTasksCommand = new AsyncRelayCommand(GenerateVocabTasksAsync);
            GenerateGrammarTasksCommand = new AsyncRelayCommand(GenerateGrammarTasksAsync);
            SelectAnswerCommand = new RelayCommand(SelectAnswer);
            NextCommand = new AsyncRelayCommand(NextAsync);
        }

        private void BackToMenu()
        {
            CurrentStep = 0;
            _taskQueue.Clear();
            _currentTask = null;
        }

        private void OpenVocabMenu()
        {
            CurrentStep = 1;
            AvailableTopics.Clear();

            var departments = _mainViewModel.DatabaseService.GetDepartments();
            foreach (var dept in departments)
            {
                foreach (var topic in _mainViewModel.DatabaseService.GetTopicsByDepartment(dept.Id))
                {
                    AvailableTopics.Add(topic);
                }
            }

            if (AvailableTopics.Count > 0)
            {
                SelectedTopic = AvailableTopics[0];
            }
        }

        private void OpenGrammarMenu()
        {
            CurrentStep = 2;
            AvailableGrammarTopics.Clear();

            foreach (var item in _mainViewModel.DatabaseService.GetGrammarTopics())
            {
                AvailableGrammarTopics.Add(item);
            }

            if (AvailableGrammarTopics.Count > 0)
            {
                SelectedGrammarTopic = AvailableGrammarTopics[0];
            }
        }

        private async Task GenerateVocabTasksAsync()
        {
            if (SelectedTopic == null)
            {
                _mainViewModel.DialogService.ShowInfo("Wybierz temat ze słówkami!");
                return;
            }

            _topicWords = _mainViewModel.DatabaseService.GetVocabularyWordsByTopic(SelectedTopic.Id);
            if (_topicWords == null || _topicWords.Count == 0)
            {
                _mainViewModel.DialogService.ShowWarning(
                    "Wybrany temat nie zawiera żadnych słówek w bazie!",
                    "Informacja");
                return;
            }

            _isGrammarMode = false;
            CurrentStep = 3;
            _solvedCount = 0;
            _taskQueue.Clear();

            await LoadMoreTasksAsync();
        }

        private async Task GenerateGrammarTasksAsync()
        {
            if (SelectedGrammarTopic == null)
            {
                _mainViewModel.DialogService.ShowInfo("Wybierz zagadnienie gramatyczne!");
                return;
            }

            _isGrammarMode = true;
            CurrentStep = 3;
            _solvedCount = 0;
            _taskQueue.Clear();

            await LoadMoreTasksAsync();
        }

        private async Task LoadMoreTasksAsync()
        {
            IsLoading = true;
            IsNextButtonVisible = false;
            FeedbackText = string.Empty;
            IsQuestionVisible = false;
            AreOptionsVisible = false;

            try
            {
                List<ExerciseTaskModel> resultList = !_isGrammarMode
                    ? await _mainViewModel.LLMService.GenerateBatchTasksForWordsAsync(_topicWords, 5)
                    : await _mainViewModel.LLMService.GenerateGrammarTasksAsync(SelectedGrammarTopic.Id, 5);

                IsLoading = false;

                if (resultList != null && resultList.Count > 0)
                {
                    foreach (var task in resultList)
                    {
                        _taskQueue.Enqueue(task);
                    }
                    ShowNextTaskFromQueue();
                }
                else
                {
                    ShowApiErrorState("Nie udało się wygenerować zadań. Serwer zwrócił pustą odpowiedź.");
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;

                string errorMessage = "Chwilowo serwer AI jest mocno obciążony lub wystąpił problem z połączeniem.";
                if (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    errorMessage = "Chwilowo AI jest przeciążone (przekroczono limit zapytań w darmowej wersji Groq).";
                }

                ShowApiErrorState(errorMessage);
            }
        }

        private void ShowApiErrorState(string message)
        {
            AreOptionsVisible = false;
            IsQuestionVisible = true;
            QuestionText = $"⚠️ {message}\n\nCo chcesz zrobić?";
            NextButtonText = "🔄 Generuj ponownie";
            IsNextButtonVisible = true;
        }

        private List<string> ShuffleOptions(List<string> options)
        {
            var shuffled = new List<string>(options);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        private void ShowNextTaskFromQueue()
        {
            NextButtonText = "Następne zadanie";

            if (_taskQueue.Count > 0)
            {
                _currentTask = _taskQueue.Dequeue();
                _solvedCount++;

                QuestionText = $"Zadanie #{_solvedCount}:\n{_currentTask.TrescZdania}";

                if (_currentTask.Opcje != null && _currentTask.Opcje.Count == 4)
                {
                    var shuffled = ShuffleOptions(_currentTask.Opcje);
                    Option1.Text = shuffled[0];
                    Option2.Text = shuffled[1];
                    Option3.Text = shuffled[2];
                    Option4.Text = shuffled[3];
                }

                Option1.Reset();
                Option2.Reset();
                Option3.Reset();
                Option4.Reset();

                IsQuestionVisible = true;
                AreOptionsVisible = true;
                FeedbackText = string.Empty;
                IsNextButtonVisible = false;
            }
            else
            {
                _ = LoadMoreTasksAsync();
            }
        }

        private void SelectAnswer(object parameter)
        {
            if (parameter is not QuizOptionItem clickedOption || _currentTask == null) return;

            Option1.IsEnabled = false;
            Option2.IsEnabled = false;
            Option3.IsEnabled = false;
            Option4.IsEnabled = false;

            var greenBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            var redBrush = new SolidColorBrush(Color.FromRgb(239, 68, 68));

            if (clickedOption.Text == _currentTask.PoprawnaOdpowiedz)
            {
                clickedOption.BackgroundBrush = greenBrush;
                FeedbackBrush = greenBrush;
                FeedbackText = $"✅ Poprawna odpowiedź!\nWyjaśnienie: {_currentTask.Wyjasnienie}";
            }
            else
            {
                clickedOption.BackgroundBrush = redBrush;
                FeedbackBrush = redBrush;
                FeedbackText = $"❌ Błędna odpowiedź. Poprawna: {_currentTask.PoprawnaOdpowiedz}\nWyjaśnienie: {_currentTask.Wyjasnienie}";

                foreach (var opt in new[] { Option1, Option2, Option3, Option4 })
                {
                    if (opt.Text == _currentTask.PoprawnaOdpowiedz)
                    {
                        opt.BackgroundBrush = greenBrush;
                    }
                }
            }

            IsNextButtonVisible = true;
        }

        private async Task NextAsync()
        {
            if (NextButtonText.Contains("Generuj ponownie") || _taskQueue.Count == 0)
            {
                await LoadMoreTasksAsync();
            }
            else
            {
                ShowNextTaskFromQueue();
            }
        }
    }
}