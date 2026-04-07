using System.Windows;
using DevExpress.Xpf.Core;
using Telemart.Client.ViewModels;

namespace Telemart.Client.Views
{
    /// <summary>
    /// Interaction logic for CustomMessageBoxView.xaml
    /// </summary>
    public partial class CustomMessageBoxView : ThemedWindow
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBoxView(CustomMessageBoxViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += res =>
            {
                Result = res;
                DialogResult = true;
                Close();
            };
        }
    }
}
