using System.Collections.ObjectModel;
using CSWPF.Helpers;

namespace CSWPF.Core;

public class ObservableCollectionRoute: ObservableObject
{
    private ObservableCollection<Route> routesCollection = new ObservableCollection<Route>();

    public ObservableCollection<Route> RoutesCollection
    {
        get { return routesCollection; }
        set { routesCollection = value; OnPropertyChanged(); }
    }
}