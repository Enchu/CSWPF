using System.Windows.Input;
using CSWPF.Core;
using CSWPF.MVVM.Model.Interface;
using CSWPF.MVVM.View;


namespace CSWPF.MVVM.ViewModel;

public class MainViewModel: ObservableObject
{
    private object _currentView;
    public object CurrentView
    {
        get { return _currentView; }
        set { _currentView = value; OnPropertyChanged(); }
    }
    
    public ICommand HomeCommand { get; set; }
    public ICommand AddCommand { get; set; }
    public ICommand SettingCommand { get; set; }

    private void Home(object obj) => CurrentView = new HomeView();
    private void Add(object obj) => CurrentView = new AddingUsersView();
    private void Setting(object obj) => CurrentView = new SettingView();

    public MainViewModel()
    {
        CurrentView = new HomeViewModel();

        HomeCommand = new RelayCommand(Home);
        AddCommand = new RelayCommand(Add);
        SettingCommand = new RelayCommand(Setting);
    }
}