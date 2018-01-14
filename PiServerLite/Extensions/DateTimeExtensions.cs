using System;

namespace PiServerLite.Extensions
{
    internal static class DateTimeExtensions
    {
        public static DateTime TrimMilliseconds(this DateTime value)
        {
            return new DateTime(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), value.Kind);
        }
    }
}
