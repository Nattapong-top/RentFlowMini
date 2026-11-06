using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Forms;
using static RentFlowApp.Tests.UI.InputTestHelper;

namespace RentFlowApp.Tests.UI;

public class BillingPeriodSelectorTests
{
    [Theory]
    [InlineData(-6)]
    [InlineData(6)]
    public void SetBillingPeriod_OutsideListedYears_KeepsThatPeriod(int yearsFromNow)
    {
        RunInSta(() =>
        {
            using var selector = new BillingPeriodSelector();
            var period = BillingPeriod.Create($"{DateTime.Now.Year + yearsFromNow}-03");

            selector.SetBillingPeriod(period);

            Assert.Equal(period.Value, selector.GetBillingPeriod()!.Value);
            var years = ((ComboBox)selector.Controls.Find("BillingYearComboBox", true)[0]).Items.Cast<int>().ToList();
            Assert.Equal(years.Order(), years);
        });
    }
}
