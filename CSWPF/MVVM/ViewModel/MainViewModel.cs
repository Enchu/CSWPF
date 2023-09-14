using CSWPF.Core;
using CSWPF.MVVM.Model.Interface;
using CSWPF.MVVM.View;
using CSWPF.Services;

namespace CSWPF.MVVM.ViewModel;

public class MainViewModel: Core.ViewModel
{
    private IUserService _userService;

    public INavigationService _navigation;
    public INavigationService Navigation
    {
        get => _navigation;
        set
        {
            _navigation = value;
            OnPropertyChanged();
        } 
    }

    public RelayCommand NavigateToHomeCommand { get; set; }
    public RelayCommand NavigateToSettingsViewCommand { get; set; }
    public RelayCommand NavigateToAddViewCommand { get; set; }
    
    public MainViewModel(INavigationService navigationService)
    {
        Navigation = navigationService;

        NavigateToHomeCommand = new RelayCommand(o => { Navigation.NavigateTo<HomeViewModel>(); },  o => true);
        NavigateToSettingsViewCommand = new RelayCommand(o => { Navigation.NavigateTo<SettingViewModel>(); },  o => true);
        NavigateToAddViewCommand = new RelayCommand(o => { Navigation.NavigateTo<AddViewModel>(); },  o => true);
    }
}