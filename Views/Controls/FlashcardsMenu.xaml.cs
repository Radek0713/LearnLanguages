using LearnLanguages.Views;
using System.Windows;
using System.Windows.Controls;

namespace LearnLanguages.Views.Controls
{
    public partial class FlashcardsMenu : UserControl
    {
        public FlashcardsMenu()
        {
            InitializeComponent();
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
            // Reset interfejsu dla nowej karty
            FlashcardFront.Visibility = Visibility.Visible;
            FlashcardBack.Visibility = Visibility.Collapsed;

            BtnFlip.Visibility = Visibility.Visible;
            EvaluationButtons.Visibility = Visibility.Collapsed;

            // TODO: Pobranie kolejnego słówka z bazy i podmiana obrazków w ImgFront / ImgBack
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