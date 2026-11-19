using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LearnLanguages.Models;
using LearnLanguages.Services;
using MySql.Data.MySqlClient;

namespace LearnLanguages.Views.Controls
{
    public partial class TasksMenu : UserControl
    {
        private List<string> topicWords = new List<string>();
        private GrammarModel selectedGrammarTopic = null;
        private Queue<ExerciseTaskModel> taskQueue = new Queue<ExerciseTaskModel>();

        private ExerciseTaskModel currentTask = null;
        private int solvedCount = 0;
        private bool isGrammarMode = false;

        public TasksMenu()
        {
            InitializeComponent();
        }

        private void btn_back_to_menu_Click(object sender, RoutedEventArgs e)
        {
            pnl_vocabulary_selection.Visibility = Visibility.Collapsed;
            pnl_grammar_selection.Visibility = Visibility.Collapsed;
            pnl_quiz.Visibility = Visibility.Collapsed;
            pnl_task_menu.Visibility = Visibility.Visible;
            taskQueue.Clear();
        }

        // Obsługa powrotu do głównego menu aplikacji (LanguageMenu)
        private void btn_back_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainContentArea.Content = new LanguageMenu();
            }
        }

        private void btn_vocab_menu_Click(object sender, RoutedEventArgs e)
        {
            pnl_task_menu.Visibility = Visibility.Collapsed;
            pnl_vocabulary_selection.Visibility = Visibility.Visible;

            var departments = DatabaseService.GetDepartments();
            var allTopics = new List<Topic>();
            foreach (var dept in departments)
            {
                allTopics.AddRange(DatabaseService.GetTopicsByDepartment(dept.Id));
            }
            cb_topics.ItemsSource = allTopics;
            if (allTopics.Count > 0) cb_topics.SelectedIndex = 0;
        }

        private void btn_grammar_menu_Click(object sender, RoutedEventArgs e)
        {
            pnl_task_menu.Visibility = Visibility.Collapsed;
            pnl_grammar_selection.Visibility = Visibility.Visible;

            var grammarList = LoadGrammarFromDatabase();
            cb_grammar.ItemsSource = grammarList;
            if (grammarList.Count > 0) cb_grammar.SelectedIndex = 0;
        }

        private List<GrammarModel> LoadGrammarFromDatabase()
        {
            var list = new List<GrammarModel>();
            using (var connection = DatabaseService.GetConnection())
            {
                connection.Open();
                var cmd = new MySqlCommand("SELECT id, nazwa FROM gramatyka", connection);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new GrammarModel
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("nazwa")
                        });
                    }
                }
            }
            return list;
        }

        private async void btn_generate_vocab_tasks_Click(object sender, RoutedEventArgs e)
        {
            if (cb_topics.SelectedItem is Topic selectedTopic)
            {
                topicWords = DatabaseService.GetVocabularyWordsByTopic(selectedTopic.Id);
                if (topicWords == null || topicWords.Count == 0)
                {
                    MessageBox.Show("Wybrany temat nie zawiera żadnych słówek w bazie!", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                isGrammarMode = false;
                pnl_vocabulary_selection.Visibility = Visibility.Collapsed;
                pnl_quiz.Visibility = Visibility.Visible;
                solvedCount = 0;
                taskQueue.Clear();

                await LoadMoreTasksAsync();
            }
            else
            {
                MessageBox.Show("Wybierz temat ze słówkami!");
            }
        }

        private async void btn_generate_grammar_tasks_Click(object sender, RoutedEventArgs e)
        {
            if (cb_grammar.SelectedItem is GrammarModel selectedGrammar)
            {
                selectedGrammarTopic = selectedGrammar;
                isGrammarMode = true;
                pnl_grammar_selection.Visibility = Visibility.Collapsed;
                pnl_quiz.Visibility = Visibility.Visible;
                solvedCount = 0;
                taskQueue.Clear();

                await LoadMoreTasksAsync();
            }
            else
            {
                MessageBox.Show("Wybierz zagadnienie gramatyczne!");
            }
        }

        private async System.Threading.Tasks.Task LoadMoreTasksAsync()
        {
            txt_loading.Visibility = Visibility.Visible;
            btn_next.Visibility = Visibility.Collapsed;
            txt_feedback.Text = "";
            HideQuizContent(true);

            try
            {
                List<ExerciseTaskModel> resultList = null;

                if (!isGrammarMode)
                {
                    resultList = await GroqService.GenerateBatchTasksForWordsAsync(topicWords, 5);
                }
                else
                {
                    resultList = await GroqService.GenerateGrammarTasksAsync(selectedGrammarTopic.Id, count: 5);
                }

                txt_loading.Visibility = Visibility.Collapsed;

                if (resultList != null && resultList.Count > 0)
                {
                    foreach (var task in resultList)
                    {
                        taskQueue.Enqueue(task);
                    }

                    ShowNextTaskFromQueue();
                }
                else
                {
                    ShowApiErrorState("Nie udało się wygenerować zadań. Serwer zwrócił pustą odpowiedź.");
                }
            }
            catch (Exception ex)
            {
                txt_loading.Visibility = Visibility.Collapsed;

                // Sprawdzamy czy to błąd związany z limitem (np. 429 Too Many Requests) lub ogólny błąd sieciowy/serwera
                string errorMessage = "Chwilowo serwer AI jest mocno obciążony lub wystąpił problem z połączeniem.";
                if (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
                {
                    errorMessage = "Chwilowo AI jest przeciążone (przekroczono limit zapytań w darmowej wersji Groq).";
                }

                ShowApiErrorState(errorMessage);
            }
        }

        // Pomocnicza metoda wyświetlająca stan błędu w panelu quizu zamiast wyskakującego okienka
        private void ShowApiErrorState(string message)
        {
            HideQuizContent(true);
            txt_question.Visibility = Visibility.Visible;
            txt_question.Text = $"⚠️ {message}\n\nCo chcesz zrobić?";

            // Przekształcamy przycisk 'btn_next' lub obsługujemy go w interfejsie jako "Generuj ponownie"
            btn_next.Content = "🔄 Generuj ponownie";
            btn_next.Visibility = Visibility.Visible;

            // Opcjonalnie: jeśli chcesz umożliwić powrót do wyboru kategorii bezpośrednio z widoku błędu,
            // możesz sterować widocznością dodatkowego przycisku cofnięcia.
        }

        private void ShowNextTaskFromQueue()
        {
            // Przywracamy standardową nazwę przycisku następnego zadania (na wypadek, gdyby był wcześniej zmieniony na "Generuj ponownie")
            btn_next.Content = "Następne zadanie";

            if (taskQueue.Count > 0)
            {
                currentTask = taskQueue.Dequeue();
                solvedCount++;
                txt_question.Text = $"Zadanie #{solvedCount}:\n{currentTask.TrescZdania}";

                if (currentTask.Opcje != null && currentTask.Opcje.Count >= 4)
                {
                    btn_option1.Content = currentTask.Opcje[0];
                    btn_option2.Content = currentTask.Opcje[1];
                    btn_option3.Content = currentTask.Opcje[2];
                    btn_option4.Content = currentTask.Opcje[3];
                }

                ResetButtons();
                HideQuizContent(false);
                txt_feedback.Text = "";
                btn_next.Visibility = Visibility.Collapsed;
            }
            else
            {
                _ = LoadMoreTasksAsync();
            }
        }

        private void Answer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && currentTask != null)
            {
                string selectedAnswer = clickedButton.Content?.ToString();
                ToggleButtonsEnabled(false);

                if (selectedAnswer == currentTask.PoprawnaOdpowiedz)
                {
                    clickedButton.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    txt_feedback.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    txt_feedback.Text = $"✅ Poprawna odpowiedź!\nWyjaśnienie: {currentTask.Wyjasnienie}";
                }
                else
                {
                    clickedButton.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    txt_feedback.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    txt_feedback.Text = $"❌ Błędna odpowiedź. Poprawna: {currentTask.PoprawnaOdpowiedz}\nWyjaśnienie: {currentTask.Wyjasnienie}";

                    HighlightCorrect(currentTask.PoprawnaOdpowiedz);
                }

                btn_next.Visibility = Visibility.Visible;
            }
        }

        private async void btn_next_Click(object sender, RoutedEventArgs e)
        {
            // Jeśli przycisk pełni rolę ponownego generowania po błędzie:
            if (btn_next.Content.ToString().Contains("Generuj ponownie"))
            {
                await LoadMoreTasksAsync();
                return;
            }

            if (taskQueue.Count > 0)
            {
                ShowNextTaskFromQueue();
            }
            else
            {
                await LoadMoreTasksAsync();
            }
        }

        private void HideQuizContent(bool hide)
        {
            Visibility v = hide ? Visibility.Collapsed : Visibility.Visible;
            txt_question.Visibility = v;
            btn_option1.Visibility = v;
            btn_option2.Visibility = v;
            btn_option3.Visibility = v;
            btn_option4.Visibility = v;
        }

        private void ResetButtons()
        {
            var defaultColor = (Brush)new BrushConverter().ConvertFrom("#2563EB");
            btn_option1.Background = defaultColor;
            btn_option2.Background = defaultColor;
            btn_option3.Background = defaultColor;
            btn_option4.Background = defaultColor;

            ToggleButtonsEnabled(true);
        }

        private void ToggleButtonsEnabled(bool isEnabled)
        {
            btn_option1.IsEnabled = isEnabled;
            btn_option2.IsEnabled = isEnabled;
            btn_option3.IsEnabled = isEnabled;
            btn_option4.IsEnabled = isEnabled;
        }

        private void HighlightCorrect(string correct)
        {
            var green = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            if (btn_option1.Content?.ToString() == correct) btn_option1.Background = green;
            if (btn_option2.Content?.ToString() == correct) btn_option2.Background = green;
            if (btn_option3.Content?.ToString() == correct) btn_option3.Background = green;
            if (btn_option4.Content?.ToString() == correct) btn_option4.Background = green;
        }
    }
}