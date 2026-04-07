using System;

namespace Telemart.Client.Data.Requests.Features.Report.TransferObjects
{
    public class ReportFieldDto : IEquatable<ReportFieldDto>
    {
        public string UniqueName { get; set; }

        public string Area { get; set; }

        public string Caption { get; set; }

        public string Description { get; set; }

        public string CellFormat { get; set; }

        public string FieldName { get; set; }

        public string GroupInterval { get; set; }

        public string SummaryType { get; set; }

        public string DisplayFormat { get; set; }

        public string EntityType { get; set; }

        public string FilterPopupMode { get; set; }

        public static bool operator !=(ReportFieldDto left, ReportFieldDto right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(ReportFieldDto left, ReportFieldDto right)
        {
            return Equals(left, right);
        }

        public bool Equals(ReportFieldDto other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Area, other.Area, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Caption, other.Caption, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Description, other.Description, StringComparison.OrdinalIgnoreCase)
                && string.Equals(CellFormat, other.CellFormat, StringComparison.OrdinalIgnoreCase)
                && string.Equals(FieldName, other.FieldName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(GroupInterval, other.GroupInterval, StringComparison.OrdinalIgnoreCase)
                && string.Equals(SummaryType, other.SummaryType, StringComparison.OrdinalIgnoreCase)
                && string.Equals(DisplayFormat, other.DisplayFormat, StringComparison.OrdinalIgnoreCase)
                && string.Equals(EntityType, other.EntityType, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ReportFieldDto)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Area != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Area) : 0;

                hashCode = (hashCode * 397) ^ (Caption != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Caption) : 0);
                hashCode = (hashCode * 397) ^ (Description != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Description) : 0);
                hashCode = (hashCode * 397) ^ (CellFormat != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(CellFormat) : 0);
                hashCode = (hashCode * 397) ^ (FieldName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(FieldName) : 0);
                hashCode = (hashCode * 397) ^ (GroupInterval != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(GroupInterval) : 0);
                hashCode = (hashCode * 397) ^ (SummaryType != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(SummaryType) : 0);
                hashCode = (hashCode * 397) ^ (DisplayFormat != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(DisplayFormat) : 0);
                hashCode = (hashCode * 397) ^ (EntityType != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(EntityType) : 0);

                return hashCode;
            }
        }
    }
}