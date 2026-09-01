using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LearnLanguages.Services;

namespace LearnLanguages.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainContentArea.Content = new Controls.StartMenu();

            // 2. Testujemy połączenie z bazą przy starcie
            TestDatabaseConnection();
        }

        private void TestDatabaseConnection()
        {
            try
            {
                using (var connection = DatabaseService.GetConnection())
                {
                    connection.Open();
    
                    MessageBox.Show("✅ Połączono z bazą danych!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Błąd połączenia z bazą danych:\n{ex.Message}\n\nSprawdź poprawność danych w pliku .env!",
                                "Błąd bazy danych",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}