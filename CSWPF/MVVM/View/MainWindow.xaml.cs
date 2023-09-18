using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.MVVM.Model;
using CSWPF.MVVM.Model.Interface;
using CSWPF.MVVM.View;
using CSWPF.MVVM.ViewModel;
using CSWPF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SteamAuth;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using static CSWPF.Helpers.HelperCS;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using MessageBox = System.Windows.Forms.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace CSWPF.Windows
{
    public partial class MainWindow
    {
        private static List<User> _users = new();

        public MainWindow()
        {
            InitializeComponent();
            
            Settings.lineChanger("#0.0.0.0 store.steampowered.com", Settings.Hosts, 25);
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dependencyObject); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void StartClick(object sender, RoutedEventArgs e)
        {
            _users.Clear();

            foreach (CheckBox checkBox in FindVisualChildren<CheckBox>(PanelCurrentView))
            {
                if (checkBox.IsChecked == true && checkBox.Tag is User user)
                {
                    _users.Add(user);
                }
            }
            
            StartLive(_users);
        }

        private void KillClick(object sender, RoutedEventArgs e)
        {
            AccountHelper.KillAll();
            Settings.lineChanger("0.0.0.0 store.steampowered.com", Settings.Hosts, 25);
        }

        private void TestClick(object sender, RoutedEventArgs e)
        {
            TestProces.GetProcess();
        }

        private void SDAClick(object sender, RoutedEventArgs e)
        {
            StartAny($"{Settings.SDA}Steam Desktop Authenticator.exe");
        }
        
        private void MEMClick(object sender, RoutedEventArgs e)
        {
            StartAny(@"C:\Program Files\Mem Reduct\memreduct.exe");
        }

        private void ToolClick(object sender, RoutedEventArgs e)
        {
            StartAny(@"\D:\Game\SteamRootTools\SteamRouteTool.exe");
        }

        private void StartAny(string fileName)
        {
            try
            {
                new Process()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        UseShellExecute = false,
                        FileName = fileName,
                        WorkingDirectory = new FileInfo(fileName).Directory.FullName
                    }
                }.Start();
            }
            catch
            {
                MessageBox.Show("Не могу открыть программу ***.exe");
            }
        }
        
    }
}