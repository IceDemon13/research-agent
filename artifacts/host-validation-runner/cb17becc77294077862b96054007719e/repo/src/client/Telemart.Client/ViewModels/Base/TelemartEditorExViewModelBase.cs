using System;
using System.ComponentModel;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;

namespace Telemart.Client.ViewModels.Base
{
    public abstract class TelemartEditorExViewModelBase<TDto, TParam, TViewItem> : TelemartEditorViewModelBase<TDto, TParam, TViewItem>
        where TDto : class, new()
        where TParam : class, IEditorParameter
        where TViewItem : ILockableEntity, INotifyPropertyChanged, IDataErrorInfo, ICloneable, ICommentEntity, new()
    {
        protected TelemartEditorExViewModelBase(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            EditCommentCommand = new DelegateCommand(EditComment, CanEditComment);
        }

        protected TelemartEditorExViewModelBase()
        {
        }

        public IDelegateCommand EditCommentCommand { get; }

        private bool CanEditComment()
        {
            return Model != null && !IsNew && Model.EmployeeLockId == null && !CanEdit();
        }

        private void EditComment()
        {
            TelemartCommentEditorViewModel<TDto> viewModel = new TelemartCommentEditorViewModel<TDto>(
                WebClient,
                Dictionaries,
                MessageFacadeService,
                Messenger);

            CommentEditorParameter parameter = new CommentEditorParameter(Model.Id, Model.Comment);

            DialogDocumentManagerService.ShowView("TelemartCommentEditorView", viewModel, parameter, this);

            if (viewModel.IsOk)
            {
                SetData(viewModel.UpdatedEntity);
            }
        }
    }
}