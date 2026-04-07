using System.Collections.Generic;

namespace Telemart.Client.Dictionaries
{
    public sealed class ActiveValue
    {
        private ActiveValue(double id, string name)
        {
            Id = id;
            Name = name;
        }

        public double Id { get; }

        public string Name { get; }

        public static IEnumerable<ActiveValue> GetCategoryAvailableValues()
        {
            yield return new ActiveValue(0, "Нет");
            yield return new ActiveValue(0.5, "Скрыта");
            yield return new ActiveValue(1, "Да");
        }

        public static IEnumerable<ActiveValue> GetProductCardAvailableValues()
        {
            yield return new ActiveValue(0, "Нигде");
            yield return new ActiveValue(0.5, "B2B");
            yield return new ActiveValue(1, "Telemart.ua");
        }
    }
}
