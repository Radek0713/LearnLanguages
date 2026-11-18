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
            if (sender is Button clickedButton && clickedButton.Tag is Topic selectedTopic)
            {
                DatabaseService.MarkTopicAsSelected(selectedTopic.Id);

                // Odświeżamy zarówno górny spis wybranych tematów, jak i listę po prawej
                LoadSelectedTopics();
                TopicsItemsControl.ItemsSource = DatabaseService.GetTopicsByDepartment(selectedTopic.DepartmentId);
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
