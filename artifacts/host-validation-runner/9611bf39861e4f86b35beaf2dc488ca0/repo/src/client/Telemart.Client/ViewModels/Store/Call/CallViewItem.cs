using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class CallViewItem : BindableBase, ICloneable, IDataErrorInfo, ILockableEntity
    {
        private const string SpaceSymbol = " ";
        private string displayPhones;

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value, () => RaisePropertyChanged(nameof(DisplayId))); }
        }

        public int? ParrentId
        {
            get { return GetProperty(() => ParrentId); }
            set { SetProperty(() => ParrentId, value); }
        }

        public bool IsFolder
        {
            get { return GetProperty(() => IsFolder); }
            set { SetProperty(() => IsFolder, value, () => RaisePropertiesChanged(nameof(Priority), nameof(CallType), nameof(CallFrom), nameof(CallTo), nameof(Fio), nameof(Phone), nameof(Task))); }
        }

        public int CallTypeId
        {
            get { return GetProperty(() => CallTypeId); }
            set { SetProperty(() => CallTypeId, value); }
        }

        public CallTypeDto CallType
        {
            get { return GetProperty(() => CallType); }
            set { SetProperty(() => CallType, value, () => RaisePropertyChanged(nameof(Incoming))); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int? EmployeeRespId
        {
            get { return GetProperty(() => EmployeeRespId); }
            set { SetProperty(() => EmployeeRespId, value); }
        }

        public int? OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? ServiceRequestId
        {
            get { return GetProperty(() => ServiceRequestId); }
            set { SetProperty(() => ServiceRequestId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public ContractorSimpleDto Contractor
        {
            get { return GetProperty(() => Contractor); }
            set { SetProperty(() => Contractor, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public Priority Priority
        {
            get { return GetProperty(() => Priority); }
            set { SetProperty(() => Priority, value); }
        }

        public CallState CallState
        {
            get { return GetProperty(() => CallState); }
            set { SetProperty(() => CallState, value); }
        }

        public ObservableCollection<CallDependencyViewItem> Dependencies
        {
            get { return GetProperty(() => Dependencies); }
            set { SetProperty(() => Dependencies, value); }
        }

        public DateTime? CallFrom
        {
            get { return GetProperty(() => CallFrom); }
            set { SetProperty(() => CallFrom, value, () => RaisePropertiesChanged(nameof(CallDisplayTime), nameof(ExpirationTime))); }
        }

        public DateTime? CallTo
        {
            get { return GetProperty(() => CallTo); }
            set { SetProperty(() => CallTo, value, () => RaisePropertiesChanged(nameof(CallDisplayTime), nameof(Expired))); }
        }

        public string CallDisplayTime => GetCallDisplayTime();

        public bool Incoming => CallType?.Id == CallTypeConstants.IncomingTypeId;

        public bool? Expired => CallFrom <= DateTime.Now ? (bool?)(CallTo < DateTime.Now) : null;

        public int? DisplayId => Id > 0 ? Id : (int?)null;

        public string ExpirationTime => CallFrom <= DateTime.Now && CallTimeDiff != null
            ? $"{(CallTimeDiff < TimeSpan.Zero ? "-" : string.Empty)}{Math.Abs((int)CallTimeDiff.Value.TotalHours):00}:{Math.Abs(CallTimeDiff.Value.Minutes):00}"
            : string.Empty;

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get
            {
                return GetProperty(() => Phone);
            }

            set
            {
                SetProperty(() => Phone, value, () =>
                {
                    CalculateDisplayPhones();
                    RaisePropertiesChanged(nameof(DisplayPhones), nameof(DisplayPhone));
                });
            }
        }

        public string Phone2
        {
            get
            {
                return GetProperty(() => Phone2);
            }

            set
            {
                SetProperty(() => Phone2, value, () =>
                {
                    CalculateDisplayPhones();
                    RaisePropertiesChanged(nameof(DisplayPhones), nameof(DisplayPhone));
                });
            }
        }

        public string DisplayPhone => IsCompleted ? Regex.Replace(Phone, "[0-9]", "0", RegexOptions.Compiled) : Phone;

        public string DisplayPhones
        {
            get
            {
                if (displayPhones == null)
                {
                    CalculateDisplayPhones();
                }

                return displayPhones;
            }
        }

        public string CreatedFrom
        {
            get { return GetProperty(() => CreatedFrom); }
            set { SetProperty(() => CreatedFrom, value); }
        }

        public string Task
        {
            get { return GetProperty(() => Task); }
            set { SetProperty(() => Task, value, () => RaisePropertyChanged(nameof(DisplayTask))); }
        }

        public string DisplayTask => Task?.Replace(Environment.NewLine, SpaceSymbol).Replace("\n", SpaceSymbol);

        public string Result
        {
            get { return GetProperty(() => Result); }
            set { SetProperty(() => Result, value, () => RaisePropertyChanged(nameof(DisplayResult))); }
        }

        public string DisplayResult => Result?.Replace(Environment.NewLine, SpaceSymbol).Replace("\n", SpaceSymbol);

        public int Attempt
        {
            get { return GetProperty(() => Attempt); }
            set { SetProperty(() => Attempt, value); }
        }

        public int? EmployeeCreatedById
        {
            get { return GetProperty(() => EmployeeCreatedById); }
            set { SetProperty(() => EmployeeCreatedById, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int? EmployeeCompletedById
        {
            get { return GetProperty(() => EmployeeCompletedById); }
            set { SetProperty(() => EmployeeCompletedById, value); }
        }

        public DateTime? CompletedOn
        {
            get
            {
                return GetProperty(() => CompletedOn);
            }

            set
            {
                SetProperty(() => CompletedOn, value, () =>
                {
                    RaisePropertyChanged(nameof(DisplayPhone));
                    IsCompleted = CompletedOn.HasValue;
                });
            }
        }

        public bool IsCompleted { get; set; }

        string IDataErrorInfo.Error => string.Empty;

        private TimeSpan? CallTimeDiff => CallTo - (CompletedOn ?? DateTime.Now);

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<CallViewItem> builder)
        {
            builder.Property(x => x.Priority).MatchesInstanceRule((x, y) => y.IsFolder || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CallType).MatchesInstanceRule((x, y) => y.IsFolder || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CallFrom).MatchesInstanceRule((x, y) => y.IsFolder || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.CallTo).MatchesInstanceRule((x, y) => y.IsFolder || x != null, () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Fio).MatchesInstanceRule((x, y) => y.IsFolder || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Phone).MatchesInstanceRule((x, y) => y.IsFolder || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
            builder.Property(x => x.Task).MatchesInstanceRule((x, y) => y.IsFolder || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        public string GetCallDisplayTime(string delimiter = "-")
        {
            if (CallFrom == null || CallTo == null)
            {
                return string.Empty;
            }

            return CallFrom?.Date == CallTo?.Date
                ? $"{CallFrom:dd.MM.yy HH:mm}{delimiter}{CallTo:HH:mm}"
                : $"{CallFrom:dd.MM.yy HH:mm}{delimiter}{CallTo:dd.MM.yy HH:mm}";
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public CallViewItem Clone()
        {
            CallViewItem cloneItem = ReflectionObjectCloner.Clone(this);

            cloneItem.Dependencies = Dependencies.Select(x => ReflectionObjectCloner.Clone(x)).ToObservableCollection();

            return cloneItem;
        }

        private void CalculateDisplayPhones()
        {
            displayPhones = Phone;

            if (!string.IsNullOrEmpty(Phone2))
            {
                displayPhones = $"{Phone}, {Phone2}";
            }
        }
    }
}