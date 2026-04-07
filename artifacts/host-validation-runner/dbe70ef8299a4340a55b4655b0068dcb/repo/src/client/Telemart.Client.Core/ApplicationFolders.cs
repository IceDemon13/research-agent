using System;
using System.IO;

namespace Telemart.Client.Core
{
    public static class ApplicationFolders
    {
        private static readonly string PrintedPath;

        static ApplicationFolders()
        {
            const string AppName = "telemart.client";
            const string AppNameFolder = "TelemartClient";

            LocalApplicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppName);

            LocalApplication = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppNameFolder,
                $"{AppName}.exe");

            CommonApplicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                AppName);

            ApplicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppName);

            PrintedPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "documents",
                AppName,
                "printed");
        }

        public static string PrintedFolderPath => Path.Combine(PrintedPath, $"{DateTime.Today:dd.MM.yyyy}");

        public static string LocalApplicationData { get; }

        public static string LocalApplication { get; }

        public static string ApplicationData { get; }

        public static string CommonApplicationData { get; }
    }
}