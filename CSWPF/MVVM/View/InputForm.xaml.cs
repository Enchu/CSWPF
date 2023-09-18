using System.Windows;

namespace CSWPF.Windows;

public partial class InputForm : Window
{
    public bool Canceled = false;
    private bool userClosed = true;
    public InputForm(string label, bool password = false)
    {
        InitializeComponent();
        this.labelText.Content = label;
    }

    private void BtnAccept_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(this.txtBox.Text))
        {
            this.Canceled = true;
            this.userClosed = false;
            this.Close();
        }
        else
        {
            this.Canceled = false;
            this.userClosed = false;
            this.Close();
        }
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        this.Canceled = true;
        this.userClosed = false;
        this.Close();
    }
}