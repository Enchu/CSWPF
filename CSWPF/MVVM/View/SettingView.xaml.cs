using System.IO;
using System.Windows;
using System.Windows.Forms;
using CSWPF.Utils;
using UserControl = System.Windows.Controls.UserControl;

namespace CSWPF.MVVM.View;

public partial class SettingView : UserControl
{
    public SettingView()
    {
        InitializeComponent();
    }
    
    private void TBSteamPathClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fm = new OpenFileDialog();
        fm.ShowDialog();
        Msg.ShowInfo(Path.GetFullPath(fm.FileName));
    }
}