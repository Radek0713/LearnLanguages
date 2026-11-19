using LearnLanguages.Services;
using LearnLanguages.Views;
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

            if (DatabaseService.CurrentProfile != null)
            {
                TxtProfileName.Text = $"👤 {DatabaseService.CurrentProfile.Name}";
            }
        }
        private void Btn_english_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo(new LanguageMenu());
        }

        private void Btn_change_profile_Click(object sender, RoutedEventArgs e)
        {
            DatabaseService.CurrentProfile = null;
            NavigateTo(new Profiles());
        }
        private void Btn_delete_profile_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseService.CurrentProfile == null) return;

            string profileName = DatabaseService.CurrentProfile.Name;

            MessageBoxResult result = MessageBox.Show(
                $"Czy na pewno chcesz trwale usunąć profil „{profileName}” wraz z całym postępem nauki?\nTej operacji nie można cofnąć.",
                "Ostrzeżenie – usuwanie profilu",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.OK)
            {
                try
                {
                    DatabaseService.DeleteProfile(DatabaseService.CurrentProfile.Id);
                    DatabaseService.CurrentProfile = null;
                    NavigateTo(new Profiles());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas usuwania profilu:\n{ex.Message}",
                                    "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NavigateTo(UserControl nextView)
        {
            var mainWindow = (Window.GetWindow(this) ?? Application.Current.MainWindow) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = nextView;
            }
        }
    }
}
