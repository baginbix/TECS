using TECS;
using Xunit;

namespace UnitTestsECS;

public class TickTests
{
    [Fact]
    public void DefaultConstructor_StartsAtZero()
    {
        Tick tick = new();

        Assert.Equal(0u, tick.CurrentTick);
    }

    [Fact]
    public void Constructor_SetsInitialValue()
    {
        Tick tick = new(42);

        Assert.Equal(42u, tick.CurrentTick);
    }

    [Fact]
    public void ImplicitConversion_FromUint_Works()
    {
        Tick tick = 42u;

        Assert.Equal(42u, tick.CurrentTick);
    }

    [Fact]
    public void ImplicitConversion_ToUint_Works()
    {
        Tick tick = new(42);

        uint value = tick;

        Assert.Equal(42u, value);
    }

    [Fact]
    public void Equals_Tick_Works()
    {
        Tick a = new(10);
        Tick b = new(10);
        Tick c = new(20);

        Assert.True(a.Equals(b));
        Assert.False(a.Equals(c));
    }

    [Fact]
    public void Equals_Uint_Works()
    {
        Tick tick = new(10);

        Assert.True(tick.Equals(10u));
        Assert.False(tick.Equals(20u));
    }

    [Fact]
    public void Equals_Object_WorksForTick()
    {
        Tick tick = new(10);

        object same = new Tick(10);
        object different = new Tick(20);

        Assert.True(tick.Equals(same));
        Assert.False(tick.Equals(different));
    }

    [Fact]
    public void Equals_Object_DoesNotMatchUint()
    {
        Tick tick = new(10);

        object value = 10u;

        Assert.False(tick.Equals(value));
    }

    [Fact]
    public void EqualityOperators_WorkWithTicks()
    {
        Tick a = new(10);
        Tick b = new(10);
        Tick c = new(20);

        Assert.True(a == b);
        Assert.False(a != b);

        Assert.False(a == c);
        Assert.True(a != c);
    }

    [Fact]
    public void EqualityOperators_WorkWithUint()
    {
        Tick tick = new(10);

        Assert.True(tick == 10u);
        Assert.False(tick != 10u);

        Assert.False(tick == 20u);
        Assert.True(tick != 20u);
    }

    [Fact]
    public void EqualityOperators_WorkWithUintOnLeft()
    {
        Tick tick = new(10);

        Assert.True(10u == tick);
        Assert.False(10u != tick);

        Assert.False(20u == tick);
        Assert.True(20u != tick);
    }

    [Fact]
    public void GreaterThan_Works()
    {
        Tick a = new(20);
        Tick b = new(10);

        Assert.True(a > b);
        Assert.False(b > a);
        Assert.False(a > a);
    }

    [Fact]
    public void LessThan_Works()
    {
        Tick a = new(10);
        Tick b = new(20);

        Assert.True(a < b);
        Assert.False(b < a);
        Assert.False(a < a);
    }

    [Fact]
    public void GreaterThanOrEqual_Works()
    {
        Tick a = new(20);
        Tick b = new(10);
        Tick same = new(20);

        Assert.True(a >= b);
        Assert.True(a >= same);
        Assert.False(b >= a);
    }

    [Fact]
    public void LessThanOrEqual_Works()
    {
        Tick a = new(10);
        Tick b = new(20);
        Tick same = new(10);

        Assert.True(a <= b);
        Assert.True(a <= same);
        Assert.False(b <= a);
    }

    [Fact]
    public void ComparisonOperators_WorkWithUint()
    {
        Tick tick = new(10);

        Assert.True(tick > 5u);
        Assert.False(tick > 10u);

        Assert.True(tick < 20u);
        Assert.False(tick < 10u);

        Assert.True(tick >= 10u);
        Assert.True(tick >= 5u);
        Assert.False(tick >= 20u);

        Assert.True(tick <= 10u);
        Assert.True(tick <= 20u);
        Assert.False(tick <= 5u);
    }

    [Fact]
    public void ReverseComparisonOperators_WorkWithUint()
    {
        Tick tick = new(10);

        Assert.True(20u > tick);
        Assert.False(10u > tick);

        Assert.True(5u < tick);
        Assert.False(10u < tick);

        Assert.True(10u >= tick);
        Assert.True(20u >= tick);
        Assert.False(5u >= tick);

        Assert.True(10u <= tick);
        Assert.True(5u <= tick);
        Assert.False(20u <= tick);
    }

    [Fact]
    public void Addition_Works()
    {
        Tick tick = new(10);

        Tick result = tick + 5u;

        Assert.Equal(15u, result.CurrentTick);
    }

    [Fact]
    public void Subtraction_Works()
    {
        Tick tick = new(10);

        Tick result = tick - 5u;

        Assert.Equal(5u, result.CurrentTick);
    }

    [Fact]
    public void Increment_Works()
    {
        Tick tick = new(10);

        tick++;

        Assert.Equal(11u, tick.CurrentTick);
    }

    [Fact]
    public void Decrement_Works()
    {
        Tick tick = new(10);

        tick--;

        Assert.Equal(9u, tick.CurrentTick);
    }

    [Fact]
    public void GetHashCode_IsEqualForEqualTicks()
    {
        Tick a = new(42);
        Tick b = new(42);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_IsNotRequiredToBeEqualForDifferentTicks()
    {
        Tick a = new(42);
        Tick b = new(43);

        // This is intentionally not Assert.NotEqual.
        // Different values are allowed to have the same hash code.
        Assert.Equal(a, a);
        Assert.Equal(b, b);
    }

    [Fact]
    public void ToString_ReturnsExpectedValue()
    {
        Tick tick = new(42);

        Assert.Equal("Current tick: 42", tick.ToString());
    }
}