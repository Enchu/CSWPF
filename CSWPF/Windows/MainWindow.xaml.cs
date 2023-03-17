using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Utils;
using Newtonsoft.Json;
using SteamAuth;

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
        
        void Load()
        {
            Check();
            foreach (var db in _users)
            {
                //login
                var login = new Label();
                login.Content = db.Login;
                PanelForLogin.Children.Add(login);
                //password
                var password = new Button();
                password.Content = "Copy";
                password.Click += db.ClickPassword;
                PanelForPassword.Children.Add(password);
                //start
                var startCS = new Button();
                startCS.Content = "Go";
                startCS.Click += db.ClickStart;
                PanelForButton.Children.Add(startCS);
                //invent
                var checkInventory = new Button();
                checkInventory.Content = "Check";
                checkInventory.Click += db.ClickCheckInventory;
                //PanelForButton.Children.Add(checkInventory);
            }
            _users.Clear();
        }

        private void AddBtClick(object sender, RoutedEventArgs e)
        {
            if (Msg.ShowQuestion("Вы действительно хотите добавить?"))
            {
                User newUser = new User(LoginTextBox.Text, PasswordTextBox.Text);
                Helper.SaveToDB(newUser);
                Msg.ShowInfo("Данные добавлены");
                Load();
                CollapsedAll(Visibility.Visible);
                PanelForAdd.Visibility = Visibility.Collapsed;
            }
        }

        private void CollapsedAll(Visibility visibility)
        {
            PanelForLogin.Visibility = visibility;
            PanelForPassword.Visibility = visibility;
            PanelForButton.Visibility = visibility;
            PanelFor.Visibility = visibility;
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
    }
}