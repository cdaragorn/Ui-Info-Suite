using System.Collections.Generic;

namespace UIInfoSuite.Extensions
{
    static class CollectionExtensions
    {

        public static TValue SafeGet<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary, Tkey key, TValue defaultValue = default(TValue))
        {
            var value = defaultValue;

            if (dictionary != null)
            {
                if (!dictionary.TryGetValue(key, out value))
                    value = defaultValue;
            }

            return value;
        }
    }
}
