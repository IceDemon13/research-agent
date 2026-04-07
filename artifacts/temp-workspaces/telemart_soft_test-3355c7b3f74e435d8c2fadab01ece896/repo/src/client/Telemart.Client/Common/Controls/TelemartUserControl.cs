using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Telemart.Client.Core;

namespace Telemart.Client.Common.Controls
{
    public class TelemartUserControl : UserControl
    {
        private readonly string dirPath;
        private readonly string filePath;
        private Window window;

        public TelemartUserControl()
        {
            dirPath = Path.Combine(ApplicationFolders.LocalApplicationData, "settings");
            filePath = Path.Combine(dirPath, $"{GetType().Name}_dialog_settings.json");

            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;
        }

        private void ApplySettingsToControl()
        {
            if (File.Exists(filePath))
            {
                DialogSettingsInfo settings = ReadDialogSettingsInfo();

                window.Width = settings.Width;
                window.Height = settings.Height;
                window.Top = settings.Top >= SystemParameters.PrimaryScreenHeight - 1 || settings.Top < 0
                    ? GetMiddleOffset(SystemParameters.PrimaryScreenHeight, window.ActualHeight)
                    : settings.Top;
                window.Left = settings.Left >= SystemParameters.PrimaryScreenWidth - 1 || settings.Left < 0
                    ? GetMiddleOffset(SystemParameters.PrimaryScreenWidth, window.ActualWidth)
                    : settings.Left;

                if (settings.WindowState == WindowState.Maximized)
                {
                    window.WindowState = settings.WindowState;
                }

                static double GetMiddleOffset(double offset, double size)
                {
                    return (offset - size) / 2d;
                }
            }
        }

        private DialogSettingsInfo ReadDialogSettingsInfo()
        {
            DialogSettingsInfo settings;

            using FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader streamReader = new StreamReader(stream);
            using JsonReader jsonReader = new JsonTextReader(streamReader);

            JsonSerializer serializer = new JsonSerializer();

            settings = serializer.Deserialize<DialogSettingsInfo>(jsonReader);

            return settings;
        }

        private void WriteDialogSettingsInfo()
        {
            DialogSettingsInfo settings = new DialogSettingsInfo(
                window.ActualWidth,
                window.ActualHeight,
                window.Top,
                window.Left,
                window.WindowState);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            using MemoryStream stream = new MemoryStream();
            using StreamWriter streamWriter = new StreamWriter(stream);
            using JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter);

            JsonSerializer serializer = new JsonSerializer();

            serializer.Serialize(jsonWriter, settings);

            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Flush();

            File.WriteAllBytes(filePath, stream.ToArray());
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            window = Parent as Window;

            if (window != null)
            {
                ApplySettingsToControl();
                window.Closing += OnClosing;
            }
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            if (window != null)
            {
                window.Closing -= OnClosing;
                WriteDialogSettingsInfo();
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            window.Owner?.Activate();
        }

        private class DialogSettingsInfo
        {
            public DialogSettingsInfo(double width, double height, double top, double left, WindowState windowState)
            {
                Width = width;
                Height = height;
                Top = top;
                Left = left;
                WindowState = windowState;
            }

            public DialogSettingsInfo()
            {
            }

            [JsonProperty("Width")]
            public double Width { get; set; }

            [JsonProperty("Height")]
            public double Height { get; set; }

            [JsonProperty("Top")]
            public double Top { get; set; }

            [JsonProperty("Left")]
            public double Left { get; set; }

            [JsonProperty("WindowState")]
            public WindowState WindowState { get; set; }
        }
    }
}