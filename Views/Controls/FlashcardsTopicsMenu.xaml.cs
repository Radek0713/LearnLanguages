using LearnLanguages.Models;
using LearnLanguages.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LearnLanguages.Views.Controls
{
    public partial class FlashcardsTopicsMenu : UserControl
    {
        public FlashcardsTopicsMenu()
        {
            InitializeComponent();
            LoadTopics();
        }

        private void LoadTopics()
        {
            List<TopicReviewSummary> topics = DatabaseService.GetTopicReviewSummariesForToday();

            int totalToday = topics.Sum(t => t.TodayCount);
            int totalOverdue = topics.Sum(t => t.OverdueCount);
            int totalAll = totalToday + totalOverdue;

            TxtReviewAll.Text = $"Powtórz wszystkie ({totalAll})";
            TxtReviewOverdue.Text = $"Tylko zaległe ({totalOverdue})";

            BtnReviewAll.IsEnabled = totalAll > 0;
            BtnReviewOverdue.IsEnabled = totalOverdue > 0;

            TopicsItemsControl.ItemsSource = topics;
        }

        private void BtnTopicCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TopicReviewSummary selectedTopic)
            {
                OpenFlashcards(topicId: selectedTopic.TopicId, onlyOverdue: false);
            }
        }

        private void BtnReviewAll_Click(object sender, RoutedEventArgs e)
        {
            OpenFlashcards(topicId: null, onlyOverdue: false);
        }

        private void BtnReviewOverdue_Click(object sender, RoutedEventArgs e)
        {
            OpenFlashcards(topicId: null, onlyOverdue: true);
        }

        private void OpenFlashcards(int? topicId, bool onlyOverdue)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new FlashcardsMenu(topicId, onlyOverdue);
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