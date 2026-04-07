using System.Collections.ObjectModel;
using System.Windows;
using DevExpress.Xpf.Grid;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Views.Common
{
    /// <summary>
    /// Interaction logic for CrmInfoView.xaml
    /// </summary>
    public partial class CrmInfoView
    {
        public static readonly DependencyProperty ClientContactsProperty = DependencyProperty.Register(
            nameof(ClientContacts),
            typeof(ReadOnlyObservableCollection<ClientContactViewItem>),
            typeof(CrmInfoView),
            new PropertyMetadata(default(ReadOnlyObservableCollection<ClientContactViewItem>)));

        public static readonly DependencyProperty SelectedClientContactProperty = DependencyProperty.Register(
            nameof(SelectedClientContact),
            typeof(ClientContactViewItem),
            typeof(CrmInfoView),
            new PropertyMetadata(default(ClientContactViewItem)));

        public static readonly DependencyProperty ShowLoadingPanelProperty = DependencyProperty.Register(
            nameof(ShowLoadingPanel),
            typeof(bool),
            typeof(CrmInfoView),
            new PropertyMetadata(false));

        public static readonly DependencyProperty ClientContactsViewProperty = DependencyProperty.Register(
            nameof(ClientContactsView),
            typeof(TableView),
            typeof(CrmInfoView),
            new PropertyMetadata(null));

        public CrmInfoView()
        {
            InitializeComponent();
        }

        public ReadOnlyObservableCollection<ClientContactViewItem> ClientContacts
        {
            get => (ReadOnlyObservableCollection<ClientContactViewItem>)GetValue(ClientContactsProperty);
            set => SetValue(ClientContactsProperty, value);
        }

        public ClientContactViewItem SelectedClientContact
        {
            get => (ClientContactViewItem)GetValue(SelectedClientContactProperty);
            set => SetValue(SelectedClientContactProperty, value);
        }

        public bool ShowLoadingPanel
        {
            get => (bool)GetValue(ShowLoadingPanelProperty);
            set => SetValue(ShowLoadingPanelProperty, value);
        }

        public TableView ClientContactsView
        {
            get => (TableView)GetValue(ClientContactsViewProperty);
            set => SetValue(ClientContactsViewProperty, value);
        }

        private void CrmInfoViewLoaded(object sender, RoutedEventArgs e)
        {
            ClientContactsView = CrmGridControl.View as TableView;
        }

        private void TableView_OnCustomCellAppearance(object sender, CustomCellAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }

        private void TableView_OnCustomRowAppearance(object sender, CustomRowAppearanceEventArgs e)
        {
            e.Result = e.ConditionalValue;
            e.Handled = true;
        }
    }
}
