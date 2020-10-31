using System;

namespace InterfaceDeserializer
{
    public class SerializerKeyAttribute : Attribute
    {
        public int Id { get; }

        public SerializerKeyAttribute(int id)
        {
            Id = id;
        }
    }
}
