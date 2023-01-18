using System;
using System.Collections.Generic;
using System.Reflection;

/// Reflector#GetPropertyGetter<TValue> provides cached readonly access to properties through reflection. 
/// Where TValue can be a supertype of the actual property type.
/// Based on SMAPI's Reflector class.
namespace UIInfoSuite2.Infrastructure.Reflection
{
    public interface IReflectedGetProperty<TValue>
    {
        PropertyInfo PropertyInfo { get; }

        TValue GetValue();
    }

    public class ReflectedGetProperty<TValue> : IReflectedGetProperty<TValue>
    {
        private readonly string DisplayName;
        private readonly Func<TValue>? GetMethod;

        public PropertyInfo PropertyInfo { get; }

        public ReflectedGetProperty(Type parentType, object? obj, PropertyInfo property, bool isStatic)
        {
            // validate input
            if (parentType == null)
                throw new ArgumentNullException(nameof(parentType));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            // validate static
            if (isStatic && obj != null)
                throw new ArgumentException("A static property cannot have an object instance.");
            if (!isStatic && obj == null)
                throw new ArgumentException("A non-static property must have an object instance.");


            this.DisplayName = $"{parentType.FullName}::{property.Name}";
            this.PropertyInfo = property;

            if (this.PropertyInfo.GetMethod != null)
            {
                try
                {
                    this.GetMethod = (Func<TValue>) Delegate.CreateDelegate(typeof(Func<TValue>), obj, this.PropertyInfo.GetMethod);
                }
                catch (ArgumentException)
                {
                    if (this.PropertyInfo.PropertyType.IsEnum)
                    {
                        Type enumType = this.PropertyInfo.PropertyType;
                        this.GetMethod = delegate () {
                            var enumDelegate = Delegate.CreateDelegate(typeof(Func<>).MakeGenericType(Enum.GetUnderlyingType(enumType)), obj, this.PropertyInfo.GetMethod);
                            return (TValue) Enum.ToObject(enumType, enumDelegate.DynamicInvoke()!);
                        };
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public TValue GetValue()
        {
            if (this.GetMethod == null)
                throw new InvalidOperationException($"The {this.DisplayName} property has no get method.");
            
            try
            {
                return this.GetMethod();
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Can't convert the {this.DisplayName} property from {this.PropertyInfo.PropertyType.FullName} to {typeof(TValue).FullName}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't get the value of the {this.DisplayName} property", ex);
            }
        }
    }

    public class Reflector
    {
        public class IntervalMemoryCache<TKey, TValue>
            where TKey : notnull
        {
            private Dictionary<TKey, TValue> HotCache = new();
            private Dictionary<TKey, TValue> StaleCache = new();

            public TValue GetOrSet(TKey cacheKey, Func<TValue> get)
            {
                // from hot cache
                if (this.HotCache.TryGetValue(cacheKey, out TValue? value))
                    return value;

                // from stale cache
                if (this.StaleCache.TryGetValue(cacheKey, out value))
                {
                    this.HotCache[cacheKey] = value;
                    return value;
                }

                // new value
                value = get();
                this.HotCache[cacheKey] = value;
                return value;
            }

            public void StartNewInterval()
            {
                this.StaleCache.Clear();
                if (this.HotCache.Count is not 0)
                    (this.StaleCache, this.HotCache) = (this.HotCache, this.StaleCache); // swap hot cache to stale
            }
        }

        private readonly IntervalMemoryCache<string, MemberInfo?> Cache = new();

        public void NewCacheInterval()
        {
            this.Cache.StartNewInterval();
        }

        public IReflectedGetProperty<TValue> GetPropertyGetter<TValue>(object obj, string name, bool required = true)
        {
            // validate
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Can't get a instance property from a null object.");

            // get property from hierarchy
            IReflectedGetProperty<TValue>? property = this.GetGetPropertyFromHierarchy<TValue>(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (required && property == null)
                throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a '{name}' instance property.");
            return property!;
        }

        private IReflectedGetProperty<TValue>? GetGetPropertyFromHierarchy<TValue>(Type type, object? obj, string name, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            PropertyInfo? property = this.GetCached(
                'p', type, name, isStatic,
                fetch: () =>
                {
                    for (Type? curType = type; curType != null; curType = curType.BaseType)
                    {
                        PropertyInfo? propertyInfo = curType.GetProperty(name, bindingFlags);
                        if (propertyInfo != null)
                        {
                            type = curType;
                            return propertyInfo;
                        }
                    }

                    return null;
                }
            );

            return property != null
                ? new ReflectedGetProperty<TValue>(type, obj, property, isStatic)
                : null;
        }

        private TMemberInfo? GetCached<TMemberInfo>(char memberType, Type type, string memberName, bool isStatic, Func<TMemberInfo?> fetch)
            where TMemberInfo : MemberInfo
        {
            string key = $"{memberType}{(isStatic ? 's' : 'i')}{type.FullName}:{memberName}";
            return (TMemberInfo?) this.Cache.GetOrSet(key, fetch);
        }
    }
}
