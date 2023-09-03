using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CSWPF.Directory;
using CSWPF.Helpers;
using CSWPF.Utils;
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
        
        private void createButton(string iconsName , RoutedEventHandler click, StackPanel stackPanel, string login)
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
                PanelForLogin.Children.Add(login);

                //password
                createButton("screwdriver.png", db.ClickPassword, PanelForPassword, db.Login);

                //start
                createButton("cow.png", db.ClickStart, stackPanel, db.Login);
                
                //kill
                createButton("cloud-lightning.png", db.ClickKill, PanelForKill, db.Login);

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

                PanelForStart.Children.Add(stackPanel);
            }
            
            _users.Clear();
        }

        public void SelectedMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                CheckBox checkBox = sender as CheckBox;
                checkBox.IsChecked = !checkBox.IsChecked;
            }
        }

        

        public SteamGuardAccount account;
        private async void AddSDA()
        {
            string username = LoginTextBox.Text;
            string password = PasswordTextBox.Text;

            SteamClient steamClient = new SteamClient();
            steamClient.Connect();

            while (!steamClient.IsConnected)
            {
                await Task.Delay(500);
            }
            
            CredentialsAuthSession authSession;
            try
            {
                authSession = await steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(
                    new AuthSessionDetails
                    {
                        Username = username,
                        Password = password,
                        IsPersistentSession = false,
                        PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_MobileApp,
                        ClientOSType = EOSType.Android9,
                        Authenticator = new UserFormAuthenticator(this.account),
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Steam Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }
            
            AuthPollResult pollResponse;
            try
            {
                pollResponse = await authSession.PollingWaitForResultAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Steam Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            SessionData sessionData = new SessionData()
            {
                SteamID = authSession.SteamID.ConvertToUInt64(),
            };

            MessageBox.Show(sessionData.ToString());
            
            var result = MessageBox.Show("Steam account login succeeded. Press OK to continue adding SDA as your authenticator.", "Steam Login", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                MessageBox.Show("Adding authenticator aborted.", "Steam Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoginTextBox.IsEnabled = true;
                LoginTextBox.Text = "Login";
                return;
            }
            
            AuthenticatorLinker linker = new AuthenticatorLinker(sessionData);
            AuthenticatorLinker.LinkResult linkResponse = AuthenticatorLinker.LinkResult.GeneralFailure;
            
            while (linkResponse != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                try
                {
                    linkResponse = linker.AddAuthenticator();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error adding your authenticator: " + ex.Message, "Steam Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LoginTextBox.IsEnabled = true;
                    LoginTextBox.Text = "Login";
                    return;
                }
            }
            
            Manifest manifest = Manifest.GetManifest();
            string passKey = null;
            if (manifest.Entries.Count == 0)
            {
                passKey = manifest.PromptSetupPassKey("Please enter an encryption passkey. Leave blank or hit cancel to not encrypt (VERY INSECURE).");
            }
            else if (manifest.Entries.Count > 0 && manifest.Encrypted)
            {
                bool passKeyValid = false;
                while (!passKeyValid)
                {
                    InputForm passKeyForm = new InputForm("Please enter your current encryption passkey.");
                    passKeyForm.ShowDialog();
                    if (!passKeyForm.Canceled)
                    {
                        passKey = passKeyForm.txtBox.Text;
                        passKeyValid = manifest.VerifyPasskey(passKey);
                        if (!passKeyValid)
                        {
                            MessageBox.Show("That passkey is invalid. Please enter the same passkey you used for your other accounts.");
                        }
                    }
                    else
                    {
                        this.Close();
                        return;
                    }
                }
            }
            
            //Save the file immediately; losing this would be bad.
            if (!manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey))
            {
                manifest.RemoveAccount(linker.LinkedAccount);
                MessageBox.Show("Unable to save mobile authenticator file. The mobile authenticator has not been linked.");
                this.Close();
                return;
            }

            MessageBox.Show("The Mobile Authenticator has not yet been linked. Before finalizing the authenticator, please write down your revocation code: " + linker.LinkedAccount.RevocationCode);

            AuthenticatorLinker.FinalizeResult finalizeResponse = AuthenticatorLinker.FinalizeResult.GeneralFailure;
            while (finalizeResponse != AuthenticatorLinker.FinalizeResult.Success)
            {
                InputForm smsCodeForm = new InputForm("Please input the SMS code sent to your phone.");
                smsCodeForm.ShowDialog();
                if (smsCodeForm.Canceled)
                {
                    manifest.RemoveAccount(linker.LinkedAccount);
                    this.Close();
                    return;
                }

                InputForm confirmRevocationCode = new InputForm("Please enter your revocation code to ensure you've saved it.");
                confirmRevocationCode.ShowDialog();
                if (confirmRevocationCode.txtBox.Text.ToUpper() != linker.LinkedAccount.RevocationCode)
                {
                    MessageBox.Show("Revocation code incorrect; the authenticator has not been linked.");
                    manifest.RemoveAccount(linker.LinkedAccount);
                    this.Close();
                    return;
                }

                string smsCode = smsCodeForm.txtBox.Text;
                finalizeResponse = linker.FinalizeAddAuthenticator(smsCode);

                switch (finalizeResponse)
                {
                    case AuthenticatorLinker.FinalizeResult.BadSMSCode:
                        continue;

                    case AuthenticatorLinker.FinalizeResult.UnableToGenerateCorrectCodes:
                        MessageBox.Show("Unable to generate the proper codes to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                        manifest.RemoveAccount(linker.LinkedAccount);
                        this.Close();
                        return;

                    case AuthenticatorLinker.FinalizeResult.GeneralFailure:
                        MessageBox.Show("Unable to finalize this authenticator. The authenticator should not have been linked. In the off-chance it was, please write down your revocation code, as this is the last chance to see it: " + linker.LinkedAccount.RevocationCode);
                        manifest.RemoveAccount(linker.LinkedAccount);
                        this.Close();
                        return;
                }
            }

            //Linked, finally. Re-save with FullyEnrolled property.
            manifest.SaveAccount(linker.LinkedAccount, passKey != null, passKey);
            MessageBox.Show("Mobile authenticator successfully linked. Please write down your revocation code: " + linker.LinkedAccount.RevocationCode);
            //Add new user
            User newUser = new User(LoginTextBox.Text, PasswordTextBox.Text);
            SaveNew(newUser,linker.LinkedAccount.IdentitySecret ,linker.LinkedAccount.SharedSecret);
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

            foreach (CheckBox checkBox in FindVisualChildren<CheckBox>(PanelForStart))
            {
                if (checkBox.IsChecked == true && checkBox.Tag is User user)
                {
                    _users.Add(user);
                }
            }
            //ads
            StartLive(_users);
        }

        private void KillClick(object sender, RoutedEventArgs e)
        {
            AccountHelper.KillAll();
        }

        private void AddBtClick(object sender, RoutedEventArgs e)
        {
            if (Msg.ShowQuestion("Вы действительно хотите добавить?"))
            {
                User newUser = new User(LoginTextBox.Text, PasswordTextBox.Text);
                LoginTextBox.Clear();
                PasswordTextBox.Clear();
                
                SaveToDB(newUser);
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

        private void TBSteamPathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fm = new OpenFileDialog();
            fm.ShowDialog();
            Msg.ShowInfo(Path.GetFullPath(fm.FileName));
        }
    }
}