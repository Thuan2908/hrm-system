using Hrm.Modules.Payroll.Application;
using Xunit;

namespace Hrm.UnitTests;

public sealed class PayrollCalculationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1000000, 0)]
    [InlineData(5_000_000, 250_000)] // Bậc 1: 5tr * 5% = 250.000
    [InlineData(10_000_000, 500_000)] // Bậc 1: 10tr * 5% = 500.000
    [InlineData(20_000_000, 1_500_000)] // Bậc 2: 20tr * 10% - 500k = 1.500.000
    [InlineData(30_000_000, 2_500_000)] // Bậc 2: 30tr * 10% - 500k = 2.500.000
    [InlineData(50_000_000, 6_500_000)] // Bậc 3: 50tr * 20% - 3.5tr = 6.500.000
    [InlineData(80_000_000, 14_500_000)] // Bậc 4: 80tr * 30% - 9.5tr = 14.500.000
    [InlineData(120_000_000, 27_500_000)] // Bậc 5: 120tr * 35% - 14.5tr = 27.500.000
    public void ProgressiveTaxCalculatesCorrectlyAccordingToVietnameseLaw(decimal taxableIncome, decimal expectedTax)
    {
        var actualTax = PayrollService.CalculateProgressivePersonalIncomeTax(taxableIncome);
        Assert.Equal(expectedTax, actualTax);
    }
}
