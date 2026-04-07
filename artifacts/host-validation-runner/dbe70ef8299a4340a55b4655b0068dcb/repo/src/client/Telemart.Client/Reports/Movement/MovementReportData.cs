using System;
using System.Collections.Generic;

namespace Telemart.Client.ReportDesigner
{
    public class MovementReportData
    {
        public MovementReportData(
            int movementId,
            int stateId,
            DateTime createdOn,
            string warehouseFromName,
            string warehouseToName,
            DateTime dateTimeAssemblyPrint,
            IReadOnlyCollection<MovementProductGroupReportData> groups)
        {
            MovementId = movementId;
            StateId = stateId;
            CreatedOn = createdOn;
            WarehouseFromName = warehouseFromName;
            WarehouseToName = warehouseToName;
            DateTimeAssemblyPrint = $"Дата печати: {dateTimeAssemblyPrint:dd.MM.yyyy HH:mm}";
            Groups = groups;
        }

        public int MovementId { get; }

        public int StateId { get; }

        public DateTime CreatedOn { get; }

        public string CreatedOnFormatted => CreatedOn.ToString("dd.MM.yyyy");

        public string WarehouseFromName { get; }

        public string WarehouseToName { get; }

        public string DateTimeAssemblyPrint { get; }

        public IReadOnlyCollection<MovementProductGroupReportData> Groups { get; }
    }
}