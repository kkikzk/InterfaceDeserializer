using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace InterfaceDeserializer
{
    internal class InterfaceBuilder
    {
        private static readonly ImplementationBuilder _implementationBuilder = new ImplementationBuilder();

        internal Type Type { get; }
        internal MemberInfo[] AllFields { get; }

        internal InterfaceBuilder(Type type)
        {
            lock (this)
            {
                if (_implementationBuilder.TypeMap.TryGetValue(type, out var generatedType))
                {
                    Type = generatedType;
                }
                else
                {
                    Type = _implementationBuilder.GenerateType(type);
                }
            }
            AllFields = FormatterServices.GetSerializableMembers(Type, new StreamingContext());
        }

        internal object Create(object[] fieldValues)
        {
            var result = FormatterServices.GetUninitializedObject(Type);
            return FormatterServices.PopulateObjectMembers(result, AllFields, fieldValues);
        }
    }
}
