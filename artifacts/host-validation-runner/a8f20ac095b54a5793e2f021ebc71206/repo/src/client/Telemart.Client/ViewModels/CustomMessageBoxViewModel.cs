using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.Native;

namespace Telemart.Client.ViewModels
{

    public class CustomMessageBoxViewModel : ViewModelBase
    {
        public string Message { get; init; } = string.Empty;

        public string Caption { get; init; } = "Сообщение";

        public bool SuspendKeyboard { get; init; } = false;

        public MessageBoxButton Buttons { get; init; } = MessageBoxButton.OK;

        public MessageBoxImage Icon { get; init; } = MessageBoxImage.None;

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public IDelegateCommand OkCommand => new DelegateCommand(() => Close(MessageBoxResult.OK));

        public IDelegateCommand CancelCommand => new DelegateCommand(() => Close(MessageBoxResult.Cancel));

        public IDelegateCommand YesCommand => new DelegateCommand(() => Close(MessageBoxResult.Yes));

        public IDelegateCommand NoCommand => new DelegateCommand(() => Close(MessageBoxResult.No));

        public IDelegateCommand KeyDownCommand => new DelegateCommand<KeyEventArgs>(OnKeyDown);

        public ImageSource? ImageSource
        {
            get
            {
                string? name = Icon switch
                {
                    MessageBoxImage.Information => "SvgImages/XAF/Action_AboutInfo.svg",
                    MessageBoxImage.Warning => "SvgImages/Status/Warning.svg",
                    MessageBoxImage.Error => "SvgImages/Outlook Inspired/Cancel.svg",
                    MessageBoxImage.Question => "SvgImages/Icon Builder/Actions_Question.svg",
                    _ => null
                };

                if (name != null)
                {
                    return WpfSvgRenderer.CreateImageSource(DXImageHelper.GetImageUri(name));
                }

                return null;
            }
        }

        public void OnKeyDown(KeyEventArgs args)
        {
            if (SuspendKeyboard)
            {
                args.Handled = true;
                return;
            }
            else
            {
                switch (args.Key)
                {
                    case Key.Enter:
                        {
                            if (Buttons == MessageBoxButton.OK || Buttons == MessageBoxButton.OKCancel)
                            {
                                Close(MessageBoxResult.OK);
                            }
                            else if (Buttons == MessageBoxButton.YesNo || Buttons == MessageBoxButton.YesNoCancel)
                            {
                                Close(MessageBoxResult.Yes);
                            }

                            args.Handled = true;
                            break;
                        }
                    case Key.Escape:
                        {
                            if (Buttons == MessageBoxButton.OKCancel || Buttons == MessageBoxButton.YesNoCancel)
                            {
                                Close(MessageBoxResult.Cancel);
                            }
                            else if (Buttons == MessageBoxButton.YesNo)
                            {
                                Close(MessageBoxResult.No);
                            }

                            args.Handled = true;
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }

        public event Action<MessageBoxResult>? RequestClose;

        private void Close(MessageBoxResult result)
        {
            Result = result;
            RequestClose?.Invoke(result);
        }
    }
}
