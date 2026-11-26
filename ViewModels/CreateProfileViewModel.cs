using System;
using System.Windows.Input;
using LearnLanguages.ViewModels.Base;
using MySql.Data.MySqlClient;
using Microsoft.Data.Sqlite;

namespace LearnLanguages.ViewModels
{
    public class CreateProfileViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private string _profileName;

        public string ProfileName
        {
            get => _profileName;
            set => SetProperty(ref _profileName, value);
        }

        public ICommand SaveProfileCommand { get; }
        public ICommand CancelCommand { get; }

        public CreateProfileViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            SaveProfileCommand = new RelayCommand(SaveProfile);
            CancelCommand = new RelayCommand(() =>
                _mainViewModel.NavigateTo(new ProfilesViewModel(_mainViewModel)));
        }

        private void SaveProfile()
        {
            string trimmedName = ProfileName?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                _mainViewModel.DialogService.ShowWarning(
                    "Wpisz nazwę profilu przed zapisaniem.",
                    "Brak nazwy");
                return;
            }

            try
            {
                if (_mainViewModel.DatabaseService.GetProfiles().Count >= 5)
                {
                    _mainViewModel.DialogService.ShowInfo(
                        "Osiągnięto maksymalną liczbę profili (5). Usuń jeden z istniejących profili, aby utworzyć nowy.",
                        "Limit profili");
                    return;
                }
                _mainViewModel.DatabaseService.CreateProfile(trimmedName);
                _mainViewModel.NavigateTo(new ProfilesViewModel(_mainViewModel));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                _mainViewModel.DialogService.ShowWarning(
                    "Profil o takiej nazwie już istnieje. Wybierz inną nazwę.",
                    "Duplikat nazwy");
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                _mainViewModel.DialogService.ShowWarning(
                    "Profil o takiej nazwie już istnieje. Wybierz inną nazwę.",
                    "Duplikat nazwy");
            }
            catch (Exception ex)
            {
                _mainViewModel.DialogService.ShowError(
                    $"Błąd podczas tworzenia profilu:\n{ex.Message}",
                    "Błąd bazy danych");
            }
        }
    }
}