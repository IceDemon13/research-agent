using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Diagram;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public class CompanyStructureViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private ReadOnlyObservableCollection<DepartmentDto> _departments;
        private ReadOnlyObservableCollection<EmployeeDto> _employees;

        public CompanyStructureViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            RefreshCommand = new AsyncCommand(RefreshAsync);
            OpenDepartmentsCommand = new DelegateCommand(OpenDepartments);
            SelectPanToolCommand = new DelegateCommand(SelectPanTool, () => AllowSelectPanTool);
            SelectPointerToolCommand = new DelegateCommand(SelectPointerTool, () => !AllowSelectPanTool);
            DiagramSelectionChangedCommand = new DelegateCommand<DiagramItem>(DiagramSelectionChanged);
            DiagramDoubleClickCommand = new DelegateCommand<DiagramItem>(DiagramDoubleClick);

            messenger.Register<DepartmentMessage>(this, OnDepartmentMessage);
        }

        public virtual List<StructureDiagramViewItem> Structures
        {
            get { return GetProperty(() => Structures); }
            set { SetProperty(() => Structures, value); }
        }

        public bool AllowSelectPanTool
        {
            get { return GetProperty(() => AllowSelectPanTool); }
            set { SetProperty(() => AllowSelectPanTool, value); }
        }

        public DiagramItem SelectedDiagramItem
        {
            get { return GetProperty(() => SelectedDiagramItem); }
            set { SetProperty(() => SelectedDiagramItem, value); }
        }

        public StructureDiagramViewItem SelectedStructureDiagramViewItem
        {
            get { return GetProperty(() => SelectedStructureDiagramViewItem); }
            set { SetProperty(() => SelectedStructureDiagramViewItem, value); }
        }

        public EventHandler PointerToolSelected;

        public EventHandler PanToolSelected;

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand OpenDepartmentsCommand { get; }

        public IDelegateCommand SelectPanToolCommand { get; }

        public IDelegateCommand SelectPointerToolCommand { get; }

        public IDelegateCommand DiagramSelectionChangedCommand { get; }

        public IDelegateCommand DiagramDoubleClickCommand { get; }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Edit:
                    OpenDepartmentsCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            await Task.WhenAll(LoadDepartmentsAsync(), LoadEmployeesAsync());

            Structures = _departments?.Select(
                x => MapToStructureDiagramViewItem(x, _employees?.FirstOrDefault(y => y.Id == x.EmployeeId)))
                .ToList();
        }

        private void OpenDepartments()
        {
            DialogDocumentManagerService.ShowView<DepartmentsViewModel>(null, this);
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            _departments = dtos.Where(x => x.Active).ToReadOnlyObservableCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            _employees = employees.ToReadOnlyObservableCollection();
        }

        private StructureDiagramViewItem MapToStructureDiagramViewItem(DepartmentDto departmentDto, EmployeeDto employeeDto)
        {
            return new StructureDiagramViewItem(
                departmentDto.Id,
                departmentDto.Name,
                employeeDto?.Id,
                employeeDto?.Name,
                employeeDto?.Position,
                employeeDto?.Email,
                employeeDto?.Phone1,
                departmentDto.ParentId ?? 0);
        }

        private void OnDepartmentMessage(DepartmentMessage message)
        {
            RefreshCommand.Execute(null);
        }

        private void SelectPanTool()
        {
            PanToolSelected.Invoke(this, null!);
            AllowSelectPanTool = false;
        }

        private void SelectPointerTool()
        {
            PointerToolSelected.Invoke(this, null!);
            AllowSelectPanTool = true;
        }

        private void DiagramSelectionChanged(DiagramItem diagramItem)
        {
            SelectedDiagramItem = diagramItem;

            if (diagramItem?.DataContext is StructureDiagramViewItem diagramViewItem)
            {
                SelectedStructureDiagramViewItem = diagramViewItem;
            }
            else
            {
                SelectedStructureDiagramViewItem = null;
            }
        }

        private void DiagramDoubleClick(DiagramItem diagramItem)
        {
            if (diagramItem?.DataContext is not StructureDiagramViewItem diagramViewItem)
            {
                return;
            }

            DialogDocumentManagerService.ShowView<DepartmentEditViewModel>(new DepartmentViewParameter(diagramViewItem.DepartmentId), this);
        }
    }
}