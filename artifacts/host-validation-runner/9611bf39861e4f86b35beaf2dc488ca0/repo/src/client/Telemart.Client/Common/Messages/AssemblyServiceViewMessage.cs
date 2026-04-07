namespace Telemart.Client.Common.Messages
{
    public sealed class AssemblyServiceViewMessage
    {
        public AssemblyServiceViewMessage(int assemblyServiceId)
        {
            AssemblyServiceId = assemblyServiceId;
        }

        public int AssemblyServiceId { get; }
    }
}