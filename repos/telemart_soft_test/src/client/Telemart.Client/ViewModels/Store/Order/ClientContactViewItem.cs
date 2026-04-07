using System;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ClientContactViewItem : BindableBase
    {
        public DateTime? Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public ClientContactType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Content
        {
            get { return GetProperty(() => Content); }
            set { SetProperty(() => Content, value); }
        }

        public CallState CallState
        {
            get { return GetProperty(() => CallState); }
            set { SetProperty(() => CallState, value, () => RaisePropertyChanged(nameof(State))); }
        }

        public SmsState SmsState
        {
            get { return GetProperty(() => SmsState); }
            set { SetProperty(() => SmsState, value, () => RaisePropertyChanged(nameof(State))); }
        }

        public EmailState EmailState
        {
            get { return GetProperty(() => EmailState); }
            set { SetProperty(() => EmailState, value, () => RaisePropertyChanged(nameof(State))); }
        }

        public string State
        {
            get
            {
                if (Type == ClientContactType.Call)
                {
                    return CallState?.Name;
                }

                if (Type == ClientContactType.Sms)
                {
                    return SmsState?.Name;
                }

                if (Type == ClientContactType.Email)
                {
                    return EmailState?.Name;
                }

                return null;
            }
        }

        public int? SmsTemplateId
        {
            get { return GetProperty(() => SmsTemplateId); }
            set { SetProperty(() => SmsTemplateId, value); }
        }

        public string EmployeeName
        {
            get { return GetProperty(() => EmployeeName); }
            set { SetProperty(() => EmployeeName, value); }
        }

        public int InitiatedBy
        {
            get { return GetProperty(() => InitiatedBy); }
            set { SetProperty(() => InitiatedBy, value); }
        }
    }
}
