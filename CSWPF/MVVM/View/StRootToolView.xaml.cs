using CSWPF.Helpers;
using NetFwTypeLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CSWPF.Core;

namespace CSWPF.MVVM.View
{
    public partial class StRootToolView : UserControl
    {
        public ObservableCollection<Route> RoutesCollection { get; set; } = new ObservableCollection<Route>();
        
        int rowCount = 0;
        bool firstLoad = true;
        readonly string networkconfigURL = @"https://api.steampowered.com/ISteamApps/GetSDRConfig/v1?appid=730";
        readonly string networkconfigURLBackup = @"https://raw.githubusercontent.com/SteamDatabase/SteamTracking/597d81b2a436d260a7ffe3b53eb3b9ed932c2efb/Random/NetworkDatagramConfig.json";

        private StackPanel stackPanel;
        private Label labelName;
        private Label labelPing;
        private CheckBox checkBoxBlocked;

        public StRootToolView()
        {
            InitializeComponent();

            DataContext = this;
            RoutesCollection = new ObservableCollection<Route>();
            Thread populateRoutesThread = new Thread(new ThreadStart(PopulateRoutes));
            populateRoutesThread.Start();
        }

        public async void Load()
        {
            foreach (var db in RoutesCollection)
            {
                stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Horizontal;

                labelName = new Label();
                labelName.Content = db.Desc;
                labelName.Width = 250;
                stackPanel.Children.Add(labelName);

                labelPing = new Label();
                labelPing.Width = 50;
                labelPing.Content = "zero";
                stackPanel.Children.Add(labelPing);

                checkBoxBlocked = new CheckBox(); 
                //checkBoxBlocked.IsChecked = db.AllCheck;
                stackPanel.Children.Add(checkBoxBlocked);

                PanelForStart.Children.Add(stackPanel);
            }

            PopulateRoute();
        }

