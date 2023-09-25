using System.Collections.Generic;
using System.ComponentModel;
using CSWPF.Core;

namespace CSWPF.Helpers;

public class Route: ObservableObject
{
    private string _name;
    public string Name
    {
        get { return _name; }
        set { _name = value; OnPropertyChanged(); }
    }
    
    private Dictionary<string, string> _ranges;
    public Dictionary<string, string> Ranges
    {
        get { return _ranges; }
        set { _ranges = value; OnPropertyChanged(nameof(Ranges)); }
    }
    
    private List<int> _rowIndex;
    public List<int> RowIndex
    {
        get { return _rowIndex; }
        set { _rowIndex = value; OnPropertyChanged(); }
    }
    
    private string _desc;
    public string Desc
    {
        get { return _desc; }
        set { _desc = value; OnPropertyChanged(); }
    }
    
    private bool _extended;
    public bool Extended
    {
        get { return _extended; }
        set { _extended = value; OnPropertyChanged(); }
    }
    
    private bool _pw;
    public bool PW
    {
        get { return _pw; }
        set { _pw = value; OnPropertyChanged(); }
    }
    
    private bool _allCheck;
    public bool AllCheck
    {
        get { return _allCheck; }
        set { _allCheck = value; OnPropertyChanged(); }
    }
}