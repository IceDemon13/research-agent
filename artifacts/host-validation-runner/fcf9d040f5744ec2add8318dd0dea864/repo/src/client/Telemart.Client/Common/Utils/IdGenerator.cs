namespace Telemart.Client.Common.Utils
{
    public class IdGenerator : IIdGenerator
    {
        private int current;

        public int GetNext()
        {
            current = current - 1;

            return current;
        }
    }
}
