using System;
using CSWPF.Core;
using CSWPF.MVVM.Model.Interface;

namespace CSWPF.Services;

public interface INavigationService
{
    ViewModel CurrentView { get; set; }
    void NavigateTo<T>() where T : ViewModel;
}

public class NavigationService: ObservableObject, INavigationService
{
    private readonly Func<Type, ViewModel> _viewModelFactory;
    private ViewModel _currentView;

    public ViewModel CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            OnPropertyChanged();
        }
    }

    public NavigationService(Func<Type, ViewModel> viewModelFactory)
    {
        _viewModelFactory = viewModelFactory;
    }
    
    public void NavigateTo<TViewModel>() where TViewModel : ViewModel
    {
        ViewModel viewModel = _viewModelFactory.Invoke(typeof(TViewModel));
        CurrentView = viewModel;
    }
    
}