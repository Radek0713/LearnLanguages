using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
            LoadSelectedTopics();
            LoadDepartments();
        }
        private void LoadSelectedTopics()
        {
            List<SelectedTopicItem> selected = DatabaseService.GetSelectedTopics();

            TxtSelectedHeader.Text = $"Aktualnie wybrane tematy ({selected.Count}):";
            SelectedTopicsItemsControl.ItemsSource = selected;

            TxtNoSelectedTopics.Visibility = selected.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        private void LoadDepartments()
        {
            var departments = DatabaseService.GetDepartments();
            DepartmentsListBox.ItemsSource = departments;

            // Automatyczne zaznaczenie pierwszego działu, aby prawa kolumna nie była pusta na starcie
            if (departments != null && departments.Count > 0)
            {
                DepartmentsListBox.SelectedIndex = 0;
            }
        }
        private void DepartmentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentsListBox.SelectedItem is Department selectedDepartment)
            {
                TopicsItemsControl.ItemsSource = DatabaseService.GetTopicsByDepartment(selectedDepartment.Id);
            }
        }
        private void AddTopic_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Topic topic)
            {
                if (!topic.IsSelected)
                {
                    DatabaseService.MarkTopicAsSelected(topic.Id);
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"Czy na pewno chcesz usunąć temat „{topic.Name}” z aktywnej nauki?\nDotychczasowe postępy powtórek zostaną zachowane na wypadek powrotu.",
                        "Potwierdzenie usunięcia",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    DatabaseService.UnmarkTopicAsSelected(topic.Id);
                }

                // Odświeżenie górnego paska wybranych tematów oraz listy tematów w dziale
                LoadSelectedTopics();
                if (DepartmentsListBox.SelectedItem is Department selectedDept)
                {
                    TopicsItemsControl.ItemsSource = DatabaseService.GetTopicsByDepartment(selectedDept.Id);
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
