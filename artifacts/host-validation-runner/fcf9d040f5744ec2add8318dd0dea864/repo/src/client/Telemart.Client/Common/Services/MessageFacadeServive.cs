using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telemart.Client.Data.Requests.Features.Notifications;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Client.Views;

namespace Telemart.Client.Common.Services
{
    internal sealed class MessageFacadeServive : IMessageFacadeService
    {
        private const string NotificationError = "NotificationError";
        private const string NotificationWarning = "NotificationWarning";
        private const string MessageBoxError = "MessageBoxError";
        private const string MessageBoxWarning = "MessageBoxWarning";
        private const string UriPrefix = @"pack://application:,,,/Images/";
        private readonly ILogger<MessageFacadeServive> _logger;
        private readonly IMessenger _messenger;

        public MessageFacadeServive(
            INotificationService notificationService,
            INewCommentNotificationService newCommentNotificationService,
            IEntityNotificationService entityNotificationService,
            ILogger<MessageFacadeServive> logger,
            IMessenger messenger,
            IServiceProvider serviceProvider)
        {
            _messenger = messenger;
            _logger = logger;
            ServiceProvider = serviceProvider;
            NotificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            NewCommentNotificationService = newCommentNotificationService ?? throw new ArgumentNullException(nameof(newCommentNotificationService));
            EntityNotificationService = entityNotificationService ?? throw new ArgumentNullException(nameof(entityNotificationService));
        }

        private INotificationService NotificationService { get; }

        private INewCommentNotificationService NewCommentNotificationService { get; }

        private IEntityNotificationService EntityNotificationService { get; }

        private IServiceProvider ServiceProvider { get; }

        public bool Confirm(string text, string caption)
        {
            MainWindowViewModel viewModel = (MainWindowViewModel)App.Current.MainWindow.DataContext;

            ConfirmViewModel confirm = viewModel.WorkspaceViewModel.DialogDocumentManagerService.ShowView<ConfirmViewModel>(new ConfirmViewModelParameter(text, caption), viewModel);

            return confirm.IsOk;
        }

        public MessageBoxResult ShowMessageBox(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            MessageBoxResult defaultResult = MessageBoxResult.None,
            FrameworkElement owner = null)
        {
            return DXMessageBox.Show(owner ?? App.Current.MainWindow, messageBoxText, caption, button, icon, defaultResult);
        }

        public MessageBoxResult ShowCustomMessageBox(
            string messageBoxText,
            string caption,
            MessageBoxButton button,
            MessageBoxImage icon,
            bool suspendKeyboard,
            Window owner = null)
        {
            var vm = new CustomMessageBoxViewModel
            {
                Caption = caption,
                Message = messageBoxText,
                Buttons = button,
                Icon = icon,
                SuspendKeyboard = suspendKeyboard
            };

            var view = new CustomMessageBoxView(vm)
            {
                Owner = owner ?? Application.Current.MainWindow
            };

            view.ShowDialog();
            return vm.Result;
        }

        public void ShowMessageBoxError(string text, string caption = "", FrameworkElement owner = null)
        {
            if (string.IsNullOrWhiteSpace(caption))
            {
                caption = Resources.MessageBox_ErrorCaption;
            }

            ShowMessageBox(text, caption, MessageBoxButton.OK, MessageBoxImage.Error, owner: owner);

            _logger.LogInformation("{messageBoxError}: {text}", MessageBoxError, text);
        }

        public void ShowMessageBoxInfo(string text, string caption = "", FrameworkElement owner = null)
        {
            if (string.IsNullOrWhiteSpace(caption))
            {
                caption = Resources.MessageBox_InformationCaption;
            }

            ShowMessageBox(text, caption, MessageBoxButton.OK, MessageBoxImage.Information, owner: owner);
        }

        public void ShowMessageBoxWarning(string text, string caption = "", FrameworkElement owner = null)
        {
            if (string.IsNullOrWhiteSpace(caption))
            {
                caption = Resources.MessageBox_WarningCaption;
            }

            ShowMessageBox(text, caption, MessageBoxButton.OK, MessageBoxImage.Warning, owner: owner);

            _logger.LogInformation("{messageBoxWarning}: {text}", MessageBoxWarning, text);
        }

