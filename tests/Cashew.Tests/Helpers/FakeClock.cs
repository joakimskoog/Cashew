using System;

namespace Cashew.Tests.Helpers
{
    public class FakeClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; }
    }
}