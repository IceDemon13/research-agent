using System;
using System.Globalization;

namespace Telemart.Client.Dictionaries
{
    public sealed class OrganizationOwnership : DictionaryItem
    {
        private const int TovId = 1;
        private const int PrivateEnterpreneurId = 2;
        private const int IndividualId = 3;

        private OrganizationOwnership(int id, string name, string description, bool isLegal, bool active = true)
            : base(id, name, active)
        {
            Description = description;
            IsLegal = isLegal;
        }

        public static OrganizationOwnership Tov { get; } = new OrganizationOwnership(1, "ТОВ", "ТОВАРИСТВО З ОБМЕЖЕНОЮ ВІДПОВІДАЛЬНІСТЮ", true);

        public static OrganizationOwnership PrivateEnterpreneur { get; } = new OrganizationOwnership(2, "ФОП", "Фізична особа підприємець", false);

        public static OrganizationOwnership Individual { get; } = new OrganizationOwnership(3, "ФЛ", "Фізична особа", false);

        public string Description { get; }

        public bool IsLegal { get; }

        public static OrganizationOwnership GetById(int id)
        {
            OrganizationOwnership ownership;

            switch (id)
            {
                case TovId:
                    ownership = Tov;
                    break;
                case PrivateEnterpreneurId:
                    ownership = PrivateEnterpreneur;
                    break;
                case IndividualId:
                    ownership = Individual;
                    break;
                default:
                    throw new NotSupportedException($"Ownership with Id:{id.ToString(CultureInfo.InvariantCulture)} not supported.");
            }

            return ownership;
        }
    }
}
