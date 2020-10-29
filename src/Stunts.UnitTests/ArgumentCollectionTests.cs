using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace Stunts.UnitTests
{
    public class ArgumentCollectionTests
    {
        static readonly MethodInfo valueTypeMethod = typeof(ArgumentCollectionTests).GetMethod(nameof(DoValueType), BindingFlags.Static | BindingFlags.Public)!;
        static readonly MethodInfo nullableValueTypeMethod = typeof(ArgumentCollectionTests).GetMethod(nameof(DoNullableValueType), BindingFlags.Static | BindingFlags.Public)!;
        static readonly MethodInfo referenceTypeMethod = typeof(ArgumentCollectionTests).GetMethod(nameof(DoReferenceType), BindingFlags.Static | BindingFlags.Public)!;

        [Fact]
        public void ThrowsIfMismatchValuesLength()
            => Assert.Throws<ArgumentException>(() => new ArgumentCollection(new object[] { 5, "Foo" }, valueTypeMethod.GetParameters()));

        [Fact]
        public void ThrowsIfGetNameNotFound()
            => Assert.Throws<KeyNotFoundException>(() => new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters())["foo"]);

        [Fact]
        public void ThrowsIfSetNameNotFound()
            => Assert.Throws<KeyNotFoundException>(() => new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters())["foo"] = 1);

        [Fact]
        public void ThrowsIfGetInfoByNameNotFound()
            => Assert.Throws<KeyNotFoundException>(() => new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).GetInfo("foo"));

        [Fact]
        public void AccessValueByIndex()
        {
            var arguments = new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters());

            Assert.Equal(5, arguments[0]);

            arguments[0] = 10;

            Assert.Equal(10, arguments[0]);
        }

        [Fact]
        public void AccessValueByName()
        {
            var arguments = new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters());

            Assert.Equal(5, arguments["value"]);

            arguments["value"] = 10;

            Assert.Equal(10, arguments["value"]);
        }

        [Fact]
        public void ContainsByName() 
            => Assert.True(new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).Contains("value"));

        [Fact]
        public void GetNameFromIndex()
            => Assert.Equal("value", new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).NameOf(0));

        [Fact]
        public void GetIndexFromName()
            => Assert.Equal(0, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).IndexOf("value"));


        [Fact]
        public void GetInfoFromIndex()
            => Assert.NotNull(new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).GetInfo(0));

        [Fact]
        public void GetInfoFromName()
            => Assert.NotNull(new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).GetInfo("value"));

        [Fact]
        public void EnumerateGeneric()
            => Assert.Collection(new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()), value => Assert.Equal(5, value));

        [Fact]
        public void Enumerate()
        {
            IEnumerable arguments = new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters());
            foreach (var value in arguments)
            {
                Assert.Equal(5, value);
            }
        }

        [Fact]
        public void Count()
            => Assert.Equal(1, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).Count);


        [Fact]
        public void GetTyped()
        {
            Assert.Equal(5, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).Get<int>(0));
            Assert.Equal(5, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).Get<int>("value"));

            // If nullable annotations are ignored and we request a nullable int, it should still work.
#nullable disable
            Assert.Equal((int?)null, new ArgumentCollection(new object[] { null }, nullableValueTypeMethod.GetParameters()).Get<int?>(0));
            Assert.Equal((int?)null, new ArgumentCollection(new object[] { null }, nullableValueTypeMethod.GetParameters()).Get<int?>("value"));
#nullable restore

            Assert.Equal("foo", new ArgumentCollection(new object[] { "foo" }, referenceTypeMethod.GetParameters()).Get<string>(0));
            Assert.Equal("foo", new ArgumentCollection(new object[] { "foo" }, referenceTypeMethod.GetParameters()).Get<string>("value"));
        }

        [Fact]
        public void GetOptionalTyped()
        {
            Assert.Equal(5, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).GetNullable<int>(0));
            Assert.Equal(5, new ArgumentCollection(new object[] { 5 }, valueTypeMethod.GetParameters()).GetNullable<int>("value"));

            Assert.Equal((int?)null, new ArgumentCollection(new object?[] { null }, nullableValueTypeMethod.GetParameters()).GetNullable<int?>(0));
            Assert.Equal((int?)null, new ArgumentCollection(new object?[] { null }, nullableValueTypeMethod.GetParameters()).GetNullable<int?>("value"));
                
            Assert.Equal("foo", new ArgumentCollection(new object[] { "foo" }, referenceTypeMethod.GetParameters()).GetNullable<string>(0));
            Assert.Equal("foo", new ArgumentCollection(new object[] { "foo" }, referenceTypeMethod.GetParameters()).GetNullable<string>("value"));

            Assert.Null(new ArgumentCollection(new object?[] { null }, referenceTypeMethod.GetParameters()).GetNullable<string>(0));
            Assert.Null(new ArgumentCollection(new object?[] { null }, referenceTypeMethod.GetParameters()).GetNullable<string>("value"));
        }

        [Fact]
        public void GetTypedThrowsIfNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ArgumentCollection(new object?[] { null }, valueTypeMethod.GetParameters()).Get<int>(0));
            Assert.Throws<ArgumentNullException>(() => new ArgumentCollection(new object?[] { null }, valueTypeMethod.GetParameters()).Get<int>("value"));

            Assert.Throws<ArgumentNullException>(() => new ArgumentCollection(new object?[] { null }, valueTypeMethod.GetParameters()).Get<string>(0));
            Assert.Throws<ArgumentNullException>(() => new ArgumentCollection(new object?[] { null }, valueTypeMethod.GetParameters()).Get<string>("value"));
        }

        [Fact]
        public void GetTypedThrowsIfIncompatible()
        {
            Assert.Throws<ArgumentException>(() => new ArgumentCollection(new object[] { "foo" }, valueTypeMethod.GetParameters()).Get<int>(0));
            Assert.Throws<ArgumentException>(() => new ArgumentCollection(new object[] { "foo" }, valueTypeMethod.GetParameters()).Get<int>("value"));
        }

        public static void DoValueType(int value) { }
        public static void DoNullableValueType(int? value) { }
        public static void DoReferenceType(string value) { }
    }
}
