using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Utils;
using Newtonsoft.Json;

namespace CSWPF.Windows
{
    public partial class MainWindow
    {
        //-console -no-browser -window -w 640 -h 480 -novid -nosound +connect 185.255.133.169:20065 192.168.31.220:27015
        private static List<User> _users = new();
        private static void Check()
        {
            foreach (var filename in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + @"\Account\", "*.json"))
            {
                User allUsers = JsonConvert.DeserializeObject<User>(File.ReadAllText(filename));
                allUsers.CheckAccount();
                _users.Add(allUsers);
            }
        }
        
        private void Load()
        {
            Check();
            CreateChildren();
            _users.Clear();
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
                var password = new Button();
                Image passwordContent = new Image();
                passwordContent.Stretch = Stretch.Uniform;
                passwordContent.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/screwdriver.png"));
                password.Content = passwordContent;
                password.Style = this.FindResource("ImageButtonStyle") as Style;
                password.Click += db.ClickPassword;
                PanelForPassword.Children.Add(password);
                
                //start
                var startCS = new Button();
                Image startCSContent = new Image();
                startCSContent.Stretch = Stretch.Uniform;
                startCSContent.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/cow.png"));
                startCS.Content = startCSContent;
                startCS.Style = this.FindResource("ImageButtonStyle") as Style;
                //startCS.Style = Application.Current.FindResource("ImageButtonStyle") as Style;
                startCS.Click += db.ClickStart;
                stackPanel.Children.Add(startCS);
                
                //kill
                var killCS = new Button();
                Image killCsContent = new Image();
                killCsContent.Stretch = Stretch.Uniform;
                killCsContent.Source = new BitmapImage(new Uri("pack://application:,,,/Icons/cloud-lightning.png"));
                killCS.Content = killCsContent;
                killCS.Style = this.FindResource("ImageButtonStyle") as Style;
                killCS.Click += db.ClickKill;
                PanelForKill.Children.Add(killCS);
                
                //open Steam
                var openSteam = new Button();
                openSteam.Content = "Steam";
                openSteam.Click += db.ClickOpenSteam;
                //PanelForOpenSteam.Children.Add(openSteam);
                
                //invent
                var checkInventory = new Button();
                checkInventory.Content = "Check";
                checkInventory.Click += db.ClickCheckInventory;
                //PanelForButton.Children.Add(checkInventory);
                
                //checkbox
                CheckBox checkBoxFinal = new CheckBox();
                checkBoxFinal.Margin = new Thickness(5);
                checkBoxFinal.Content = "F";
                stackPanel.Children.Add(checkBoxFinal);

                CheckBox checkBoxPrime = new CheckBox();
                checkBoxPrime.Margin = new Thickness(5);
                checkBoxPrime.Content = "Prime";
                checkBoxPrime.IsChecked = db.Prime;
                checkBoxPrime.Checked += db.CheckPrime;
                stackPanel.Children.Add(checkBoxPrime);

                PanelForStart.Children.Add(stackPanel);
            }
            
            _users.Clear();
        }

        private void AddBtClick(object sender, RoutedEventArgs e)
        {
            if (Msg.ShowQuestion("Вы действительно хотите добавить?"))
            {
                User newUser = new User(LoginTextBox.Text, PasswordTextBox.Text);
                LoginTextBox.Clear();
                PasswordTextBox.Clear();
                
                Helper.SaveToDB(newUser);
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
        
        private void AddClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Collapsed);
            PanelForAdd.Visibility = Visibility.Visible;
        }

        private void BackBtClick(object sender, RoutedEventArgs e)
        {
            CollapsedAll(Visibility.Visible);
            PanelForAdd.Visibility = Visibility.Collapsed;
        }
    }
}