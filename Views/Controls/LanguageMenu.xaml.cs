using LearnLanguages.View.Controls;
using LearnLanguages.Views.Controls;
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
    /// Logika interakcji dla klasy UserControl1.xaml
    /// </summary>
    public partial class LanguageMenu : UserControl
    {
        public LanguageMenu()
        {
            InitializeComponent();
        }

        private void btn_calendar_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Otwieramy nasz nowy, genialny kalendarz
                mainWindow.MainContentArea.Content = new CalendarMenu();
            }
        }

        private void btn_topics_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow != null)
            {
                // Podmień aktualny widok (LanguageMenu) na nowy widok (TopicsMenu)
                mainWindow.MainContentArea.Content = new TopicsMenu();
            }
        }

        private void btn_flashcards_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btn_tasks_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
