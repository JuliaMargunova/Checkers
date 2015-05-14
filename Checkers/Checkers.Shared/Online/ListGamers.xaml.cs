﻿using Checkers.Entities;
using Checkers.Helpers;
using Checkers.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Checkers
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ListGamers : Page
    {
        private GameService gameService = null;
        private DispatcherTimer UpdateStateTimer { get; set; }
        private int IdGame { get; set; }
        public ListGamers()
        {
            this.InitializeComponent();

#if WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif

            gameService = new GameService();
            PopulateOnlineGamers();
            UpdateStateTimer = new DispatcherTimer();
            UpdateStateTimer.Interval = TimeSpan.FromSeconds(5);
            UpdateStateTimer.Tick += async delegate
            {
                int myGamerId = SettingsHelper.GetCurrentGamerId();
                var response = await gameService.OnlineGameServiceCall("GET", String.Format("CheckInputGame/{0}", myGamerId));
                XDocument xml = XDocument.Parse(CleanXml(response));
                var result = xml.Root.Value;
                if (!result.Contains("00"))
                {
                    //прислать челу уведомление о входящей игре

                    ///////////////
                    CreateGame();//создаем для нас новую игру, чтобы остальные могли к нам подключиться
                    string idGame = xml.Descendants("CheckInputGameResult").FirstOrDefault().Element("IdGame").Value;
                    string idCurrentGamer = xml.Descendants("CheckInputGameResult").FirstOrDefault().Element("IdCurrentGamer").Value;
                    Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1}", idCurrentGamer, idGame));
                }              
            };
            UpdateStateTimer.Start();

        }

#if WINDOWS_PHONE_APP
        private void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            Frame frame = Window.Current.Content as Frame;
            if (frame == null)
            {
                return;
            }

            if (frame.CanGoBack)
            {
                frame.GoBack();
                e.Handled = true;
            }
        }
#endif

        private async void CreateGame()
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            var response = await gameService.OnlineGameServiceCall("GET", String.Format("CreateGame/{0}", idFirstGamer));
            XDocument xml = XDocument.Parse(response);
            var result = xml.Root.Value;
            var isCreated = result == "true" ? true : false;
            if (!isCreated)
            {

            }
        }

        private async void PopulateOnlineGamers(string userName = "0")
        {
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            int idFirstGamer = (int)roamingSettings.Values["idFirstGamer"];
            string xmlListGames = await gameService.OnlineGameServiceCall("GET", String.Format("GetGames/{0}/{1}/{2}", idFirstGamer.ToString(), userName, "0"));
            XDocument xml = XDocument.Parse(CleanXml(xmlListGames));
            var listGames = new List<CheckersGame>();
            foreach (var item in xml.Descendants("CheckersGame"))
            {
                try
                {
                    CheckersGame game = new CheckersGame();
                    game.IdGame = Convert.ToInt32(item.Element("IdGame").Value);
                    game.Login = item.Element("Login").Value;
                    game.Rating = Convert.ToInt32(item.Element("Rating").Value);
                    game.IdFirstGamer = Convert.ToInt32(item.Element("IdFirstGamer").Value);
                    listGames.Add(game);
                }
                catch (Exception)
                {

                }
            }
            ListOnlineGamers.ItemsSource = listGames;
        }

        private async void StartGame(CheckersGame game)
        {
            var random = new Random();
           // int currentValue = random.Next(1, 3);
            int currentValue = 1;
            IdGame = game.IdGame;
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            int idSecondGamer = (int)roamingSettings.Values["idFirstGamer"];
            var response = await gameService.OnlineGameServiceCall("GET", String.Format("StartGame/{0}/{1}/{2}", game.IdGame, idSecondGamer, currentValue == 1 ? idSecondGamer : game.IdFirstGamer));
            XDocument xml = XDocument.Parse(CleanXml(response));
            var result = xml.Descendants("StartGameResult").FirstOrDefault().Element("Success").Value;
            var isStarted = result == "true" ? true : false;
            if (!isStarted)
            {
                //TODO:
            }
            else
            {
                int idCurrentGamer = Convert.ToInt32(xml.Descendants("StartGameResult").FirstOrDefault().Element("IdCurrentGamer").Value);
                Frame.Navigate(typeof(OnlineGame), String.Format("{0},{1}", idCurrentGamer, game.IdGame));
            }
        }

        private void ListOnlineGamers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var game = (CheckersGame)e.AddedItems[0];
                StartGame(game);
            }
        }

        private void findLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopulateOnlineGamers(findLogin.Text.Length > 0 ? findLogin.Text : "0");
        }

        private string CleanXml(string xml)
        {
            return xml
                .Replace("a:", "")
                .Replace("xmlns:a=\"http://schemas.datacontract.org/2004/07/GameService.Entities\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"", "")
                .Replace("http://tempuri.org/", "");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}