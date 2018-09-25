using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PiServerLite.Extensions
{
    internal static class ObjectExtensions
    {
        public static IDictionary<string, object> ToDictionary(object parameters)
        {
            if (parameters == null) return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (parameters is IDictionary<string, object> dictionary) return dictionary;

            return parameters.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => new KeyValuePair<string, object>(property.Name, property.GetValue(parameters)))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
