using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSWPF.Directory;
using CSWPF.Helpers;
using Newtonsoft.Json;

namespace CSWPF.MVVM.View;

public partial class HomeView : UserControl
{
    private static List<User> _users = new();
    public HomeView()
    {
        InitializeComponent();
        Load();
    }
    private void Load()
    {
        Check();
        CreateChildren();
    }

    private void Check()
    {
        foreach (var filename in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory() + @"\Account\", "*.json"))
        {
            User allUsers = JsonConvert.DeserializeObject<User>(File.ReadAllText(filename));
            allUsers.CheckAccount();
            _users.Add(allUsers);
        }
    }

    private void createButton(string iconsName, RoutedEventHandler click, StackPanel stackPanel, string login)
    {
        Button button = new Button();
        Image image = new Image();
        image.Stretch = Stretch.Uniform;
        image.Source = new BitmapImage(new Uri($"pack://application:,,,/Icons/{iconsName}"));
        button.Content = image;
        button.Style = this.FindResource("ImageButtonStyle") as Style;
        button.Click += click;
        button.Tag = login;

        stackPanel.Children.Add(button);
    }

    public void SelectedMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            CheckBox checkBox = sender as CheckBox;
            checkBox.IsChecked = !checkBox.IsChecked;
        }
    }

    private void CreateChildren()
    {
        foreach (var db in _users)
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            //login
            var login = new Button();
            login.Content = db.Login;
            login.Click += db.ClickLogin;
            stackPanel.Children.Add(login);

            //password
            createButton("screwdriver.png", db.ClickPassword, stackPanel, db.Login);

            //start
            createButton("cow.png", db.ClickStart, stackPanel, db.Login);

            //open Steam
            //createButton("steam.png", db.ClickOpenSteam, PanelForOpenSteam, db.Login);

            //checkbox
            CheckBox checkBoxF = new CheckBox();
            checkBoxF.Margin = new Thickness(5);
            checkBoxF.Content = "F";
            checkBoxF.Tag = db;
            checkBoxF.MouseMove += SelectedMove;
            stackPanel.Children.Add(checkBoxF);

            CheckBox checkBoxPrime = new CheckBox();
            checkBoxPrime.Margin = new Thickness(5);
            checkBoxPrime.Content = "Prime";
            checkBoxPrime.IsChecked = db.Prime;
            checkBoxPrime.Checked += db.CheckPrime;
            stackPanel.Children.Add(checkBoxPrime);

            //invent and trade
            createButton("irrigation.png", db.ClickCheckInventory, stackPanel, db.Login);

            //kill
            createButton("cloud-lightning.png", db.ClickKill, stackPanel, db.Login);

            PanelForStart.Children.Add(stackPanel);
        }

        Settings.CalculateWindowPositions(_users);
        _users.Clear();
    }
}