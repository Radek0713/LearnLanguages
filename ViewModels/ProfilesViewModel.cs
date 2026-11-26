using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LearnLanguages.Models;
using LearnLanguages.ViewModels.Base;

namespace LearnLanguages.ViewModels
{
    public class ProfilesViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<ProfileModel> Profiles { get; }
        public bool HasNoProfiles => Profiles.Count == 0;
        private const int MaxProfilesCount = 5;

        public ICommand SelectProfileCommand { get; }
        public ICommand CreateNewProfileCommand { get; }
        public ICommand ExitAppCommand { get; }

        public ProfilesViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            Profiles = new ObservableCollection<ProfileModel>(_mainViewModel.DatabaseService.GetProfiles());

            SelectProfileCommand = new RelayCommand(SelectProfile);
            CreateNewProfileCommand = new RelayCommand(CreateNewProfile);
            ExitAppCommand = new RelayCommand(ExitApp);
        }

        private void SelectProfile(object parameter)
        {
            if (parameter is ProfileModel selectedProfile)
            {
                _mainViewModel.CurrentProfile = selectedProfile;
                _mainViewModel.NavigateTo(new StartMenuViewModel(_mainViewModel));
            }
        }
        private void CreateNewProfile()
        {
            if (Profiles.Count >= MaxProfilesCount)
            {
                _mainViewModel.DialogService.ShowInfo(
                    $"Osiągnięto maksymalną liczbę profili ({MaxProfilesCount}). Usuń jeden z istniejących profili, aby utworzyć nowy.",
                    "Limit profili");
                return;
            }

            _mainViewModel.NavigateTo(new CreateProfileViewModel(_mainViewModel));
        }

        private void ExitApp()
        {
            if (_mainViewModel.DialogService.ShowConfirmation(
                "Czy na pewno chcesz zamknąć aplikację?",
                "Zamykanie aplikacji"))
            {
                Application.Current.Shutdown();
            }
        }
    }
}