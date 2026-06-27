using FluentAssertions;
using Paperless.Shared.Abstractions;

namespace Paperless.Shared.Tests;

public class ValueObjectTests
{
    private sealed class TestValueObject : ValueObject
    {
        public string Name { get; }
        public int Age { get; }

        public TestValueObject(string name, int age)
        {
            Name = name;
            Age = age;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Name;
            yield return Age;
        }
    }

    private sealed class AnotherValueObject : ValueObject
    {
        public string Value { get; }

        public AnotherValueObject(string value)
        {
            Value = value;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }

    [Fact]
    public void ValueObject_Equal_WhenAllComponentsMatch()
    {
        var a = new TestValueObject("Alice", 30);
        var b = new TestValueObject("Alice", 30);

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ValueObject_NotEqual_WhenAnyComponentDiffers()
    {
        var a = new TestValueObject("Alice", 30);
        var b = new TestValueObject("Bob", 30);
        var c = new TestValueObject("Alice", 25);

        a.Should().NotBe(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void ValueObject_NotEqual_WhenComparedToDifferentType()
    {
        var a = new TestValueObject("Alice", 30);
        var b = new AnotherValueObject("Alice");

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_OperatorOverloads_WorkCorrectly()
    {
        var a = new TestValueObject("Alice", 30);
        var b = new TestValueObject("Alice", 30);
        var c = new TestValueObject("Bob", 30);

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();

        (null as TestValueObject == null).Should().BeTrue();
        (null as TestValueObject == a).Should().BeFalse();
        (a == null).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_GetHashCode_ReturnsSameForEqualObjects()
    {
        var objs = Enumerable.Range(0, 100)
            .Select(_ => new TestValueObject("Alice", 30))
            .ToList();

        var hashCodes = objs.Select(o => o.GetHashCode()).Distinct();

        hashCodes.Should().ContainSingle();
    }

    [Fact]
    public void ValueObject_Equal_WhenNullComponentsAreInvolved()
    {
        var objWithNull = new TestValueObject(null!, 0);
        var sameObj = new TestValueObject(null!, 0);

        objWithNull.Should().Be(sameObj);
        objWithNull.GetHashCode().Should().Be(sameObj.GetHashCode());
    }
}
