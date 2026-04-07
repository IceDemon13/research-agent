using System;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Layouts;

namespace Telemart.Client.Common.Layouts
{
    public class FilterModuleLayoutService<TFilteringItem> : ModuleLayoutService, IFilterModuleLayoutService<TFilteringItem>
        where TFilteringItem : FilteringItemBase, new()
    {
        private readonly ILogger<FilterModuleLayoutService<TFilteringItem>> logger;

        private IFilteringViewModel<TFilteringItem> filteringViewModel;

        public FilterModuleLayoutService(ILogger<FilterModuleLayoutService<TFilteringItem>> logger)
        {
            this.logger = logger;
        }

        public override void LoadLayout(GridControl gridControl)
        {
            ModuleLayoutsViewModel viewModel = SizeableDocumentManagerService.ShowView<ModuleLayoutsViewModel>(new ModuleLayoutParameter(ModuleId, true, null, null), Parent);

            if (!viewModel.IsOk)
            {
                return;
            }

            Layout = string.Empty; // Needs to trigger GridLayoutBehavior.OnLayoutChanged()
            Layout = viewModel.SelectedLayout.Layout;
            RaisePropertyChanged(nameof(Layout));

            TFilteringItem filteringItem = new TFilteringItem();

            try
            {
                filteringItem.WithInitializedDataViaReflection(viewModel.SelectedLayout.Parameters);

                filteringViewModel.SetFilteringItem(filteringItem);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply serialized parameters");
            }
        }

        public override void SaveLayout(GridControl gridControl)
        {
            SizeableDocumentManagerService.ShowView<ModuleLayoutsViewModel>(
                new ModuleLayoutParameter(
                    ModuleId,
                    false,
                    SaveLayoutToString(gridControl.SaveLayoutToStream),
                    filteringViewModel.GetFilteringItem().BuildParameters().Select(x => (x.Item1, x.Item2)).ToArray()),
                Parent);
        }

        public void Init(int moduleId, ISupportServices parent, IFilteringViewModel<TFilteringItem> viewModel)
        {
            SizeableDocumentManagerService = parent.ServiceContainer.GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);
            ModuleId = moduleId;
            Parent = parent;

            filteringViewModel = viewModel;
        }
    }
}