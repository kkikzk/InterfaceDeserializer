using System;
using System.Text;
using Xunit;

namespace InterfaceDeserializer.Test
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var testData = "abcdefgあいうえお";
            var base63string = Convert.ToBase64String(Encoding.UTF8.GetBytes(testData));
            var encrypted = Encrypter.Encrypt(Encoding.ASCII.GetBytes(base63string));
            var actual = Encoding.ASCII.GetString(Encrypter.Decrypt(encrypted));

            Assert.Equal("abcdefgあいうえお", actual);
        }

        public interface ITest
        {
            [SerializerKey(1)]
            int Count { set; get; }

            [SerializerKey(0)]
            IInternalTest Internal { get; }
        }

        public interface IInternalTest
        {
            [SerializerKey(2)]
            string Name { set; get; }

            [SerializerKey(3)]
            IInternalTest2 Internal2 { get; }
        }

        public interface IInternalTest2
        {
            [SerializerKey(4)]
            bool Bool { get; }
        }

        [Fact]
        public void Test2()
        {
            var internalTest2 = InterfaceBuilder.Create<IInternalTest2>(new object[] { true });
            var internalTest = InterfaceBuilder.Create<IInternalTest>(new object[] { "name", internalTest2 });
            var actual = InterfaceBuilder.Create<ITest>(new object[] { 5, internalTest });

            Assert.Equal(5, actual.Count);
            Assert.Equal("name", actual.Internal.Name);
            Assert.True(actual.Internal.Internal2.Bool);
        }
    }
}
