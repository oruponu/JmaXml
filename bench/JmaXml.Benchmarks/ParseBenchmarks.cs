using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using System.Xml;

namespace JmaXml.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(Config))]
public class ParseBenchmarks
{
    private static readonly XmlReaderSettings ScanSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    private string _xml = "";

    [Params("77_01_27_240613_VXSE45.xml", "32-39_11_05_240613_VXSE53.xml", "15_18_01_250630_VPWS50.xml")]
    public string Sample { get; set; } = "";

    [GlobalSetup]
    public void Setup() => _xml = File.ReadAllText(Path.Combine(FindSamples(), Sample));

    [Benchmark(Baseline = true)]
    public int Scan()
    {
        using var reader = XmlReader.Create(new StringReader(_xml), ScanSettings);
        var length = 0;
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Text) length += reader.Value.Length;
            else if (reader.NodeType == XmlNodeType.Element) while (reader.MoveToNextAttribute()) length += reader.Value.Length;
        }
        return length;
    }

    [Benchmark]
    public Report Parse() => Report.Parse(_xml);

    private sealed class Config : ManualConfig
    {
        public Config() => SummaryStyle = SummaryStyle.Default.WithMaxParameterColumnWidth(40);
    }

    private static string FindSamples()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JmaXml.slnx"))) dir = dir.Parent;
        return dir is null
            ? throw new DirectoryNotFoundException("JmaXml.slnx not found above " + AppContext.BaseDirectory)
            : Path.Combine(dir.FullName, "tests", "JmaXml.Tests", "fixtures", "samples");
    }
}
