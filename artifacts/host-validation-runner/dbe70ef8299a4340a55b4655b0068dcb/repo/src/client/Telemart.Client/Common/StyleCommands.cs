using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.Mvvm;
using DevExpress.Xpf.Editors;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Common.Messages;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Discussions;
using Telemart.Client.ViewModels.ModuleAnalytics;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Common
{
    public static class StyleCommands
    {
        public static IServiceProvider ServiceProvider { get; set; }

        public static IDelegateCommand LookUpEditPopupOpening { get; } = new DelegateCommand<OpenPopupEventArgs>(x =>
            {
                x.Cancel = x.Source is LookUpEditBase edit && edit.IsReadOnly;
            });

        public static IDelegateCommand WikiHelpOpen { get; } = new DelegateCommand<object>(
            x =>
            {
                if (x == null)
                {
                    return;
                }

                string nameModule = x.GetType().Name;

                IDictionaries dictionaries = ServiceProvider.GetRequiredService<IDictionaries>();

                ModuleHelpUrl modelHelp = dictionaries.GetItemByName<ModuleHelpUrl>(nameModule);

                IMessenger messenger = ServiceProvider.GetRequiredService<IMessenger>();

                messenger.Send(new TelewikiHelpMessage(modelHelp.ModuleName, modelHelp.Url));
            },
            x =>
            {
                if (x == null)
                {
                    return false;
                }

                string nameModule = x.GetType().Name;

                IDictionaries dictionaries = ServiceProvider.GetRequiredService<IDictionaries>();

                ModuleHelpUrl modelHelp = dictionaries.GetItemByName<ModuleHelpUrl>(nameModule);

                return !string.IsNullOrWhiteSpace(modelHelp?.Url);
            });

        public static IDelegateCommand OpenDocumentDiscussionsCommand { get; } = new DelegateCommand<object>(
            x =>
            {
                if (x == null)
                {
                    return;
                }

                Entity entity = GetEntityByModelName(x.GetType().Name);

                int? id = GetDocumentId(x);

                IMessenger messenger = ServiceProvider.GetRequiredService<IMessenger>();

                messenger.Send(new DocumentDiscussionsParameter(entity.Id, id!.Value));
            },
            x =>
            {
                if (x is null || GetDocumentId(x) is null)
                {
                    return false;
                }

                Entity entity = GetEntityByModelName(x.GetType().Name);

                return entity is not null;
            });

        public static IDelegateCommand SetModuleAnalyticsPositionCommand { get; } = new DelegateCommand<object>(
            x =>
            {
                if (x == null)
                {
                    return;
                }

                IMessenger messenger = ServiceProvider.GetRequiredService<IMessenger>();

                string viewModelName = x.GetType().Name;

                string title = null;

                ISupportServices parentViewModel = null;

                if (x is TelemartDialogViewModelBase dialogViewModel)
                {
                    title = dialogViewModel.Title.ToString();
                    parentViewModel = dialogViewModel;
                }

                if (x is OrderViewModel orderViewModel)
                {
                    title = orderViewModel.Title.ToString();
                    parentViewModel = orderViewModel;
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = "Аналитика формы";
                }

                messenger.Send(new ModuleAnalyticsPositionParameter(viewModelName, title, parentViewModel));
            },
            x =>
            {
                if (x is null || GetDocumentId(x) is null)
                {
                    return false;
                }

                return true;
            });

        private static Entity GetEntityByModelName(string modelName)
        {
            IDictionaries dictionaries = ServiceProvider.GetRequiredService<IDictionaries>();

            IReadOnlyCollection<Entity> entities = dictionaries.GetItems<Entity>();

            return entities.FirstOrDefault(z => z.DiscussionViewModels?.Contains(modelName) ?? false);
        }

        private static int? GetDocumentId(object viewModel)
        {
            Type viewModelType = viewModel.GetType();

            PropertyInfo modelProperty = viewModelType.GetProperty(nameof(TelemartEditorViewModelBase<AssemblyServiceDto, AssemblyServiceParameter, AssemblyServiceViewItem>.Model));

            if (modelProperty is null)
            {
                return null;
            }

            object modelValue = modelProperty.GetValue(viewModel);

            PropertyInfo idProperty = modelValue?.GetType().GetProperty(nameof(TelemartEditorViewItemBase.Id), BindingFlags.Instance | BindingFlags.Public);

            object idValue = idProperty?.GetValue(modelValue);

            if (idValue is int id)
            {
                return id;
            }

            return null;
        }
    }
}