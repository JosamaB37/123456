﻿using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for NewGameViewUserControl.xaml
    /// </summary>
    public partial class GameViewUserControl : UserControl
    {
        private GameViewViewModel ViewModel { get; set; }
        private int gameID { get; set; }
        private bool loaded = false;
        public GameViewUserControl(Game game, bool reloadGameObject = true)
        {
            InitializeComponent();
            ViewModel = new GameViewViewModel();
            if (false == reloadGameObject)
            {
                ViewModel.Game = game;
            }
            gameID = game.ID;
            this.DataContext = ViewModel;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Focus();
            if (!loaded)
            {
                loaded = true;
                if (ViewModel.Game == null)
                {
                    try
                    {
                        ViewModel.Game = await Task<Game>.Run(() =>
                        {
                            string gameList = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/games/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Game>(gameList);
                        });
                        ViewModel.Progress = await Task<Progress>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}");
                            return System.Text.Json.JsonSerializer.Deserialize<Progress>(result);
                        });

                        ViewModel.UserProgress = await Task<Progress[]>.Run(() =>
                        {
                            string result = WebHelper.GetRequest(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/game/{gameID}");
                            Progress[] progresses = System.Text.Json.JsonSerializer.Deserialize<Progress[]>(result);
                            if (LoginManager.Instance.IsLoggedIn())
                            {
                                progresses = progresses.Where(p => p.User.ID != LoginManager.Instance.GetCurrentUser().ID).ToArray();
                            }
                            return progresses;
                        });
                    }
                    catch (Exception ex) { }
                }
                ViewModel.IsInstalled = IsGameInstalled(ViewModel.Game);
                ViewModel.IsDownloaded = IsGameDownloaded(ViewModel.Game);
                ViewModel.ShowRawgTitle = Preferences.Get(AppConfigKey.ShowRawgTitle, AppFilePath.UserFile) == "1";
            }
        }
        private bool IsGameInstalled(Game? game)
        {
            if (game == null)
                return false;
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == game.ID).FirstOrDefault();
            if (result.Equals(default(KeyValuePair<Game, string>)))
                return false;

            return true;
        }
        private bool IsGameDownloaded(Game? game)
        {
            if (game == null)
                return false;
            return DownloadsViewModel.Instance.DownloadedGames.Where(gameUC => gameUC.GetGameId() == game.ID).Count() > 0;
        }
        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }
        private void GamePlay_Click(object sender, MouseButtonEventArgs e)
        {
            GamePlay();
        }

        public void GamePlay()
        {
            string path = "";
            KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ViewModel.Game.ID).FirstOrDefault();
            if (!result.Equals(default(KeyValuePair<Game, string>)))
            {
                path = result.Value;
            }
            if (!Directory.Exists(path))
            {
                MainWindowViewModel.Instance.AppBarText = $"Can not find part of '{path}'";
                return;
            }
            string savedExecutable = Preferences.Get(AppConfigKey.Executable, $"{path}\\gamevault-exec");
            string parameter = Preferences.Get(AppConfigKey.LaunchParameter, $"{path}\\gamevault-exec");
            if (savedExecutable == string.Empty)
            {
                if (GameSettingsUserControl.TryPrepareLaunchExecutable(path))
                {
                    savedExecutable = Preferences.Get(AppConfigKey.Executable, $"{path}\\gamevault-exec");
                }
                else
                {
                    MainWindowViewModel.Instance.AppBarText = $"No valid Executable found";
                    return;
                }
            }
            if (File.Exists(savedExecutable))
            {
                try
                {
                    ProcessHelper.StartApp(savedExecutable, parameter);
                }
                catch
                {

                    try
                    {
                        ProcessHelper.StartApp(savedExecutable, parameter, true);
                    }
                    catch
                    {
                        MainWindowViewModel.Instance.AppBarText = $"Can not execute '{savedExecutable}'";
                    }
                }
                MainWindowViewModel.Instance.Library.GetGameInstalls().SetLastPlayedGame(result.Key.ID);
                //Preferences.Set(AppConfigKey.LastPlayed, DateTime.Now.ToString(), $"{path}\\gamevault-exec");
            }
            else
            {
                MainWindowViewModel.Instance.AppBarText = $"Could not find Executable '{savedExecutable}'";
            }
        }

        private void GameSettings_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Game == null)
                return;
            MainWindowViewModel.Instance.OpenPopup(new GameSettingsUserControl(ViewModel.Game) { Width = 1200, Height = 800, Margin = new Thickness(50) });
        }
        private async void GameDownload_Click(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel.Game == null)
                return;
            await MainWindowViewModel.Instance.Downloads.TryStartDownload(ViewModel.Game);
        }
        private void KeyBindingEscape_OnExecuted(object sender, object e)
        {
            MainWindowViewModel.Instance.UndoActiveControl();
        }

        private void Website_Navigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                string url = e.Uri.OriginalString;
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { }
        }

        private async void GameState_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0 || !LoginManager.Instance.IsLoggedIn())
                return;

            if (e.AddedItems.Count > 0)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        WebHelper.Put(@$"{SettingsViewModel.Instance.ServerUrl}/api/progresses/user/{LoginManager.Instance.GetCurrentUser().ID}/game/{gameID}", System.Text.Json.JsonSerializer.Serialize(new Progress() { State = ViewModel.Progress.State }));
                    }
                    catch (Exception ex)
                    {
                        string msg = WebExceptionHelper.TryGetServerMessage(ex);
                        MainWindowViewModel.Instance.AppBarText = msg;
                    }
                });
            }
        }

        private void ShowProgressUser_Click(object sender, MouseButtonEventArgs e)
        {
            Progress selectedProgress = ((FrameworkElement)sender).DataContext as Progress;
            if (selectedProgress != null)
            {
                MainWindowViewModel.Instance.Community.ShowUser(selectedProgress.User);
            }
        }

        public void RefreshGame(Game game)
        {
            ViewModel.Game = game;
        }

        private void GameTitle_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ViewModel.ShowRawgTitle = !ViewModel.ShowRawgTitle;
                Preferences.Set(AppConfigKey.ShowRawgTitle, ViewModel.ShowRawgTitle ? "1" : "0", AppFilePath.UserFile);
            }
            catch { }
        }
    }
}
