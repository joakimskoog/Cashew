using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Cashew.Tests")]

namespace Cashew
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