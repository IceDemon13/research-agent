using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryLockInfo
    {
        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        public override string ToString()
        {
            return $"{CategoryName} ({EmployeeLockName})";
        }
    }
}