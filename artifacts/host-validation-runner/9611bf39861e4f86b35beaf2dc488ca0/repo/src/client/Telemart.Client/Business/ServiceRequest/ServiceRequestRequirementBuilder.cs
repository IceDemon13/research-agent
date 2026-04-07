using System;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.ServiceRequest
{
    internal sealed class ServiceRequestRequirementBuilder
    {
        private readonly ServiceRequestRequirement _requirement;
        private readonly int? _repairDays;
        private readonly int? _serviceRepairTypeId;
        private readonly int? _changeOnProductId;
        private readonly string _changeOnProductName;
        private readonly Payment _payment;
        private readonly string _cashbox;

        public ServiceRequestRequirementBuilder(
            ServiceRequestRequirement requirement,
            int? repairDays,
            int? serviceRepairTypeId,
            int? changeOnProductId,
            string changeOnProductName,
            Payment payment,
            string cashbox)
        {
            _requirement = requirement;
            _repairDays = repairDays;
            _changeOnProductId = changeOnProductId;
            _changeOnProductName = changeOnProductName;
            _payment = payment;
            _cashbox = cashbox;
            _serviceRepairTypeId = serviceRepairTypeId;
        }

        public string GetRequirementText()
        {
            string requirementText;

            switch (_requirement.Id)
            {
                case ServiceRequestRequirement.RepairId:

                    if (_serviceRepairTypeId == ServiceRepairType.Warranty.Id || _serviceRepairTypeId is null)
                    {
                        requirementText = _repairDays.HasValue
                            ? $"{ServiceRepairType.Warranty.Name}, {_repairDays} дней"
                            : ServiceRepairType.Warranty.Name;
                    }
                    else
                    {
                        requirementText = ServiceRepairType.Paid.Name;
                    }

                    break;
                case ServiceRequestRequirement.ChangeId:
                    requirementText = _changeOnProductId.HasValue
                        ? $"{_changeOnProductName} ({_changeOnProductId.Value})"
                        : string.Empty;
                    break;
                case ServiceRequestRequirement.ReturnMoneyId:
                    requirementText = GetReturnMoneyString();
                    break;

                case ServiceRequestRequirement.TradeInId:
                    requirementText = null;
                    break;
                default:
                    throw new NotSupportedException();
            }

            return requirementText;
        }

        private string GetReturnMoneyString()
        {
            if (_payment is null && string.IsNullOrWhiteSpace(_cashbox))
            {
                return "Способ возврата определяется автоматически";
            }

            string result = $"{_payment?.Name}";

            if (_payment?.Id == Payment.CashId)
            {
                result += $". Касса: {_cashbox}.";
            }

            return result;
        }
    }
}