        private void ButtonClearRules(object sender, RoutedEventArgs e)
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                if (rule.Name.Contains("SteamRouteTool-")) { fwPolicy2.Rules.Remove(rule.Name); }
            }
            ClearOrBlockAllCheckBoxes();
            MessageBox.Show("You have cleared all firewall rules created by this tool.", "Steam Route Tool - Rules Clear");
        }

        private void ClearOrBlockAllCheckBoxes()
        {
            foreach (CheckBox checkBox in Settings.FindVisualChildren<CheckBox>(PanelForStart))
            {
                if (checkBox.IsChecked == true)
                {
                    checkBox.IsChecked = false;
                }
            }
        }

        private void BTBlockAll(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox checkBox in Settings.FindVisualChildren<CheckBox>(PanelForStart))
            {
                if (checkBox.IsChecked == false)
                {
                    checkBox.IsChecked = true;
                }
            }

            List<Route> selectedRoutes = new List<Route>();

            foreach (Route route in RoutesCollection)
            {
                foreach (StackPanel stackPanel in PanelForStart.Children)
                {
                    CheckBox checkBox = stackPanel.Children[2] as CheckBox;
                    if (checkBox != null && checkBox.IsChecked == true)
                    {
                        selectedRoutes.Add(route);
                        break;
                    }
                }
            }

            if (selectedRoutes.Count > 0)
            {
                foreach (Route route in selectedRoutes)
                {
                    SetRule(route);
                }

            }
        }


        private void PopulateRoutes()
        {
            string raw;
            try
            {
                raw = new WebClient().DownloadString(networkconfigURL);
            }
            catch
            {
                try
                {
                    raw = new WebClient().DownloadString(networkconfigURLBackup);
                }
                catch
                {
                    return;
                }
            }

            JObject jObj = JsonConvert.DeserializeObject<JObject>(raw);

            foreach (KeyValuePair<string, JToken> rc in (JObject)jObj["pops"])
            {
                if (rc.Value.ToString().Contains("relays") && !rc.Value.ToString().Contains("cloud-test"))
                {
                    Route route = new Route();
                    route.Name = rc.Key;
                    if (rc.Value.ToString().Contains("\"desc\"")) { route.Desc = rc.Value["desc"].ToString(); }
                    route.Ranges = new Dictionary<string, string>();
                    route.RowIndex = new List<int>();
                    foreach (JToken range in rc.Value["relays"])
                    {
                        route.Ranges.Add(range["ipv4"].ToString(), range["port_range"][0].ToString() + "-" + range["port_range"][1].ToString());
                        route.RowIndex.Add(rowCount);
                        rowCount++;
                    }
                    if (rc.Value.ToString().Contains("partners\": 2")) { route.PW = true; }
                    else { route.PW = false; }
                    route.Extended = false;
                    route.AllCheck = false;
                    
                    Dispatcher.Invoke(() => RoutesCollection.Add(route));
                }
            }

            if (Dispatcher.CheckAccess()) { Load(); }
            else { Dispatcher.Invoke(() => Load()); }
        }

        private async void PopulateRoute()
        {
            int rowIndex = 0;

            if (PanelForStart.Children.Count == 0)
            {
                foreach (Route route in RoutesCollection)
                {
                    for (int i = 0; i < route.Ranges.Count; i++)
                    {
                        StackPanel stackPanel = new StackPanel();
                        stackPanel.Orientation = Orientation.Horizontal;

                        Label label = new Label();

                        if (route.Desc != null)
                        {
                            label.Content = route.Desc + " " + (i + 1);
                        }
                        else
                        {
                            label.Content = route.Name + " " + (i + 1);
                        }

                        stackPanel.Children.Add(label);

                        CheckBox checkBox = new CheckBox();
                        checkBox.IsChecked = false;
                        stackPanel.Children.Add(checkBox);

                        PanelForStart.Children.Add(stackPanel);

                        rowIndex++;
                    }
                }
            }

            rowIndex = 0;

            foreach (Route route in RoutesCollection)
            {
                for (int i = 0; i < route.Ranges.Count; i++)
                {
                    if (rowIndex < PanelForStart.Children.Count)
                    {
                        StackPanel stackPanel = (StackPanel)PanelForStart.Children[rowIndex];
                        Label label = (Label)stackPanel.Children[0];

                        if (route.Desc != null)
                        {
                            label.Content = route.Desc + " " + (i + 1);
                        }
                        else
                        {
                            label.Content = route.Name + " " + (i + 1);
                        }

                        if (!route.Extended)
                        {
                            if (route.Desc != null)
                            {
                                label.Content = route.Desc;
                            }
                            else
                            {
                                label.Content = route.Name;
                            }
                        }

                        if (i > 0 && !route.Extended)
                        {
                            stackPanel.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            stackPanel.Visibility = Visibility.Visible;
                        }

                        rowIndex++;
                    }
                }
            }

            if (firstLoad)
            {
                PingRoutes();
                GetCurrentBlocked();
            }
            firstLoad = false;
        }

        private void PingRoutes()
        {
            foreach (Route route in RoutesCollection)
            {
                Thread thread = new Thread(() =>
                {
                    for (int i = 0; i < route.Ranges.Count; i++)
                    {
                        string responseTime = PingHost(route.Ranges.Keys.ToArray()[i]);

                        this.Dispatcher.Invoke(() =>
                        {
                            if (route.RowIndex[i] < PanelForStart.Children.Count)
                            {
                                StackPanel stackPanel = (StackPanel)PanelForStart.Children[route.RowIndex[i]];
                                Label labelPing = (Label)stackPanel.Children[1];

                                if (responseTime != "-1")
                                {
                                    int pingValue = Convert.ToInt32(responseTime);

                                    if (pingValue <= 50)
                                    {
                                        labelPing.Background = new SolidColorBrush(Colors.White);
                                        labelPing.Foreground = new SolidColorBrush(Colors.Green);
                                    }
                                    else if (pingValue > 50 && pingValue <= 100)
                                    {
                                        labelPing.Background = new SolidColorBrush(Colors.White);
                                        labelPing.Foreground = new SolidColorBrush(Colors.Orange);
                                    }
                                    else
                                    {
                                        labelPing.Background = new SolidColorBrush(Colors.Black);
                                        labelPing.Foreground = new SolidColorBrush(Colors.Red);
                                    }
                                }
                                else
                                {
                                    labelPing.Background = new SolidColorBrush(Colors.White);
                                    labelPing.Foreground = new SolidColorBrush(Colors.DarkRed);
                                }

                                labelPing.Content = responseTime;
                            }
                        });

                    }
                });

                thread.Start();
            }
        }


        private void GetCurrentBlocked()
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);

            foreach (INetFwRule rule in fwPolicy2.Rules)
            {
                if (rule.Name.Contains("SteamRouteTool-"))
                {
                    string name = rule.Name.Split('-')[1];

                    List<string> addr = new List<string>();

                    foreach (string tosplit in rule.RemoteAddresses.Split(',')) { addr.Add(tosplit.Split('/')[0]); }
                    foreach (Route route in RoutesCollection)
                    {
                        if (route.Name == name)
                        {
                            bool extended = true;
                            bool firstBlocked = false;
                            int blockedCount = 0;
                            for (int i = 0; i < route.Ranges.Count; i++)
                            {
                                if (addr.Contains(route.Ranges.Keys.ToArray()[i]))
                                {
                                    this.Dispatcher.Invoke(() =>
                                    {
                                        checkBoxBlocked.IsChecked = true;
                                    });

                                    if (i != 0) { blockedCount++; }
                                    if (i == 0) { firstBlocked = true; }
                                }
                            }
                            if (blockedCount == route.Ranges.Count - 1 && firstBlocked)
                            {
                                extended = false;
                            }
                            route.Extended = extended;
                            if (extended)
                            {
                                foreach (int index in route.RowIndex)
                                {
                                    StackPanel stackPanel = (StackPanel)PanelForStart.Children[index];
                                    stackPanel.Visibility = Visibility.Visible;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string PingHost(string host)
        {
            try
            {
                Ping ping = new Ping();
                PingReply pingreply = ping.Send(host);
                if (pingreply.RoundtripTime == 0) { return "-1"; }
                else { return pingreply.RoundtripTime.ToString(); }
            }
            catch { return "-1"; }
        }

        private async void PingSingleRoute(Route route)
        {
            foreach (var row_index in route.RowIndex)
            {
                StackPanel stackPanel = (StackPanel)PanelForStart.Children[row_index];
                string responseTime = PingHost(route.Ranges.Keys.ToArray()[row_index]);

                Label labelPing = (Label)stackPanel.Children[1];

                labelPing.Content = responseTime;

                if (responseTime != "-1")
                {
                    int pingValue = Convert.ToInt32(responseTime);
                    if (pingValue <= 50)
                    {
                        labelPing.Background = new SolidColorBrush(Colors.White);
                        labelPing.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else if (pingValue <= 100)
                    {
                        labelPing.Background = new SolidColorBrush(Colors.White);
                        labelPing.Foreground = new SolidColorBrush(Colors.Orange);
                    }
                    else
                    {
                        labelPing.Background = new SolidColorBrush(Colors.Black);
                        labelPing.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
                else
                {
                    labelPing.Background = new SolidColorBrush(Colors.White);
                    labelPing.Foreground = new SolidColorBrush(Colors.DarkRed);
                }
            }
        }

        private void SetRule(Route route)
        {
            Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            INetFwPolicy2 fwPolicy2 = (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            try
            {
                fwPolicy2.Rules.Remove("SteamRouteTool-" + route.Name);
            }catch { }

            INetFwRule fwRule = (INetFwRule2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FWRule"));

            fwRule.Enabled = true;
            fwRule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            fwRule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;

            string remoteAddresses = "";
            int maxp = 0;
            int minp = 99999;

            foreach (Route selectedRoute in RoutesCollection)
            {
                if (selectedRoute.AllCheck || selectedRoute.RowIndex.Any(index => route.RowIndex.Contains(index)))
                {
                    foreach (KeyValuePair<string, string> range in selectedRoute.Ranges)
                    {
                        remoteAddresses += range.Key + ",";
                        int rangeMinPort = Convert.ToInt32(range.Value.Split('-')[0]);
                        int rangeMaxPort = Convert.ToInt32(range.Value.Split('-')[1]);
                        if (maxp < rangeMaxPort)
                            maxp = rangeMaxPort;
                        if (minp > rangeMinPort)
                            minp = rangeMinPort;
                    }
                }
            }
            
            if (remoteAddresses != "")
            {
                remoteAddresses = remoteAddresses.Substring(0, remoteAddresses.Length - 1);
                fwRule.RemoteAddresses = remoteAddresses;
                fwRule.Protocol = 17;
                fwRule.RemotePorts = minp + "-" + maxp;
                fwRule.Name = "SteamRouteTool-" + route.Name;

                INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
                firewallPolicy.Rules.Add(fwRule);
            }
        }
    }
}
