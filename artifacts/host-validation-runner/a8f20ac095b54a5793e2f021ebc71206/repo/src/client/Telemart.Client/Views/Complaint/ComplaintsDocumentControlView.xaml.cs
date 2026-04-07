using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.ViewModels.Complaint;

namespace Telemart.Client.Views.Complaint
{
    /// <summary>
    /// Interaction logic for ComplaintsDocumentControlView.xaml
    /// </summary>
    public partial class ComplaintsDocumentControlView
    {
        public static readonly DependencyProperty ComplaintsProperty = DependencyProperty.Register(
            nameof(Complaints),
            typeof(ObservableCollection<ComplaintViewItem>),
            typeof(ComplaintsDocumentControlView),
            new PropertyMetadata(default(ObservableCollection<ComplaintViewItem>)));

        public static readonly DependencyProperty AddComplaintCommandProperty = DependencyProperty.Register(
            nameof(AddComplaintCommand),
            typeof(ICommand),
            typeof(ComplaintsDocumentControlView),
            new PropertyMetadata(default(ICommand)));

        public static readonly DependencyProperty EditComplaintCommandProperty = DependencyProperty.Register(
            nameof(EditComplaintCommand),
            typeof(ICommand),
            typeof(ComplaintsDocumentControlView),
            new PropertyMetadata(new DelegateCommand<ComplaintViewItem>(EditComplaint, x => x != null)));

        public static readonly DependencyProperty RefreshComplaintsCommandProperty = DependencyProperty.Register(
            nameof(RefreshComplaintsCommand),
            typeof(IAsyncCommand),
            typeof(ComplaintsDocumentControlView),
            new PropertyMetadata(default(IAsyncCommand)));

        public ComplaintsDocumentControlView()
        {
            InitializeComponent();
        }

        public ObservableCollection<ComplaintViewItem> Complaints
        {
            get => (ObservableCollection<ComplaintViewItem>)GetValue(ComplaintsProperty);
            set => SetValue(ComplaintsProperty, value);
        }

        public ICommand AddComplaintCommand
        {
            get => (ICommand)GetValue(AddComplaintCommandProperty);
            set => SetValue(AddComplaintCommandProperty, value);
        }

        public ICommand EditComplaintCommand
        {
            get => (ICommand)GetValue(EditComplaintCommandProperty);
            set => SetValue(EditComplaintCommandProperty, value);
        }

        public IAsyncCommand RefreshComplaintsCommand
        {
            get => (IAsyncCommand)GetValue(RefreshComplaintsCommandProperty);
            set => SetValue(RefreshComplaintsCommandProperty, value);
        }

        private static void EditComplaint(ComplaintViewItem complaint)
        {
            if (complaint != null)
            {
                Messenger.Default.Send(new ComplaintViewMessage(complaint.Id));
            }
        }
    }
}
