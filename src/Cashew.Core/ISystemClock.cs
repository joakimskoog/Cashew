using System;
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Cashew.Core.Tests")]

namespace Cashew.Core
{
    
    internal interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
    }

    internal class DefaultSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}