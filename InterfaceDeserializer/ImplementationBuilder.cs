using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace InterfaceDeserializer
{
    internal class ImplementationBuilder
    {
        private readonly AssemblyName _assemblyName;
        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly List<string> _typeNames = new List<string>();

        public Dictionary<Type, Type> TypeMap { get; } = new Dictionary<Type, Type>();

        public ImplementationBuilder()
        {
            _assemblyName = new AssemblyName($"<>_{Guid.NewGuid()}_Assembly");
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(_assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule("<>_Implementations");
        }

        internal bool CanBuild(Type objectType)
        {
            if (!objectType.IsInterface)
            {
                return false;
            }
            if (objectType.GetMethods().Except(
                objectType.GetProperties().SelectMany(p => new[] { p.GetGetMethod(), p.GetSetMethod() })).Any(m => m != null))
            {
                return false;
            }
            return true;
        }

        public Type GenerateType<T>()
        {
            if (!CanBuild(typeof(T)))
            {
                throw new ArgumentException($"Cannot build type for {typeof(T).Name}");
            }

            if (TypeMap.TryGetValue(typeof(T), out var generatedType))
            {
                return generatedType;
            }

            var typeBuilder = _moduleBuilder.DefineType(MakeImplName(typeof(T)),
                TypeAttributes.Class | TypeAttributes.NotPublic, typeof(object), new Type[] { typeof(T) });
            var constructorInfo = typeof(SerializableAttribute).GetConstructor(new Type[] { });
            var attrBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { });
            typeBuilder.SetCustomAttribute(attrBuilder);

            var properties = GetProperties(typeof(T));
            var fields = new List<FieldBuilder>();

            foreach (var property in properties)
            {
                GenerateProperty(typeBuilder, fields, property);
            }

            GenerateConstructor(typeBuilder, fields, properties);
            var type = typeBuilder.CreateType();
            TypeMap.Add(typeof(T), type);
            return type;
        }

        private List<PropertyInfo> GetProperties(Type interfaceType)
        {
            var result = new List<PropertyInfo>();
            foreach (var it in interfaceType.GetInterfaces())
            {
                result.AddRange(GetProperties(it));
            }
            result.AddRange(interfaceType.GetProperties());
            return result;
        }

        private string MakeImplName(Type interfaceType)
        {
            lock (_moduleBuilder)
            {
                var index = 0;
                while (true)
                {
                    var candidate = $"{interfaceType.Namespace}.{interfaceType.Name}Impl{(index == 0 ? string.Empty : index.ToString())}";
                    if (_typeNames.Contains(candidate))
                    {
                        ++index;
                        continue;
                    }
                    _typeNames.Add(candidate);
                    return candidate;
                }
            }
        }

        private void GenerateProperty(TypeBuilder typeBuilder, List<FieldBuilder> fields, PropertyInfo property)
        {
            var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
            var fieldBuilder = typeBuilder.DefineField($"_{property.Name}", GetFieldTypeForProperty(property), FieldAttributes.Private);
            fields.Add(fieldBuilder);
            if (property.GetGetMethod() != null)
            {
                GenerateGetter(typeBuilder, property, fieldBuilder, propertyBuilder);
            }
            if (property.GetSetMethod() != null)
            {
                GenerateSetter(typeBuilder, property, fieldBuilder, propertyBuilder);
            }
        }

        private Type GetFieldTypeForProperty(PropertyInfo property)
        {
            if (TypeMap.TryGetValue(property.PropertyType, out var fieldType))
            {
                return fieldType;
            }

            return property.PropertyType;
        }

        private void GenerateConstructor(TypeBuilder typeBuilder, List<FieldBuilder> fields, List<PropertyInfo> properties)
        {
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, properties.Select(p => p.PropertyType).ToArray());
            for (var i = 0; i < properties.Count; i++)
            {
                ctor.DefineParameter(i + 1, ParameterAttributes.None, properties[i].Name);
            }
            var ctorIL = ctor.GetILGenerator();
            for (var i = 0; i < fields.Count; i++)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);
                ctorIL.Emit(OpCodes.Ldarg, i + 1);
                ctorIL.Emit(OpCodes.Stfld, fields[i]);
            }
            ctorIL.Emit(OpCodes.Ret);
        }

        private PropertyBuilder GenerateGetter(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            var attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var getterBuilder = typeBuilder.DefineMethod(property.GetGetMethod().Name, attributes, property.PropertyType, Type.EmptyTypes);
            var getterIL = getterBuilder.GetILGenerator();
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIL.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(getterBuilder, property.GetGetMethod());
            propertyBuilder.SetGetMethod(getterBuilder);
            return propertyBuilder;
        }

        private void GenerateSetter(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder fieldBuilder, PropertyBuilder propertyBuilder)
        {
            var attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;
            var setterBuilder = typeBuilder.DefineMethod(property.GetSetMethod().Name, attributes, null, new Type[] { property.PropertyType });
            var setterIL = setterBuilder.GetILGenerator();
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            setterIL.Emit(OpCodes.Stfld, fieldBuilder);
            setterIL.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(setterBuilder, property.GetSetMethod());
            propertyBuilder.SetSetMethod(setterBuilder);
        }
    }
}
