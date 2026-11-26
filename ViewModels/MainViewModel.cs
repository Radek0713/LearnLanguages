using LearnLanguages.Models;
using LearnLanguages.Services;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;

        public IDatabaseService DatabaseService { get; }
        public ILLMService LLMService { get; }
        public IDialogService DialogService { get; }

        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public ProfileModel CurrentProfile
        {
            get => DatabaseService.CurrentProfile;
            set
            {
                if (DatabaseService.CurrentProfile != value)
                {
                    DatabaseService.CurrentProfile = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel()
        {
            DatabaseService = new MySqlDatabaseService();
            LLMService = new GroqService(DatabaseService);
            DialogService = new DialogService();

            TestDatabaseConnection();

            CurrentViewModel = new ProfilesViewModel(this);
        }
        private void TestDatabaseConnection()
        {
            if (DatabaseService.TestConnection(out string errorMessage))
            {
                // nie ma potrzeby wyświetlać jeśli połączono z bazą
                //DialogService.ShowInfo("✅ Połączono z bazą danych!", "Sukces");
            }
            else
            {
                DialogService.ShowError(
                    $"❌ Błąd połączenia z bazą danych:\n{errorMessage}\n\nSprawdź poprawność danych w pliku .env!",
                    "Błąd bazy danych");
            }
        }

        public void NavigateTo(BaseViewModel nextViewModel)
        {
            CurrentViewModel = nextViewModel;
        }
    }
}