using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using LearnLanguages.Models;
using LearnLanguages.Services;

namespace LearnLanguages.Views.Controls
{
    public partial class Profiles : UserControl
    {
        public Profiles()
        {
            InitializeComponent();
            LoadProfiles();
        }

        private void LoadProfiles()
        {
            List<ProfileModel> profiles = DatabaseService.GetProfiles();
            ProfilesItemsControl.ItemsSource = profiles;

            TxtNoProfiles.Visibility = profiles.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProfileModel selectedProfile)
            {
                // Zapamiętujemy aktywny profil w sesji aplikacji
                DatabaseService.CurrentProfile = selectedProfile;

                var mainWindow = (Window.GetWindow(this) ?? Application.Current.MainWindow) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.MainContentArea.Content = new StartMenu();
                }
            }
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (Window.GetWindow(this) ?? Application.Current.MainWindow) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new CreateProfileMenu();
            }
        }
        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Czy na pewno chcesz zamknąć aplikację?",
                "Zamykanie aplikacji",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}