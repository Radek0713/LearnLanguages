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
using LearnLanguages.Views;

namespace LearnLanguages.Views.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy StartMenu.xaml
    /// </summary>
    public partial class StartMenu : UserControl
    {
        public StartMenu()
        {
            InitializeComponent();
        }
        private void Btn_english_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new LanguageMenu();
            }
        }
        /*
        private void Btn_german_Click(object sender, RoutedEventArgs e) { }
        private void Btn_french_Click(object sender, RoutedEventArgs e) { }
        private void Btn_spanish_Click(object sender, RoutedEventArgs e) { }
        private void Btn_italian_Click(object sender, RoutedEventArgs e) { }
        */

        private void Btn_unsupportedLanguage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            string languageName = (button?.Content?.ToString() ?? "Ten język").ToLower();

            MessageBox.Show(
                $"Język {languageName} nie jest obecnie obsługiwany w tej wersji aplikacji.",
                "Brak obsługi języka",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
