using System;
using System.IO;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Layouts;

namespace Telemart.Client.Common.Layouts
{
    public class ModuleLayoutService : BindableBase, IModuleLayoutService
    {
        public ModuleLayoutService()
        {
            LoadLayoutCommand = new DelegateCommand<GridControl>(LoadLayout);
            SaveLayoutCommand = new DelegateCommand<GridControl>(SaveLayout);
        }

        public IDelegateCommand LoadLayoutCommand { get; }

        public IDelegateCommand SaveLayoutCommand { get; }

        public string Layout
        {
            get { return GetProperty(() => Layout); }
            set { SetProperty(() => Layout, value); }
        }

        protected IDocumentManagerService SizeableDocumentManagerService { get; set; }

        protected ISupportServices Parent { get; set; }

        protected int ModuleId { get; set; }

        public virtual void LoadLayout(GridControl gridControl)
        {
            ModuleLayoutsViewModel viewModel = SizeableDocumentManagerService.ShowView<ModuleLayoutsViewModel>(new ModuleLayoutParameter(ModuleId, true, null, null), Parent);

            if (!viewModel.IsOk)
            {
                return;
            }

            Layout = string.Empty; // Needs to trigger GridLayoutBehavior.OnLayoutChanged()
            Layout = viewModel.SelectedLayout.Layout;
            RaisePropertyChanged(nameof(Layout));
        }

        public virtual void SaveLayout(GridControl gridControl)
        {
            string layout = SaveLayoutToString(gridControl.SaveLayoutToStream);

            SizeableDocumentManagerService.ShowView<ModuleLayoutsViewModel>(
                new ModuleLayoutParameter(
                    ModuleId,
                    false,
                    layout,
                    null),
                Parent);
        }

        public void Init(int moduleId, ISupportServices parent)
        {
             SizeableDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);
             ModuleId = moduleId;
             Parent = parent;
        }

        protected static string SaveLayoutToString(Action<MemoryStream> saveToStreamAction)
        {
            using MemoryStream layoutStream = new();
            using StreamReader streamReader = new(layoutStream);
            saveToStreamAction(layoutStream);

            layoutStream.Position = 0;

            return streamReader.ReadToEnd();
        }
    }
}