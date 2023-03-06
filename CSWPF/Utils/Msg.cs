using System.Windows;

namespace CSWPF.Utils;

public class Msg
{
    public static void ShowInfo(string msg)
    {
        MessageBox.Show(msg, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public static void ShowError(string msg)
    {
        MessageBox.Show(msg, "Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
    }

    public static bool ShowQuestion(string msg)
    {
        return MessageBox.Show(msg, "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
               MessageBoxResult.Yes;
    }
}