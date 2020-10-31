using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InterfaceDeserializer
{
    internal static class SerizlizerKeyAttibuteCollector
    {
        internal class KeyDetail
        {
            internal int Key { set; get; }
            internal PropertyInfo PropertyInfo { set; get; }
        }

        internal static Dictionary<int, KeyDetail> Collect(Type type)
        {
            var interfaces = new List<Type>();
            if (type.IsInterface)
            {
                interfaces.Add(type);
            }
            interfaces.AddRange(type.GetInterfaces());
            return CollectInternal(interfaces);
        }

        private static Dictionary<int, KeyDetail> CollectInternal(List<Type> types)
        {
            var keys = new Dictionary<int, KeyDetail>();
            foreach (var it in types)
            {
                foreach (var propertyInfo in it.GetProperties())
                {
                    propertyInfo.GetCustomAttributes(true);
                    var targetAttrs = propertyInfo.GetCustomAttributes(true).Where(_ => _.GetType() == typeof(SerializerKeyAttribute));
                    if (targetAttrs.Any())
                    {
                        var targetAttr = (SerializerKeyAttribute)targetAttrs.First();
                        keys.Add(targetAttr.Id, new KeyDetail() { Key = targetAttr.Id, PropertyInfo = propertyInfo });
                    }
                }
            }
            return keys;
        }
    }
}
