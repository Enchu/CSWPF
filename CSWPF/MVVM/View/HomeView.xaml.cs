using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    private Dictionary<string, Action<User, StackPanel>> propertyUpdateActions = new Dictionary<string, Action<User, StackPanel>>();
    private static List<User> _users = new();
    public HomeView()
    {
        InitializeComponent();
        Load();
    }
    
    private void Load()
    {
        InitializePropertyUpdateActions();
        
        Check();
        CreateChildren();
    }
    
    private void InitializePropertyUpdateActions()
    {
        propertyUpdateActions["Login"] = UpdateLoginButton;
        propertyUpdateActions["Password"] = UpdatePasswordButton;
        propertyUpdateActions["Start"] = UpdateStartButton;
        propertyUpdateActions["StartCS"] = UpdateStartCSButton;
        propertyUpdateActions["Final"] = UpdateFinalCheckBox;
        propertyUpdateActions["Prime"] = UpdatePrimeCheckBox;
        propertyUpdateActions["IventAndTrade"] = UpdateIventAndTradeButton;
        propertyUpdateActions["Kill"] = UpdateKillButton;
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
        image.Source = new BitmapImage(new Uri($"pack://application:,,,/Icons/{iconsName}"));
        button.Content = image;
        button.Style = this.FindResource("ImageButtonStyle") as Style;
        button.Click += click;
        
        button.Width = 50;
        button.Tag = login;
        button.Margin = new Thickness(0, 0, 5, 0);

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
            stackPanel.Margin = new Thickness(0);

            foreach (var property in propertyUpdateActions.Keys)
            {
                propertyUpdateActions[property](db, stackPanel);
            }
            
            //open Steam
            //createButton("steam.png", db.ClickOpenSteam, PanelForOpenSteam, db.Login);

            PanelForStart.Children.Add(stackPanel);
        }

        Settings.CalculateWindowPositions(_users);
        _users.Clear();
    }
    
    private void UpdateLoginButton(User user, StackPanel stackPanel)
    {
        createButton("login.png", user.ClickLogin, stackPanel, user.Login);
    }

    private void UpdatePasswordButton(User user, StackPanel stackPanel)
    {
        createButton("screwdriver.png", user.ClickPassword, stackPanel, user.Login);
    }
    
    private void UpdateStartButton(User user, StackPanel stackPanel)
    {
        createButton("cow.png", user.ClickStart, stackPanel, user.Login);
    }

    private void UpdateStartCSButton(User user, StackPanel stackPanel)
    {
        createButton("cs.png", user.CsClick, stackPanel, user.Login);
    }
    
    private void UpdateIventAndTradeButton(User user, StackPanel stackPanel)
    {
        createButton("cs.png", user.CsClick, stackPanel, user.Login);
    }
    
    private void UpdateKillButton(User user, StackPanel stackPanel)
    {
        createButton("cloud-lightning.png", user.ClickKill, stackPanel, user.Login);
    }

    private void UpdatePrimeCheckBox(User user, StackPanel stackPanel)
    {
        CheckBox checkBoxPrime = new CheckBox();
        checkBoxPrime.Margin = new Thickness(5);
        checkBoxPrime.Content = "Prime";
        checkBoxPrime.IsChecked = user.Prime;
        checkBoxPrime.Checked += user.CheckPrime;
        stackPanel.Children.Add(checkBoxPrime);
    }

    private void UpdateFinalCheckBox(User user, StackPanel stackPanel)
    {
        CheckBox checkBoxF = new CheckBox();
        checkBoxF.Margin = new Thickness(5);
        checkBoxF.Content = "F";
        checkBoxF.Tag = user;
        checkBoxF.MouseMove += SelectedMove;
        stackPanel.Children.Add(checkBoxF);
    }
}