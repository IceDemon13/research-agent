using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Directories.Contractor;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class SelectContractorTemplateViewModel : TelemartDialogViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectContractorTemplateViewModel" /> class.
        /// </summary>
        /// <param name="webClient">The web client.</param>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <param name="messageFacadeService">The message facade service.</param>
        /// <param name="mapper">The mapper.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mapper" /> is <see langword="null" />.</exception>
        public SelectContractorTemplateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            TemplatesEditor = new ContractorTemplatesEditorViewModel(WebClient, Dictionaries, MessageFacadeService);
            TemplatesEditor.ApplyTemplate += ApplyTemplate;
            TemplatesEditor.Cancel += TemplateEditorCancel;

            HandleSelectedContractorChangedCommand = new DelegateCommand<ContractorViewItem>(HandleSelectedContractorChanged);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);

            Title = "Выбор шаблона";
        }

        public SelectContractorTemplateViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleSelectedContractorChangedCommand { get; }

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        #endregion

        #region INPC

        public List<ContractorViewItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            set { SetProperty(() => Contractors, value); }
        }

        public ContractorTemplatesEditorViewModel TemplatesEditor
        {
            get { return GetProperty(() => TemplatesEditor); }
            set { SetProperty(() => TemplatesEditor, value); }
        }

        public string OkButtonTitle
        {
            get { return GetProperty(() => OkButtonTitle); }
            private set { SetProperty(() => OkButtonTitle, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<SelectContractorTemplateViewModel> builder)
        {
            ////builder.Property(x => x.TemplatesEditor.SelectedContractor).Required(() => Resources.RequiredErrorMessage);
            ////builder.Property(x => x.TemplatesEditor.SelectedContractorTemplate).Required(() => Resources.RequiredErrorMessage);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            TemplatesEditor.Cancel -= TemplateEditorCancel;
            TemplatesEditor.ApplyTemplate -= ApplyTemplate;
        }

        protected override bool CanOk()
        {
            return TemplatesEditor.SelectedContractor != null && TemplatesEditor.SelectedContractorTemplate != null;
        }

        protected override async Task HandleLoadedAsync()
        {
            ContractorTemplateMode mode = (ContractorTemplateMode)Parameter;

            switch (mode)
            {
                case ContractorTemplateMode.CreateOrder:
                    OkButtonTitle = "Создать";
                    break;
                case ContractorTemplateMode.Select:
                    OkButtonTitle = "Выбрать";
                    break;
            }

            try
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

                Contractors = contractors
                    .Where(x => x.IsFolder == false && x.IsClient && x.Active)
                    .OrderBy(x => x.SubdivisionId)
                    .ThenBy(x => x.Name)
                    .Select(x => Mapper.Map<ContractorViewItem>(x))
                    .Where(x => WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.Subdivision.Id))
                    .ToList();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to get contractors");
                MessageFacadeService.ShowNotificationError("Ошибка при получении данных");
            }
        }

        protected override Task HandleOkAsync()
        {
            IsOk = true;
            Close();
            return Task.CompletedTask;
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OkCommand.Execute(null);
            }
            else if (e.Key == Key.Escape)
            {
                CancelCommand.Execute(null);
            }
        }

        private void HandleSelectedContractorChanged(ContractorViewItem contractor)
        {
            TemplatesEditor.SelectedContractor = contractor;
        }

        private void TemplateEditorCancel()
        {
            CancelCommand.Execute(null);
        }

        private void ApplyTemplate()
        {
            OkCommand.Execute(null);
        }
    }
}