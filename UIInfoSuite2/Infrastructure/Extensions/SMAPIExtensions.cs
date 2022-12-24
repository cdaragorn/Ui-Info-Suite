using StardewModdingAPI;
using UIInfoSuite2.Infrastructure.Reflection;

namespace UIInfoSuite2.Infrastructure.Extensions
{
    public static class SMAPIReflectorExtensions
    {
        private static Reflector? _reflector;
        public static Reflector Reflector
        {
            get => _reflector ??= new Reflector();
        }

        public static IReflectedGetProperty<TValue> GetPropertyGetter<TValue>(this IReflectionHelper _this, object obj, string name, bool required = true)
        {
            return Reflector.GetPropertyGetter<TValue>(obj, name, required);
        }
    }
}
