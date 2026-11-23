using System;
using System.Windows.Input;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class StartMenuViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public string ProfileDisplayName =>
            _mainViewModel.CurrentProfile != null
                ? $"👤 {_mainViewModel.CurrentProfile.Name}"
                : "👤 Profil";

        public ICommand StartEnglishCommand { get; }
        public ICommand ChangeProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }

        public StartMenuViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            StartEnglishCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new LanguageMenuViewModel(_mainViewModel)));

            ChangeProfileCommand = new RelayCommand(() =>
            {
                _mainViewModel.CurrentProfile = null;
                _mainViewModel.NavigateTo(new ProfilesViewModel(_mainViewModel));
            });

            DeleteProfileCommand = new RelayCommand(DeleteProfile);
        }

        private void DeleteProfile()
        {
            if (_mainViewModel.CurrentProfile == null) return;

            string profileName = _mainViewModel.CurrentProfile.Name;

            if (_mainViewModel.DialogService.ShowConfirmation(
                $"Czy na pewno chcesz trwale usunąć profil „{profileName}” wraz z całym postępem nauki?\nTej operacji nie można cofnąć.",
                "Ostrzeżenie – usuwanie profilu"))
            {
                try
                {
                    _mainViewModel.DatabaseService.DeleteProfile(_mainViewModel.CurrentProfile.Id);
                    _mainViewModel.CurrentProfile = null;
                    _mainViewModel.NavigateTo(new ProfilesViewModel(_mainViewModel));
                }
                catch (Exception ex)
                {
                    _mainViewModel.DialogService.ShowError(
                        $"Błąd podczas usuwania profilu:\n{ex.Message}",
                        "Błąd bazy danych");
                }
            }
        }
    }
}