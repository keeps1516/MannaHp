using FluentAssertions;
using MannaHp.Shared.Enums;
using MannaHp.Shared.Helpers;

namespace MannaHp.Shared.Tests.Helpers;

public class UnitConversionTests
{
    // ── GetMeasurementType ──

    [Theory]
    [InlineData(UnitOfMeasure.Oz, MeasurementType.Weight)]
    [InlineData(UnitOfMeasure.Lb, MeasurementType.Weight)]
    [InlineData(UnitOfMeasure.Cups, MeasurementType.Volume)]
    [InlineData(UnitOfMeasure.FlOz, MeasurementType.Volume)]
    [InlineData(UnitOfMeasure.Tsp, MeasurementType.Volume)]
    [InlineData(UnitOfMeasure.Tbsp, MeasurementType.Volume)]
    [InlineData(UnitOfMeasure.Each, MeasurementType.Count)]
    [InlineData(UnitOfMeasure.Shot, MeasurementType.Count)]
    public void GetMeasurementType_ReturnsCorrectType(UnitOfMeasure unit, MeasurementType expected)
    {
        UnitConversion.GetMeasurementType(unit).Should().Be(expected);
    }

    // ── GetAbbreviation ──

    [Theory]
    [InlineData(UnitOfMeasure.Oz, "oz")]
    [InlineData(UnitOfMeasure.Lb, "lb")]
    [InlineData(UnitOfMeasure.Cups, "cups")]
    [InlineData(UnitOfMeasure.FlOz, "fl oz")]
    [InlineData(UnitOfMeasure.Tsp, "tsp")]
    [InlineData(UnitOfMeasure.Tbsp, "tbsp")]
    [InlineData(UnitOfMeasure.Each, "each")]
    [InlineData(UnitOfMeasure.Shot, "shot")]
    public void GetAbbreviation_ReturnsCorrectString(UnitOfMeasure unit, string expected)
    {
        UnitConversion.GetAbbreviation(unit).Should().Be(expected);
    }

    // ── CanConvert ──

    [Theory]
    [InlineData(UnitOfMeasure.Oz, UnitOfMeasure.Lb, true)]
    [InlineData(UnitOfMeasure.Lb, UnitOfMeasure.Oz, true)]
    [InlineData(UnitOfMeasure.Cups, UnitOfMeasure.FlOz, true)]
    [InlineData(UnitOfMeasure.Tsp, UnitOfMeasure.Tbsp, true)]
    [InlineData(UnitOfMeasure.Each, UnitOfMeasure.Shot, true)]
    [InlineData(UnitOfMeasure.Oz, UnitOfMeasure.Oz, true)]
    [InlineData(UnitOfMeasure.Oz, UnitOfMeasure.FlOz, false)]
    [InlineData(UnitOfMeasure.Lb, UnitOfMeasure.Cups, false)]
    [InlineData(UnitOfMeasure.Each, UnitOfMeasure.Oz, false)]
    [InlineData(UnitOfMeasure.Shot, UnitOfMeasure.Tsp, false)]
    public void CanConvert_ReturnsExpectedResult(UnitOfMeasure from, UnitOfMeasure to, bool expected)
    {
        UnitConversion.CanConvert(from, to).Should().Be(expected);
    }

    // ── Convert ──

    [Fact]
    public void Convert_SameUnit_ReturnsSameValue()
    {
        UnitConversion.Convert(5.0m, UnitOfMeasure.Oz, UnitOfMeasure.Oz).Should().Be(5.0m);
    }

    [Fact]
    public void Convert_1LbToOz_Returns16()
    {
        UnitConversion.Convert(1.0m, UnitOfMeasure.Lb, UnitOfMeasure.Oz).Should().Be(16.0m);
    }

    [Fact]
    public void Convert_16OzToLb_Returns1()
    {
        UnitConversion.Convert(16.0m, UnitOfMeasure.Oz, UnitOfMeasure.Lb).Should().Be(1.0m);
    }

    [Fact]
    public void Convert_1CupToFlOz_Returns8()
    {
        UnitConversion.Convert(1.0m, UnitOfMeasure.Cups, UnitOfMeasure.FlOz).Should().Be(8.0m);
    }

    [Fact]
    public void Convert_8FlOzToCups_Returns1()
    {
        UnitConversion.Convert(8.0m, UnitOfMeasure.FlOz, UnitOfMeasure.Cups).Should().Be(1.0m);
    }

    [Fact]
    public void Convert_1TbspToFlOz_Returns0Point5()
    {
        UnitConversion.Convert(1.0m, UnitOfMeasure.Tbsp, UnitOfMeasure.FlOz).Should().Be(0.5m);
    }

    [Fact]
    public void Convert_3TspToTbsp_Returns1()
    {
        // 3 tsp * (1/6 fl oz per tsp) = 0.5 fl oz / (0.5 fl oz per tbsp) = 1
        UnitConversion.Convert(3.0m, UnitOfMeasure.Tsp, UnitOfMeasure.Tbsp).Should().BeApproximately(1.0m, 0.0001m);
    }

    [Fact]
    public void Convert_CrossType_ThrowsInvalidOperationException()
    {
        var act = () => UnitConversion.Convert(1.0m, UnitOfMeasure.Oz, UnitOfMeasure.FlOz);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Convert_CountToWeight_ThrowsInvalidOperationException()
    {
        var act = () => UnitConversion.Convert(1.0m, UnitOfMeasure.Each, UnitOfMeasure.Oz);
        act.Should().Throw<InvalidOperationException>();
    }
}
