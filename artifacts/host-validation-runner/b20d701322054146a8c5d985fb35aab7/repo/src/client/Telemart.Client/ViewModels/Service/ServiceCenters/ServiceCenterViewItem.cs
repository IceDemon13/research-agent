using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Newtonsoft.Json;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Validation;
using Telemart.Client.Core.Cloning;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceCenters
{
    public sealed class ServiceCenterViewItem : BindableBase, IDataErrorInfo, ICloneable, ILockableEntity
    {
        public ServiceCenterViewItem()
        {
            Active = true;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? TypeId
        {
            get
            {
                return GetProperty(() => TypeId);
            }

            set
            {
                SetProperty(() => TypeId, value, () =>
                {
                    if (TypeId != ServiceCenterType.SupplierId)
                    {
                        SupplierId = null;
                    }

                    RaisePropertyChanged(nameof(SupplierId));
                });
            }
        }

        public ServiceCenterType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public int? EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int InvoicesPerDay
        {
            get { return GetProperty(() => InvoicesPerDay); }
            set { SetProperty(() => InvoicesPerDay, value); }
        }

        public ComboBoxItem? Employee
        {
            get { return GetProperty(() => Employee); }
            set { SetProperty(() => Employee, value, () => EmployeeId = Employee?.Id); }
        }

        public int? CityId
        {
            get
            {
                return GetProperty(() => CityId);
            }

            set
            {
                SetProperty(() => CityId, value, () =>
                {
                    NpWarehouseRef = NpPostBoxRef = NpStreetRef = null;

                    RaisePropertiesChanged(nameof(NpWarehouseRef), nameof(NpPostBoxRef), nameof(NpStreetRef), nameof(NpHouse));
                });
            }
        }

        public int? SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public ServiceCenterRepairConfirmType RepairConfirmType
        {
            get { return GetProperty(() => RepairConfirmType); }
            set { SetProperty(() => RepairConfirmType, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Address
        {
            get { return GetProperty(() => Address); }
            set { SetProperty(() => Address, value); }
        }

        public string Link
        {
            get { return GetProperty(() => Link); }
            set { SetProperty(() => Link, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Phone2
        {
            get { return GetProperty(() => Phone2); }
            set { SetProperty(() => Phone2, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Skype
        {
            get { return GetProperty(() => Skype); }
            set { SetProperty(() => Skype, value); }
        }

        public string Icq
        {
            get { return GetProperty(() => Icq); }
            set { SetProperty(() => Icq, value); }
        }

        public string RecipientFio
        {
            get { return GetProperty(() => RecipientFio); }
            set { SetProperty(() => RecipientFio, value); }
        }

        public string RecipientPhone
        {
            get { return GetProperty(() => RecipientPhone); }
            set { SetProperty(() => RecipientPhone, value); }
        }

        public string Edrpou
        {
            get { return GetProperty(() => Edrpou); }
            set { SetProperty(() => Edrpou, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public string Regulations
        {
            get { return GetProperty(() => Regulations); }
            set { SetProperty(() => Regulations, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value, () => RaisePropertyChanged(nameof(Employee))); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public Guid? NpWarehouseRef
        {
            get { return GetProperty(() => NpWarehouseRef); }
            set { SetProperty(() => NpWarehouseRef, value); }
        }

        public Guid? NpPostBoxRef
        {
            get { return GetProperty(() => NpPostBoxRef); }
            set { SetProperty(() => NpPostBoxRef, value); }
        }

        public Guid? NpStreetRef
        {
            get
            {
                return GetProperty(() => NpStreetRef);
            }

            set
            {
                SetProperty(() => NpStreetRef, value, () =>
                {
                    NpHouse = null;

                    RaisePropertyChanged(nameof(NpHouse));
                });
            }
        }

        public string NpHouse
        {
            get { return GetProperty(() => NpHouse); }
            set { SetProperty(() => NpHouse, value); }
        }

        #region IDataErrorInfo

        string IDataErrorInfo.Error => string.Empty;

        string IDataErrorInfo.this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        #endregion

        public static void BuildMetadata(MetadataBuilder<ServiceCenterViewItem> builder)
        {
            builder.Property(x => x.TypeId)
               .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SupplierId)
                .MatchesInstanceRule((x, y) => y.TypeId != ServiceCenterType.SupplierId || y.SupplierId.HasValue, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Name)
               .Required(() => Resources.RequiredErrorMessage)
               .MaxLength(ServiceCenterConstants.NameMaxLength);

            builder.Property(x => x.CityId)
               .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RepairConfirmType)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.Address)
               .Required(() => Resources.RequiredErrorMessage)
               .MaxLength(ServiceCenterConstants.AddressMaxLength);

            builder.Property(x => x.Employee)
                .RequiredActive(x => x.Active);

            builder.Property(x => x.Link)
                .MatchesInstanceRule((x, y) => IsUriValid(x), () => "Cсылка не корректна");

            builder.Property(x => x.Edrpou)
                .MatchesRule((x) => string.IsNullOrEmpty(x) || (x.Length is 8 or 10), () => "ЕДРПОУ или ИНН должны содержать 8 или 10 цифр соответственно");

            builder.Property(x => x.InvoicesPerDay)
                .MatchesInstanceRule((x, y) => x > 0 && x < 1000, () => "Кол-во должно быть 1-999");

            builder.Property(x => x.Email).MatchesRegularExpression(@"^$|^.+@.+\..+$", () => "Некорректный e-mail");

            builder.Property(x => x.NpHouse)
                .MatchesInstanceRule((x, y) => !y.NpStreetRef.HasValue || (!string.IsNullOrWhiteSpace(x) && IsNpHouseValid(x)), () => "Некорректное значение");

            builder.Property(x => x.Fio).MatchesRule((x) => x?.Split(" ").Where(y => !string.IsNullOrEmpty(y)).Count() > 1, () => "Требуется минимум два слова");

            builder.Property(x => x.RecipientFio).MatchesRule((x) => string.IsNullOrEmpty(x) || x.Split(" ").Where(y => !string.IsNullOrEmpty(y)).Count() > 1, () => "Требуется минимум два слова");
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ServiceCenterViewItem Clone()
        {
            return ReflectionObjectCloner.Clone(this);
        }

        private static bool IsUriValid(string uriString)
        {
            return string.IsNullOrWhiteSpace(uriString) || (Regex.IsMatch(uriString, @"^http(s)?://([\w-]+.)+[\w-]+(/[\w- ./?%&=])?$") && Uri.IsWellFormedUriString(uriString, UriKind.Absolute));
        }

        private static bool IsNpHouseValid(string npHouse)
        {
            Regex regex = new Regex(@"^[0-9]+[\:\-]*[А-Яа-я]{0,1}$"); // 12а || 12

            string[] parts = npHouse?.Split('/');

            if (parts.Length > 2 || parts.Any(x => !regex.IsMatch(x)))
            {
                return false;
            }

            return true;
        }
    }
}
