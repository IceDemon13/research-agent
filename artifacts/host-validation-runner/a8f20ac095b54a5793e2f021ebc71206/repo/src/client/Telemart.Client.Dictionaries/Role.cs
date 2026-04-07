namespace Telemart.Client.Dictionaries
{
    public sealed class Role : DictionaryItem
    {
        private Role(int id, string name)
            : base(id, name, true)
        {
        }

        public static Role Admin { get; } = new Role(1, "admin");

        public static Role Content { get; } = new Role(3, "content");

        public static Role Manager { get; } = new Role(4, "manager");

        public static Role Product { get; } = new Role(6, "product");

        public static Role Seller { get; } = new Role(7, "seller");

        public static Role Warehouse { get; } = new Role(8, "warehouse");

        public static Role Logist { get; } = new Role(9, "logist");

        public static Role ServiceManager { get; } = new Role(10, "service-manager");

        public static Role Operator { get; } = new Role(11, "operator-cc");

        public static Role Marketer { get; } = new Role(13, "marketer");

        public static Role Packager { get; } = new Role(14, "packager");

        public static Role Accountant { get; } = new Role(15, "accountant");

        public static Role WebUser { get; } = new Role(16, "webuser");

        public static Role System { get; } = new Role(18, "system");

        public static Role OutsourceSeller { get; } = new Role(19, "outsource-seller");

        public static Role Assembler { get; } = new Role(20, "assembler");
        
        public static Role TechSupport { get; } = new Role(21, "tech-support");
        
        public static Role TradeIn { get; } = new Role(22, "trade-in");
    }
}