        public void ShowNewCommentNotification(string caption, string content, int commentId, ISupportServices parent)
        {
            CustomNewCommentNotificationViewModel viewModel = CustomNewCommentNotificationViewModel.Create(caption, content, commentId, parent);
            INotification notification = NewCommentNotificationService.CreateCustomNotification(viewModel);
            viewModel.SetNotification(notification);
            notification.ShowAsync();
        }

        public void ShowEntityNotification<TMessage>(
            int? notificationId,
            string caption,
            string content,
            TMessage viewModelParameter,
            MessageBoxImage icon)
        where TMessage : class
        {
            CustomEntityNotificationViewModel<TMessage> viewModel = CustomEntityNotificationViewModel<TMessage>.Create(
                caption,
                content,
                viewModelParameter,
                InitImageSource(icon),
                _messenger);

            INotification notification = EntityNotificationService.CreateCustomNotification(viewModel);
            viewModel.SetNotification(notification);

            try
            {
                Task<NotificationResult> resultTask = notification.ShowAsync();

                resultTask.ContinueWith(
                    async x =>
                    {
                        IWebClient webClient = ServiceProvider.GetRequiredService<IWebClient>();

                        if (notificationId.HasValue && x.Result is NotificationResult.Activated or NotificationResult.UserCanceled or NotificationResult.ApplicationHidden)
                        {
                            await webClient.ExecuteApiRequestAsync(new ReadNotification(notificationId.Value));
                        }
                    },
                    TaskScheduler.Current);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "Failed to show entity notification");
            }
        }

        public void ShowNotificationError(string text, bool playSound = false)
        {
            ShowNotification(text, MessageBoxImage.Error);

            if (playSound)
            {
                System.Media.SystemSounds.Hand.Play();
            }
        }

        public void ShowNotificationInfo(string text)
        {
            ShowNotification(text, MessageBoxImage.Information);
        }

        public void ShowUpdateNotificationInfo(string text)
        {
            ImageSource image = new BitmapImage(new Uri("pack://application:,,,/Telemart.Client;component/Images/update_ready_notification.png"));

            ShowNotification(text, image);
        }

        public void ShowNotificationWarning(string text, bool playSound = false)
        {
            ShowNotification(text, MessageBoxImage.Warning);

            if (playSound)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        public void ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems, ISupportServices parent)
        {
            IDocumentManagerService sizeableDialogDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

            ValidationResultItem[] validationItemsArray = validationItems as ValidationResultItem[] ?? validationItems.ToArray();

            sizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItemsArray),
                parent);

            string validationItemsString = string.Join(", ", validationItemsArray.Select(x => x.Message));

            _logger.LogInformation("ValidationItems: {validationItems}", validationItemsString);
        }

        public void ShowNotification(string text, MessageBoxImage icon)
        {
            ImageSource image = InitImageSource(icon);

            ShowNotification(text, image);

            switch (icon)
            {
                case MessageBoxImage.Error:
                    _logger.LogInformation("{notificationError}: {text}", NotificationError, text);
                    break;
                case MessageBoxImage.Warning:
                    _logger.LogInformation("{notificationWarning}: {text}", NotificationWarning, text);
                    break;
            }
        }

        private static ImageSource InitImageSource(MessageBoxImage icon)
        {
            ImageSource image = icon switch
            {
                MessageBoxImage.Information => new BitmapImage(new Uri($"{UriPrefix}information_32_32.png", UriKind.RelativeOrAbsolute)),
                MessageBoxImage.Error => new BitmapImage(new Uri($"{UriPrefix}error_32_32.png", UriKind.RelativeOrAbsolute)),
                MessageBoxImage.Warning => new BitmapImage(new Uri($"{UriPrefix}warning_32_32.png", UriKind.RelativeOrAbsolute)),
                MessageBoxImage.Question => new BitmapImage(new Uri($"{UriPrefix}question_32_32.png", UriKind.RelativeOrAbsolute)),
                _ => null
            };

            return image;
        }

        private void ShowNotification(string text, ImageSource image)
        {
            CustomNotificationViewModel viewModel = CustomNotificationViewModel.Create(Resources.ProductName, text, image);
            INotification notification = NotificationService.CreateCustomNotification(viewModel);
            notification.ShowAsync();
        }
    }
}