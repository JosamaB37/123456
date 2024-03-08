﻿using gamevault.Models;
using gamevault.ViewModels;
using gamevault.Windows;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using System.Linq;
using gamevault.Helper;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading.Tasks;
using System.IO.Pipes;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace gamevault
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool ShowToastMessage = true;
        public static bool IsWindowsPackage = false;

        public static CommandOptions? CommandLineOptions { get; internal set; } = null;

        private NotifyIcon m_Icon;

        private GameTimeTracker m_gameTimeTracker;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);

#if DEBUG
            AppFilePath.InitDebugPaths();

            #region DirectoryCreation
            if (!Directory.Exists(AppFilePath.ImageCache))
            {
                Directory.CreateDirectory(AppFilePath.ImageCache);
            }
            if (!Directory.Exists(AppFilePath.ConfigDir))
            {
                Directory.CreateDirectory(AppFilePath.ConfigDir);
            }
            #endregion

            await CacheHelper.OptimizeCache();
#else
            try
            {
                UpdateWindow updateWindow = new UpdateWindow();
                updateWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                LogUnhandledException(ex);
                //m_StoreHelper.NoInternetException();              
            }
#endif

            #region DirectoryCreation
            if (!Directory.Exists(AppFilePath.ImageCache))
            {
                Directory.CreateDirectory(AppFilePath.ImageCache);
            }
            if (!Directory.Exists(AppFilePath.ConfigDir))
            {
                Directory.CreateDirectory(AppFilePath.ConfigDir);
            }
            #endregion

            await LoginManager.Instance.StartupLogin();
            m_gameTimeTracker = new GameTimeTracker();
            await m_gameTimeTracker.Start();

            bool startMinimized = false;

            if ((CommandLineOptions?.Minimized).HasValue)
                startMinimized = CommandLineOptions!.Minimized!.Value;
            else if (SettingsViewModel.Instance.BackgroundStart)
                startMinimized = true;

            if (!startMinimized && MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }

            InitNotifyIcon();

            // After the app is created and most things are instantiated, handle any special command line stuff
            if (PipeServiceHandler.Instance != null)
                await PipeServiceHandler.Instance.HandleCommand(App.CommandLineOptions);
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ProcessShepherd.KillAllChildProcesses();
#if DEBUG
            e.Handled = false;
#else
            LogUnhandledException(e.Exception);
#endif
        }

        public void LogUnhandledException(Exception e)
        {
            Application.Current.DispatcherUnhandledException -= new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
            string errorMessage = $"MESSAGE:\n{e.Message}\nINNER_EXCEPTION:{(e.InnerException != null ? "" + e.InnerException.Message : null)}";
            string errorStackTrace = $"STACK_TRACE:\n{(e.StackTrace != null ? "" + e.StackTrace : null)}";
            string errorLogPath = $"{AppFilePath.ErrorLog}\\GameVault_ErrorLog_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.txt";
            if (!File.Exists(errorLogPath))
            {
                Directory.CreateDirectory(AppFilePath.ErrorLog);
                File.Create(errorLogPath).Close();
            }
            File.WriteAllText(errorLogPath, errorMessage + "\n" + errorStackTrace);
            ExceptionWindow exWin = new ExceptionWindow();
            if (exWin.ShowDialog() == true)
            {
                errorMessage += $"\nUSER_MESSAGE:{exWin.UserMessage}";
                CrashReportHelper.SendCrashReport(errorMessage, errorStackTrace, $"Type: {e.GetType().ToString()}");
            }
            ShutdownApp();
        }

        private void InitNotifyIcon()
        {
            m_Icon = new NotifyIcon();
            m_Icon.MouseDoubleClick += NotifyIcon_DoubleClick;
            Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/gamevault;component/Resources/Images/icon.ico")).Stream;
            m_Icon.Icon = new System.Drawing.Icon(iconStream);
            m_Icon.ContextMenuStrip = new ContextMenuStrip();
            m_Icon.ContextMenuStrip.Items.Add("Show", null, NotifyIcon_DoubleClick);
            m_Icon.ContextMenuStrip.Items.Add("Exit", null, NotifyIcon_Exit_Click);
            m_Icon.Visible = true;
        }

        private void NotifyIcon_DoubleClick(Object sender, EventArgs e)
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            else if (MainWindow.IsVisible == false)
            {
                MainWindow.Show();
            }
            else if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }
        }

        private async void NotifyIcon_Exit_Click(Object sender, EventArgs e)
        {
            if ((DownloadsViewModel.Instance.DownloadedGames.Where(g => g.IsDownloading() == true)).Count() > 0)
            {
                if (MainWindow.IsVisible == false)
                {
                    MainWindow.Show();
                }
                MessageDialogResult result = await ((MetroWindow)MainWindow).ShowMessageAsync($"Downloads are still running in the background, are you sure you want to close the app anyway?", "", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });
                if (result == MessageDialogResult.Affirmative)
                {
                    MainWindowViewModel.Instance.Downloads.CancelAllDownloads();
                    ShutdownApp();
                }
            }
            else
            {
                ShutdownApp();
            }
        }

        private void ShutdownApp()
        {
            ShowToastMessage = false;
            ProcessShepherd.KillAllChildProcesses();
            if (m_Icon != null)
            {
                m_Icon.Icon.Dispose();
                m_Icon.Dispose();
            }
            Shutdown();
        }
    }
}
