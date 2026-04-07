using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Newtonsoft.Json;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.Settings.Equipment
{
    public class EquipmentSettingsWorker : IEquipmentSettingsWorker
    {
        private readonly IFiscalRegistrarSettingsChecker _fiscalRegistrarSettingsChecker;
        private readonly IPosSettingsChecker _posSettingsChecker;
        private readonly IEquipmentSettingsStore _equipmentSettingsStore;

        public EquipmentSettingsWorker(
            IEquipmentSettingsStore equipmentSettingsStore,
            IPosSettingsChecker posSettingsChecker,
            IFiscalRegistrarSettingsChecker fiscalRegistrarSettingsChecker)
        {
            _fiscalRegistrarSettingsChecker = fiscalRegistrarSettingsChecker;
            _posSettingsChecker = posSettingsChecker;
            _equipmentSettingsStore = equipmentSettingsStore;
        }

        public async Task<IReadOnlyCollection<ValidationResultItem>> EquipmentSettingsWorkAsync()
        {
            EquipmentSettingsInfo equipmentSettings = await _equipmentSettingsStore.LoadAsync();

            Result<bool> posSettingsCheckResult = await _posSettingsChecker.CheckPosSettingAsync(equipmentSettings.UniqueDeviceGuid,  equipmentSettings.Pos);

            Result<bool> rroSettingsCheckResult = await _fiscalRegistrarSettingsChecker.CheckFiscalRegistrarSettingAsync(equipmentSettings.UniqueDeviceGuid, equipmentSettings.FiscalRegistrar);

            List<ValidationResultItem> validationResultItems = new List<ValidationResultItem>();

            if (!posSettingsCheckResult.IsSuccess)
            {
                IEnumerable<string> errors = posSettingsCheckResult.ErrorObj?.GetMessages();

                if (errors?.Any() == true)
                {
                    validationResultItems.AddRange(errors.Select(x => new ValidationResultItem(x, true)));
                }
            }

            if (!rroSettingsCheckResult.IsSuccess)
            {
                IEnumerable<string> errors = rroSettingsCheckResult.ErrorObj?.GetMessages();

                if (errors?.Any() == true)
                {
                    validationResultItems.AddRange(errors.Select(x => new ValidationResultItem(x, true)));
                }
            }

            if (validationResultItems.Any())
            {
                validationResultItems.Add(new ValidationResultItem(JsonConvert.SerializeObject(equipmentSettings), false));
            }

            if (posSettingsCheckResult.Data || rroSettingsCheckResult.Data)
            {
                if (rroSettingsCheckResult.Data)
                {
                    equipmentSettings.FiscalRegistrar.Clean();
                }

                if (posSettingsCheckResult.Data)
                {
                    equipmentSettings.Pos.Clean();
                }

                await _equipmentSettingsStore.SaveAsync(equipmentSettings);
            }

            return validationResultItems.ToReadOnlyObservableCollection();
        }
    }
}