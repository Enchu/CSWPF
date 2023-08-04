using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSWPF.Direct;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Utils;
using Newtonsoft.Json;
using SteamAuth;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.Forms.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace CSWPF.Windows
{
    public partial class MainWindow
    {
        private static List<Account> _users = new();
        private static void Check()
        {
            foreach (var filename in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + @"\Account\", "*.json"))
            {
                Account allUsers = JsonConvert.DeserializeObject<Account>(File.ReadAllText(filename));
                _users.Add(allUsers);
            }
        }
        
        private void Load()
        {
            Check();
            CreateChildren();
            _users.Clear();
        }

        private void createButton(string iconsName ,MouseButtonEventHandler click, StackPanel stackPanel, string login)
        {
            Button button = new Button();
            Image image = new Image();
            image.Stretch = Stretch.Uniform;
            image.Source = new BitmapImage(new Uri($"pack://application:,,,/Icons/{iconsName}"));
            button.Content = image;
            button.Style = this.FindResource("ImageButtonStyle") as Style;
            button.MouseUp += click;
            button.Tag = login;

            stackPanel.Children.Add(button);
        }

        private void createCheckBox(string content, StackPanel stackPanel)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Margin = new Thickness(5);
            checkBox.Content = content;
            /*
              checkBoxPrime.IsChecked = db.Prime;
              checkBoxPrime.Checked += db.CheckPrime;
             */

            stackPanel.Children.Add(checkBox);
        }

        private void CreateChildren()
        {
            foreach (var db in _users)
            {
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;
                
                //login
                var login = new Label();
                login.Content = db.Login;
                PanelForLogin.Children.Add(login);

                //password
                createButton("screwdriver.png", ClickPassword, PanelForPassword, db.Login);

                //start
                createButton("cow.png", ClickStart, stackPanel, db.Login);
                
                //kill
                createButton("cloud-lightning.png", ClickKill, PanelForKill, db.Login);

                //open Steam
                createButton("steam.png", ClickOpenSteam, PanelForOpenSteam, db.Login);

                //checkbox
                createCheckBox("F", stackPanel);
                createCheckBox("Prime", stackPanel);

                //invent and trade
                createButton("irrigation.png", ClickTrade, stackPanel, db.Login);

                PanelForStart.Children.Add(stackPanel);
            }
            
            _users.Clear();
        }

        Account account;
        
        private void ClickPassword(object sender, RoutedEventArgs e)
        {
            
            Clipboard.SetText(((Button)sender).Tag.ToString());
        }

        private void ClickStart(object sender, RoutedEventArgs e)
        {

        }

        private void ClickKill(object sender, RoutedEventArgs e)
        {

        }
        
        private void ClickOpenSteam(object sender, RoutedEventArgs e)
        {

        }

        private void ClickTrade(object sender, RoutedEventArgs e)
        {

        }

        private void AddBtClick(object sender, RoutedEventArgs e)
        {
            if (Msg.ShowQuestion("Вы действительно хотите добавить?"))
            {
                User newUser = new User();
                LoginTextBox.Clear();
                PasswordTextBox.Clear();
                
                HelperCS.SaveToDB(newUser);
                ClearAll();
                Load();
                
                Msg.ShowInfo("Данные добавлены");
                CollapsedAll(Visibility.Visible);
                PanelForAdd.Visibility = Visibility.Collapsed;
            }
        }

        private void CollapsedAll(Visibility visibility)
        {
            PanelForLogin.Visibility = visibility;
            PanelForPassword.Visibility = visibility;
            PanelForStart.Visibility = visibility;
            PanelFor.Visibility = visibility;
            PanelForKill.Visibility = visibility;
            PanelForOpenSteam.Visibility = visibility;
        }

        private void ClearAll()
        {
            PanelForLogin.Children.Clear();
            PanelForPassword.Children.Clear();
            PanelForStart.Children.Clear();
            PanelFor.Children.Clear();
            PanelForKill.Children.Clear();
            PanelForOpenSteam.Children.Clear();
        }

        public MainWindow()
        {
            InitializeComponent();
            Load();
        }

        private void HomeClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Visible);
            PanelForAdd.Visibility = Visibility.Collapsed;
            PanelForSettings.Visibility = Visibility.Collapsed;
        }

        private void AddClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Collapsed);
            PanelForAdd.Visibility = Visibility.Visible;
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

        private void BackBtClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Visible);
            PanelForAdd.Visibility = Visibility.Collapsed;
        }

        private void SettingClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Collapsed);
            PanelForSettings.Visibility = Visibility.Visible;
        }
    }
}