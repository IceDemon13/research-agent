using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.ViewModels.Service.ServiceMovements;

namespace Telemart.Client.Common.Calculators
{
    public interface IInsuranceCalculator
    {
        Task<decimal> CalculateByServiceMovementAsync(ServiceMovementProductViewItem[] products);
    }
}