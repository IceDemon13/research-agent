using System.Windows;
using System.Windows.Controls;
using Telemart.Client.ViewModels.Common;

namespace Telemart.Client.Views.Common
{
    public partial class RequisitesView : UserControl
    {
        public RequisitesView()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ModelProperty = DependencyProperty.Register(
            nameof(Model),
            typeof(RequisitesViewItem),
            typeof(RequisitesView),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsValidProperty = DependencyProperty.Register(nameof(IsValid), typeof(bool), typeof(RequisitesView), new PropertyMetadata(default(bool)));

        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            set { SetValue(IsValidProperty, value); }
        }

        public RequisitesViewItem Model
        {
            get => (RequisitesViewItem)GetValue(ModelProperty);
            set => SetValue(ModelProperty, value);
        }

        private void IsValidButton_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IsValid = e.NewValue is true || !RequisitesViewUserControl.IsVisible;
        }

        private void RequisitesView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            IsValid = IsValidButton.IsEnabled || e.NewValue is bool and not true;
        }
    }
}