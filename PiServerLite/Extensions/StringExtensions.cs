using System;

namespace PiServerLite.Extensions
{
    internal static class StringExtensions
    {
        public static T To<T>(this string value)
        {
            var tt = typeof(T);
            var nullableType = Nullable.GetUnderlyingType(tt);
            var isNullable = nullableType != null;

            if (string.IsNullOrEmpty(value))
            {
                if (isNullable) return (T)(object)null;
                return default(T);
            }

            if (nullableType != null)
                tt = nullableType;

            if (tt.IsEnum)
                return (T)Enum.Parse(tt, value, true);

            return (T)Convert.ChangeType(value, tt);
        }
    }
}
