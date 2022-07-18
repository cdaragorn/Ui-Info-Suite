using System.Collections.Generic;

namespace UIInfoSuite2.Infrastructure.Extensions
{
    public static class CollectionExtensions
    {
        public static TValue SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            TValue value = defaultValue;

            if (dictionary != null)
            {
                if (!dictionary.TryGetValue(key, out value))
                    value = defaultValue;
            }

            return value;
        }
    }
}
