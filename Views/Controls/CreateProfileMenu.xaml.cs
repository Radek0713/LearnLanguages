using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LearnLanguages.Services;
using MySql.Data.MySqlClient;

namespace LearnLanguages.Views.Controls
{
    public partial class CreateProfileMenu : UserControl
    {
        public CreateProfileMenu()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TxtProfileName.Focus();
        }

        private void TxtProfileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveProfile();
            }
        }

        private void BtnAddProfile_Click(object sender, RoutedEventArgs e)
        {
            SaveProfile();
        }

        private void SaveProfile()
        {
            string profileName = TxtProfileName.Text?.Trim();

            if (string.IsNullOrWhiteSpace(profileName))
            {
                MessageBox.Show("Wpisz nazwę profilu przed zapisaniem.",
                                "Brak nazwy", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProfileName.Focus();
                return;
            }

            try
            {
                DatabaseService.CreateProfile(profileName);
                NavigateToProfiles();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("Profil o takiej nazwie już istnieje. Wybierz inną nazwę.",
                                "Duplikat nazwy", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtProfileName.Focus();
                TxtProfileName.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas tworzenia profilu:\n{ex.Message}",
                                "Błąd bazy danych", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateToProfiles();
        }

        private void NavigateToProfiles()
        {
            var mainWindow = (Window.GetWindow(this) ?? Application.Current.MainWindow) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new Profiles();
            }
        }
    }
}