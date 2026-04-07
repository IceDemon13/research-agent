using System;
using System.Windows.Media;
using DevExpress.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Data.Options;

namespace Telemart.Client
{
    public sealed class ApplicationSettings : BindableBase
    {
        private ApplicationSettings(AppOptions appOptions)
        {
            MainBackgroundColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(appOptions?.MainBackgroundColor)!);
            SecondBackgroundColorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(appOptions?.SecondBackgroundColor)!);
        }

        public static IServiceProvider ServiceProvider
        {
            set
            {
                AppOptions appOptions = value.GetService<AppOptions>();

                Default = new ApplicationSettings(appOptions);
            }
        }

        public static ApplicationSettings Default { get; private set; }

        public Brush MainBackgroundColorBrush { get; }

        public Brush SecondBackgroundColorBrush { get; }
    }
}