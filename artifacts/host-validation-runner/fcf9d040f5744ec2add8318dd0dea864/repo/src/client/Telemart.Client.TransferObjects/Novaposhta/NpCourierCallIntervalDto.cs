using System;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public sealed class NpCourierCallIntervalDto
    {
        public string Number { get; init; }

        public TimeOnly Start { get; init; }

        public TimeOnly End { get; init; }

        public TimeOnly BoundaryTime { get; init; }
    }
}
