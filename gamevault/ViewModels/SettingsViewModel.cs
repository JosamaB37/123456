﻿using gamevault.Helper;
using gamevault.Models;
using System;
using System.IO;

namespace gamevault.ViewModels
{
    internal class SettingsViewModel : ViewModelBase
    {
        #region Singleton
        private static SettingsViewModel instance = null;
        private static readonly object padlock = new object();

        public static SettingsViewModel Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new SettingsViewModel();
                    }
                    return instance;
                }
            }
        }
        #endregion
        #region PrivateMembers       
        private string m_UserName { get; set; }
        private string m_RootPath { get; set; }
        private bool m_IsOnIdle = true;
        private bool m_BackgroundStart { get; set; }
        private bool m_LibStartup { get; set; }
        private bool m_AutoExtract { get; set; }
        private string m_ServerUrl { get; set; }
        private float m_ImageCacheSize { get; set; }
        private float m_OfflineCacheSize { get; set; }
        private long m_DownloadLimit { get; set; }
        private long m_DownloadLimitUIValue { get; set; }
        private User m_RegistrationUser = new User() { ProfilePicture = new Image(), BackgroundImage = new Image() };
        #endregion

        public SettingsViewModel()
        {
            UserName = Preferences.Get(AppConfigKey.Username, AppFilePath.UserFile);
            RootPath = Preferences.Get(AppConfigKey.RootPath, AppFilePath.UserFile);
            ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, AppFilePath.UserFile, true);
            m_BackgroundStart = (Preferences.Get(AppConfigKey.BackgroundStart, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(BackgroundStart));
            m_AutoExtract = (Preferences.Get(AppConfigKey.AutoExtract, AppFilePath.UserFile) == "1"); OnPropertyChanged(nameof(AutoExtract));

            string libstartup = Preferences.Get(AppConfigKey.LibStartup, AppFilePath.UserFile);
            if (libstartup == string.Empty)
            {
                LibStartup = true;
            }
            else
            {
                m_LibStartup = (libstartup == "1"); OnPropertyChanged(nameof(LibStartup));
            }
            if (long.TryParse(Preferences.Get(AppConfigKey.DownloadLimit, AppFilePath.UserFile), out long downloadLimitResult))
            {
                DownloadLimit = downloadLimitResult;
                DownloadLimitUIValue = DownloadLimit;
            }
            else
            {
                DownloadLimit = 0;
                DownloadLimitUIValue = 0;
            }
        }

        public string UserName
        {
            get { return m_UserName; }
            set { m_UserName = value; OnPropertyChanged(); }
        }
        public string RootPath
        {
            get { return m_RootPath; }
            set { m_RootPath = value; OnPropertyChanged(); }
        }
        public bool IsOnIdle
        {
            get { return m_IsOnIdle; }
            set { m_IsOnIdle = value; OnPropertyChanged(); }
        }
        public bool BackgroundStart
        {
            get { return m_BackgroundStart; }
            set
            {
                m_BackgroundStart = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_BackgroundStart)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.BackgroundStart, stringValue, AppFilePath.UserFile);
            }
        }
        public bool LibStartup
        {
            get { return m_LibStartup; }
            set
            {
                m_LibStartup = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_LibStartup)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.LibStartup, stringValue, AppFilePath.UserFile);
            }
        }
        public bool AutoExtract
        {
            get { return m_AutoExtract; }
            set
            {
                m_AutoExtract = value;
                OnPropertyChanged();
                string stringValue = "1";
                if (!m_AutoExtract)
                {
                    stringValue = "0";
                }
                Preferences.Set(AppConfigKey.AutoExtract, stringValue, AppFilePath.UserFile);
            }
        }
        public string ServerUrl
        {
            get { return m_ServerUrl; }
            set { m_ServerUrl = value; OnPropertyChanged(); }
        }
        public float ImageCacheSize
        {
            get { return m_ImageCacheSize; }
            set { m_ImageCacheSize = value; OnPropertyChanged(); }
        }
        public float OfflineCacheSize
        {
            get { return m_OfflineCacheSize; }
            set { m_OfflineCacheSize = value; OnPropertyChanged(); }
        }
        public long DownloadLimit
        {
            get { return m_DownloadLimit; }
            set { m_DownloadLimit = value; OnPropertyChanged(); }
        }
        public long DownloadLimitUIValue
        {
            get { return m_DownloadLimitUIValue; }
            set { m_DownloadLimitUIValue = value; OnPropertyChanged(); }
        }
        public User RegistrationUser
        {
            get { return m_RegistrationUser; }
            set { m_RegistrationUser = value; OnPropertyChanged(); }
        }
        public System.Windows.Forms.DialogResult SelectDownloadPath()
        {
            string DefaultDrive;
            if (Preferences.Get(AppConfigKey.InstallDrive, AppFilePath.UserFile) != "")
            {
                DefaultDrive = Preferences.Get(AppConfigKey.InstallDrive, AppFilePath.UserFile);
                Preferences.Set(AppConfigKey.RootPath, DefaultDrive + @":\NeoGameLibrary\", AppFilePath.UserFile);
            } else
            {
                DefaultDrive = "C";
                Preferences.Set(AppConfigKey.InstallDrive, "C", AppFilePath.UserFile);
                Preferences.Set(AppConfigKey.RootPath, DefaultDrive + @":\NeoGameLibrary\", AppFilePath.UserFile);
            }
            return System.Windows.Forms.DialogResult.OK;
        }
        public bool SetupCompleted()
        {
            return !((m_RootPath == string.Empty) || (m_ServerUrl == string.Empty) || (m_UserName == string.Empty));
        }
        public string Version
        {
            get
            {
                return "1.7.3";
            }
        }

    }
}
