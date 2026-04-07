using System;
using System.Threading.Tasks;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Common.Settings.Equipment
{
    public interface IFiscalRegistrarSettingsChecker
    {
        Task<Result<bool>> CheckFiscalRegistrarSettingAsync(Guid? uniqueDeviceGuid, FiscalRegistrarSettingsInfo rroSettingsInfo);
    }
}