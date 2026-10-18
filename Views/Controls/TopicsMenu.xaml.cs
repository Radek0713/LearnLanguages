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
using LearnLanguages.Models;
using LearnLanguages.Services;
using LearnLanguages.Views;

namespace LearnLanguages.Views.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy TopicsMenu.xaml
    /// </summary>
    public partial class TopicsMenu : UserControl
    {
        public TopicsMenu()
        {
            InitializeComponent();
            LoadDepartments();
        }
        private void LoadDepartments()
        {
            // Pobieramy dane z MySQL i przypisujemy je do ListBoxa
            DepartmentsListBox.ItemsSource = DatabaseService.GetDepartments();
        }
        private void DepartmentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Wyciągamy kliknięty obiekt (rzutujemy go na klasę Department)
            if (DepartmentsListBox.SelectedItem is Department selectedDepartment)
            {
                // Pobieramy tematy z bazy na podstawie ID klikniętego działu
                // i wrzucamy je do kontrolki po prawej stronie
                TopicsItemsControl.ItemsSource = DatabaseService.GetTopicsByDepartment(selectedDepartment.Id);
            }
        }
        private void AddTopic_Click(object sender, RoutedEventArgs e)
        {
            // Pobieramy przycisk, który został kliknięty
            if (sender is Button clickedButton)
            {
                // Z XAML (atrybut Tag="{Binding}") wyciągamy cały obiekt Topic
                if (clickedButton.Tag is Topic selectedTopic)
                {
                    // Zapisujemy zmianę w bazie danych MySQL
                    DatabaseService.MarkTopicAsSelected(selectedTopic.Id);

                    // Odświeżamy listę tematów, żeby przycisk zmienił się na wyszarzony "Już dodane"
                    TopicsItemsControl.ItemsSource = DatabaseService.GetTopicsByDepartment(selectedTopic.DepartmentId);
                }
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
