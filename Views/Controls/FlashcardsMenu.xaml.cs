using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using LearnLanguages.Models;
using LearnLanguages.Services;
using LearnLanguages.Views;

namespace LearnLanguages.Views.Controls
{
    public partial class FlashcardsMenu : UserControl
    {
        private List<FlashcardModel> _flashcardsQueue;
        private FlashcardModel _currentFlashcard;
        public FlashcardsMenu()
        {
            InitializeComponent();
            _flashcardsQueue = DatabaseService.GetFlashcardsForToday(); // Pobieramy wszystkie fiszki na dzisiaj z bazy
            LoadNextFlashcard(); // Przygotowujemy pierwszą fiszkę na start
        }

        private void BtnFlip_Click(object sender, RoutedEventArgs e)
        {
            FlashcardFront.Visibility = Visibility.Collapsed;
            FlashcardBack.Visibility = Visibility.Visible;

            BtnFlip.Visibility = Visibility.Collapsed;
            EvaluationButtons.Visibility = Visibility.Visible;
        }

        private void BtnBad_Click(object sender, RoutedEventArgs e)
        {
            ProcessAnswer(isCorrect: false);
        }

        private void BtnGood_Click(object sender, RoutedEventArgs e)
        {
            ProcessAnswer(isCorrect: true);
        }

        private void ProcessAnswer(bool isCorrect)
        {
            if (_currentFlashcard == null) return;

            try
            {
                DatabaseService.UpdateFlashcardProgress(_currentFlashcard, isCorrect);
                LoadNextFlashcard();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisu postępu do bazy danych:\n{ex.Message}",
                                "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNextFlashcard()
        {
            // 1. Sprawdzamy, czy są jeszcze jakieś fiszki w kolejce
            if (_flashcardsQueue == null || _flashcardsQueue.Count == 0)
            {
                MessageBox.Show("Świetna robota! Brak słówek na dziś.", "Koniec", MessageBoxButton.OK, MessageBoxImage.Information);
                ReturnToMenu();
                return;
            }

            // 2. Wyciągamy pierwszą z brzegu fiszkę i usuwamy z kolejki (żeby się nie powtarzała)
            _currentFlashcard = _flashcardsQueue[0];
            _flashcardsQueue.RemoveAt(0);
            TxtTopicInfo.Text = $"{_currentFlashcard.CategoryName}  •  {_currentFlashcard.TopicName}";

            // 3. Budujemy absolutne ścieżki do obrazków na dysku
            // Przyjmujemy, że aplikacja startuje np. z folderu bin/Debug, 
            // więc musimy wskazać folder bazowy Data, który znajduje się wyżej.
            // Dopasuj basePath w razie potrzeby, np. używając AppDomain.CurrentDomain.BaseDirectory
            string basePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data"));

            // Ścieżka POL (Awers)
            string polPath = Path.Combine(basePath, _currentFlashcard.CategoryName, _currentFlashcard.TopicName, "POL", _currentFlashcard.PolImageName);

            // Ścieżka ENG (Rewers)
            string engPath = Path.Combine(basePath, _currentFlashcard.CategoryName, _currentFlashcard.TopicName, "ENG", _currentFlashcard.EngImageName);

            // 4. Przypisujemy obrazy do kontrolek
            LoadImageSafe(ImgFront, polPath);
            LoadImageSafe(ImgBack, engPath);

            // 5. Reset interfejsu dla nowej karty
            FlashcardFront.Visibility = Visibility.Visible;
            FlashcardBack.Visibility = Visibility.Collapsed;

            BtnFlip.Visibility = Visibility.Visible;
            EvaluationButtons.Visibility = Visibility.Collapsed;
        }

        private void LoadImageSafe(Image imageControl, string filePath)
        {
            if (File.Exists(filePath))
            {
                // Ładujemy obraz zwalniając uchwyt (plik nie będzie zablokowany przez system)
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Zwalnia plik po załadowaniu
                bitmap.EndInit();

                imageControl.Source = bitmap;
            }
            else
            {
                // Opcjonalnie: Załaduj obrazek typu "Brak obrazu" lub zostaw puste
                imageControl.Source = null;
                Console.WriteLine($"Nie znaleziono pliku: {filePath}");
            }
        }

        private void ReturnToMenu()
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new LanguageMenu();
            }
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