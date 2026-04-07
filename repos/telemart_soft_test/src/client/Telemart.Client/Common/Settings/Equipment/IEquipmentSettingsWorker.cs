using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.Common.Settings.Equipment
{
    public interface IEquipmentSettingsWorker
    {
        Task<IReadOnlyCollection<ValidationResultItem>> EquipmentSettingsWorkAsync();
    }
}