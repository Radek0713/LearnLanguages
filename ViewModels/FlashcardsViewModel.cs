using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class FlashcardsViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly List<FlashcardModel> _flashcardsQueue;
        private FlashcardModel _currentFlashcard;
        private int _totalCardsCount;
        private int _currentCardNumber;

        private string _topicInfoText;
        private string _progressText;
        private BitmapImage _frontImage;
        private BitmapImage _backImage;
        private bool _isFlipped;

        public string TopicInfoText
        {
            get => _topicInfoText;
            set => SetProperty(ref _topicInfoText, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public int TotalCardsCount
        {
            get => _totalCardsCount;
            set => SetProperty(ref _totalCardsCount, value);
        }

        public int CurrentCardNumber
        {
            get => _currentCardNumber;
            set => SetProperty(ref _currentCardNumber, value);
        }

        public BitmapImage FrontImage
        {
            get => _frontImage;
            set => SetProperty(ref _frontImage, value);
        }

        public BitmapImage BackImage
        {
            get => _backImage;
            set => SetProperty(ref _backImage, value);
        }

        public bool IsFlipped
        {
            get => _isFlipped;
            set => SetProperty(ref _isFlipped, value);
        }

        public ICommand FlipCommand { get; }
        public ICommand MarkBadCommand { get; }
        public ICommand MarkGoodCommand { get; }
        public ICommand BackCommand { get; }

        public FlashcardsViewModel(MainViewModel mainViewModel, int? topicId = null, bool onlyOverdue = false)
        {
            _mainViewModel = mainViewModel;
            _flashcardsQueue = _mainViewModel.DatabaseService.GetFlashcardsForToday(topicId, onlyOverdue);
            TotalCardsCount = _flashcardsQueue?.Count ?? 0;
            CurrentCardNumber = 0;

            FlipCommand = new RelayCommand(() => IsFlipped = true);
            MarkBadCommand = new RelayCommand(() => ProcessAnswer(false));
            MarkGoodCommand = new RelayCommand(() => ProcessAnswer(true));
            BackCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new FlashcardsTopicsViewModel(_mainViewModel)));

            LoadNextFlashcard();
        }

        private void ProcessAnswer(bool isCorrect)
        {
            if (_currentFlashcard == null) return;

            try
            {
                _mainViewModel.DatabaseService.UpdateFlashcardProgress(_currentFlashcard, isCorrect);
                LoadNextFlashcard();
            }
            catch (Exception ex)
            {
                _mainViewModel.DialogService.ShowError(
                    $"Błąd podczas zapisu postępu do bazy danych:\n{ex.Message}",
                    "Błąd bazy danych");
            }
        }

        private void LoadNextFlashcard()
        {
            if (_flashcardsQueue == null || _flashcardsQueue.Count == 0)
            {
                int remainingToday = _mainViewModel.DatabaseService.GetReviewCountForToday();

                if (remainingToday == 0)
                {
                    _mainViewModel.DialogService.ShowInfo(
                        "Świetna robota! Brak słówek na dziś.",
                        "Koniec nauki");
                    _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel));
                }
                else
                {
                    _mainViewModel.DialogService.ShowInfo(
                        $"Ukończono ten zestaw! Pozostało jeszcze {remainingToday} słówek na dziś w innych tematach.",
                        "Zestaw ukończony");
                    _mainViewModel.NavigateTo(new FlashcardsTopicsViewModel(_mainViewModel));
                }
                return;
            }

            CurrentCardNumber++;
            _currentFlashcard = _flashcardsQueue[0];
            _flashcardsQueue.RemoveAt(0);

            TopicInfoText = $"{_currentFlashcard.CategoryName} • {_currentFlashcard.TopicName}";
            ProgressText = $"Karta {CurrentCardNumber} z {TotalCardsCount}";

            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            string polPath = Path.Combine(basePath, _currentFlashcard.CategoryName, _currentFlashcard.TopicName, "POL", _currentFlashcard.PolImageName);
            string engPath = Path.Combine(basePath, _currentFlashcard.CategoryName, _currentFlashcard.TopicName, "ENG", _currentFlashcard.EngImageName);

            FrontImage = LoadImageSafe(polPath);
            BackImage = LoadImageSafe(engPath);
            IsFlipped = false;
        }

        private BitmapImage LoadImageSafe(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Nie znaleziono pliku: {filePath}");
                return null;
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }
}