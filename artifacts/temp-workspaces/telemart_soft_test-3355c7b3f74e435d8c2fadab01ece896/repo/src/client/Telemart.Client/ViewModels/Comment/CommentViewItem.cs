using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Comment
{
    public class CommentViewItem : BindableBase, ILocalіzableEntity
    {
        public CommentViewItem()
        {
            IsStateChanging = false;
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int? ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public int EntityId
        {
            get { return GetProperty(() => EntityId); }
            set { SetProperty(() => EntityId, value); }
        }

        public CommentType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public CommentState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value); }
        }

        public DateTime Date
        {
            get { return GetProperty(() => Date); }
            set { SetProperty(() => Date, value); }
        }

        public int? ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductNameUkr
        {
            get { return GetProperty(() => ProductNameUkr); }
            set { SetProperty(() => ProductNameUkr, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductNameEn
        {
            get { return GetProperty(() => ProductNameEn); }
            set { SetProperty(() => ProductNameEn, value, () => { RaisePropertiesChanged(nameof(DisplayProductName)); }); }
        }

        public string ProductLink
        {
            get { return GetProperty(() => ProductLink); }
            set { SetProperty(() => ProductLink, value); }
        }

        public string Email
        {
            get { return GetProperty(() => Email); }
            set { SetProperty(() => Email, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string Text
        {
            get { return GetProperty(() => Text); }
            set { SetProperty(() => Text, value); }
        }

        public bool ValidationRequired
        {
            get { return GetProperty(() => ValidationRequired); }
            set { SetProperty(() => ValidationRequired, value); }
        }

        public DateTime? ChangeDate
        {
            get { return GetProperty(() => ChangeDate); }
            set { SetProperty(() => ChangeDate, value); }
        }

        public string Pro
        {
            get { return GetProperty(() => Pro); }
            set { SetProperty(() => Pro, value); }
        }

        public string Contra
        {
            get { return GetProperty(() => Contra); }
            set { SetProperty(() => Contra, value); }
        }

        public int Like
        {
            get { return GetProperty(() => Like); }
            set { SetProperty(() => Like, value); }
        }

        public int Dislike
        {
            get { return GetProperty(() => Dislike); }
            set { SetProperty(() => Dislike, value); }
        }

        public bool BoughtProduct
        {
            get { return GetProperty(() => BoughtProduct); }
            set { SetProperty(() => BoughtProduct, value); }
        }

        public bool Foto
        {
            get { return GetProperty(() => Foto); }
            set { SetProperty(() => Foto, value); }
        }

        public DateTime CreatedOn
        {
            get { return GetProperty(() => CreatedOn); }
            set { SetProperty(() => CreatedOn, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }

        public bool IsStateChanging
        {
            get { return GetProperty(() => IsStateChanging); }
            set { SetProperty(() => IsStateChanging, value); }
        }

        public decimal? StarsAvg
        {
            get { return GetProperty(() => StarsAvg); }
            set { SetProperty(() => StarsAvg, value); }
        }

        public decimal? StarsDivisionCount
        {
            get { return GetProperty(() => StarsDivisionCount); }
            set { SetProperty(() => StarsDivisionCount, value); }
        }

        public int ProductRatio
        {
            get { return GetProperty(() => ProductRatio); }
            set { SetProperty(() => ProductRatio, value); }
        }

        public AvatarType Avatar
        {
            get { return GetProperty(() => Avatar); }
            set { SetProperty(() => Avatar, value); }
        }

        public ObservableCollection<CommentViewItem> Children
        {
            get { return GetProperty(() => Children); }
            set { SetProperty(() => Children, value); }
        }

        string ILocalіzableEntity.Name => ProductName;

        string ILocalіzableEntity.NameUkr => ProductNameUkr;

        string ILocalіzableEntity.NameEn => ProductNameEn;

        public string DisplayProductName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}