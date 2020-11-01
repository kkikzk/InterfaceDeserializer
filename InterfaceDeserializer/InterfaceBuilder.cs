using System;
using System.Runtime.Serialization;

namespace InterfaceDeserializer
{
    internal class InterfaceBuilder
    {
        private static readonly ImplementationBuilder _implementationBuilder = new ImplementationBuilder();

        internal static T Create<T>(object[] fieldValues)
        {
            Type type = null;
            lock (_implementationBuilder)
            {
                if (_implementationBuilder.TypeMap.TryGetValue(typeof(T), out var generatedType))
                {
                    type = generatedType;
                }
                else
                {
                    type = _implementationBuilder.GenerateType<T>();
                }
            }
            var allFields = FormatterServices.GetSerializableMembers(type, new StreamingContext());
            var result = FormatterServices.GetUninitializedObject(type);
            return (T)FormatterServices.PopulateObjectMembers(result, allFields, fieldValues);
        }
    }
}
