using LearnLanguages.Models;
using LearnLanguages.Services;
using LearnLanguages.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

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
            // Przełączamy widok karty na rewers
            FlashcardFront.Visibility = Visibility.Collapsed;
            FlashcardBack.Visibility = Visibility.Visible;

            // Chowamy przycisk obracania, pokazujemy Dobrze/Źle
            BtnFlip.Visibility = Visibility.Collapsed;
            EvaluationButtons.Visibility = Visibility.Visible;
        }

        private void BtnBad_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Zapisz ocenę "Źle" w bazie danych (SM-2)
            LoadNextFlashcard();
        }

        private void BtnGood_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Zapisz ocenę "Dobrze" w bazie danych (SM-2)
            LoadNextFlashcard();
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

            // 5. Reset interfejsu
            // Reset interfejsu dla nowej karty
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