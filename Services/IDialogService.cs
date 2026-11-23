namespace LearnLanguages.Services
{
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "Informacja");
        void ShowWarning(string message, string title = "Ostrzeżenie");
        void ShowError(string message, string title = "Błąd");
        bool ShowConfirmation(string message, string title = "Potwierdzenie");
    }
}
