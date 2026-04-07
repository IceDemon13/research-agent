using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.AssemblyTest;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.AssemblyTest;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.TreeStructure;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public sealed class AssemblyServiceTestViewModel : TelemartDialogViewModelBase
    {
        // all tests without tree structure
        private AssemblyTestsViewItem[] allTests;
        private int assemblyServiceId;

        public AssemblyServiceTestViewModel(IWebClient webClient, IMessageFacadeService messageFacadeService, IDictionaries dictionaries, IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;

            SaveCommand = new AsyncCommand(SaveAsync);
        }

        public AssemblyServiceTestViewModel()
        {
        }

        public IAsyncCommand SaveCommand { get; }

        public ObservableCollection<AssemblyTestGroupViewItem> TestGroups
        {
            get { return GetProperty(() => TestGroups); }
            set { SetProperty(() => TestGroups, value); }
        }

        public bool IsFormEditable
        {
            get { return GetProperty(() => IsFormEditable); }
            set { SetProperty(() => IsFormEditable, value); }
        }

        #region DialogSettings

        public override int MinWidth => 750;

        public override int Width => 1000;

        public override int MaxWidth => 1920;

        public override int MinHeight => 400;

        public override int Height => 600;

        public override int MaxHeight => 1080;

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            AssemblyServiceTestParameter parameter = (AssemblyServiceTestParameter)Parameter;
            assemblyServiceId = parameter.Id;
            IsFormEditable = parameter.IsFormEditable;

            try
            {
                Task<List<AssemblyTestGroupDto>> assemblyTestGroupsTask = WebClient.ExecuteApiRequestAsync(new QueryAssemblyTestGroups());
                Task<List<AssemblyTestResultDto>> testResultsTask = WebClient.ExecuteApiRequestAsync(new QueryAssemblyTestResults(new AssemblyServiceTestResultsFilteringItem(assemblyServiceId)));

                (List<AssemblyTestGroupDto> AssemblyTestGroups, List<AssemblyTestResultDto> TestResults) result = await TaskExt.WhenAll(assemblyTestGroupsTask, testResultsTask);

                AssemblyTestGroupViewItem[] groups = Mapper.Map<AssemblyTestGroupViewItem[]>(result.AssemblyTestGroups.OrderBy(x => x.Position));

                foreach (AssemblyTestGroupViewItem group in groups)
                {
                    if (parameter.IsFormEditable)
                    {
                        group.Tests.RemoveAll(x => !x.Active);
                    }
                    else
                    {
                        group.Tests.RemoveAll(x => result.TestResults.All(y => y.TestId != x.Id));
                    }

                    group.Tests = group.Tests.OrderBy(x => x.Position).ToObservableCollection();
                }

                AssemblyTestGroupViewItem[] groupsWithTests = groups.Where(x => x.Tests?.Any() == true).ToArray();

                allTests = groupsWithTests.SelectMany(x => x.Tests).ToArray();

                Dictionary<int, AssemblyTestResultDto> values = result.TestResults.ToDictionary(x => x.TestId);

                SetValues(values);

                ObservableCollection<AssemblyTestGroupViewItem> testGroups = TreeStructureHelper.GetGroupTree(groups).ToObservableCollection();

                TreeStructureHelper.RemoveByCondition(testGroups, x => x.Groups?.Any() != true && x.Tests?.Any() != true);

                TestGroups = testGroups;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to test assembly service");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            Title = IsFormEditable ? "Тестирование сборки" : "Результаты тестов";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            await SaveAsync();
            Close();
        }

        private void SetValues(IDictionary<int, AssemblyTestResultDto> values)
        {
            foreach (AssemblyTestsViewItem test in allTests)
            {
                if (values.TryGetValue(test.Id, out AssemblyTestResultDto value))
                {
                    test.SetValue(value.Value, value.Required);
                }
            }
        }

        private async Task SaveAsync()
        {
            if (allTests.All(x => x.IsValueChanged is false))
            {
                MessageFacadeService.ShowNotificationInfo("Нечего сохранять");
                return;
            }

            List<AssemblyTestResultDto> results = MapCreateDto();

            try
            {
                Result<List<AssemblyTestResultDto>> result = await WebClient.ExecuteApiRequestAsync(new SetTestResults(results, assemblyServiceId));

                Dictionary<int, AssemblyTestResultDto> values = result.Data.ToDictionary(x => x.TestId);

                SetValues(values);

                MessageFacadeService.ShowNotificationInfo("Тесты успешно сохранены");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save entity");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving entity");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }
        }

        private List<AssemblyTestResultDto> MapCreateDto()
        {
            List<AssemblyTestResultDto> assemblyTestResults = new List<AssemblyTestResultDto>();

            foreach (AssemblyTestsViewItem test in allTests)
            {
                assemblyTestResults.Add(new AssemblyTestResultDto()
                {
                    AssemblyServiceId = assemblyServiceId,
                    TestId = test.Id,
                    Value = test.Value
                });
            }

            return assemblyTestResults;
        }
    }
}