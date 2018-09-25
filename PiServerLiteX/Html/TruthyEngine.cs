using System;
using System.Collections.Generic;
using System.Linq;

namespace PiServerLite.Html
{
    internal static class TruthyEngine
    {
        public static bool GetValue(object value)
        {
            if (value == null) return false;

            var type = value.GetType();
            if (type == typeof(bool))
                return (bool)value;

            if (type == typeof(string))
                return ParseStringValue((string)value);

            if (type == typeof(byte))
                return (byte)value > 0;

            if (type == typeof(short))
                return (short)value > 0;

            if (type == typeof(int))
                return (int)value > 0;

            if (type == typeof(long))
                return (long)value > 0;

            if (value is IEnumerable<object> enumerableType) return enumerableType.Any();

            return true;
        }

        private static bool ParseStringValue(string stringValue)
        {
            if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(stringValue, "yes", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(stringValue, "no", StringComparison.OrdinalIgnoreCase)) return false;

            return !string.IsNullOrEmpty(stringValue);
        }
    }
}
