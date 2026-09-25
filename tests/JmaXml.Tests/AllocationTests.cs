namespace JmaXml.Tests;

public class AllocationTests
{
    [Theory]
    [InlineData("77_01_27_240613_VXSE45.xml", 140_000)]
    [InlineData("32-39_11_05_240613_VXSE53.xml", 1_900_000)]
    [InlineData("15_18_01_250630_VPWS50.xml", 20_500_000)]
    public void Parse_allocates_within_budget(string file, long budget)
    {
        var xml = Fixtures.Sample(file);
        Report.Parse(xml);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var report = Report.Parse(xml);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(report);
        Assert.True(allocated <= budget, $"{file} allocated {allocated:N0} bytes (budget {budget:N0})");
    }
}
