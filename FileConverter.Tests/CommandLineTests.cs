using System.Globalization;
using AwesomeAssertions;

namespace FileConverter.Tests;

[TestClass]
public class CommandLineTests
{
    private const string Sample = "TestData/simplified-int.asc";

    private string _outputDir = "";

    [TestInitialize]
    public void Initialize()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), "FileConverter.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_outputDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        Directory.Delete(_outputDir, recursive: true);
    }

    [TestMethod]
    [DataRow("52.3676", "4.9041", "TestData/simplified-int_52.3676_4.9041.nmp")]
    [DataRow("-34.6037", "-58.3816", "TestData/simplified-int_-34.6037_-58.3816.nmp")] // southern and western
    public void Run_SampleRaster_MatchesGoldenFile(string lat, string @long, string golden)
    {
        // Arrange
        string output = Path.Combine(_outputDir, "out.nmp");

        // Act
        int exitCode = Run(out _, Sample, lat, @long, output);

        // Assert
        exitCode.Should().Be(CommandLine.Success);
        AssertMatchesGolden(output, golden);
    }

    [TestMethod]
    public void Run_CommaDecimalCulture_MatchesGoldenFile()
    {
        // Arrange - 1.0.x wrote "(4,9041,52,36...) 10,5" under nl-NL, or threw for non-integer lat/long
        string output = Path.Combine(_outputDir, "out.nmp");
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("nl-NL");

        int exitCode;
        try
        {
            // Act
            exitCode = Run(out _, Sample, "52.3676", "4.9041", output);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        // Assert
        exitCode.Should().Be(CommandLine.Success);
        AssertMatchesGolden(output, "TestData/simplified-int_52.3676_4.9041.nmp");
    }

    [TestMethod]
    [DataRow(new string[0], DisplayName = "no arguments")]
    [DataRow(new[] { "a.asc", "52", "4" }, DisplayName = "three arguments")]
    [DataRow(new[] { "a.asc", "52", "4", "out.nmp", "extra" }, DisplayName = "five arguments")]
    public void Run_WrongArgumentCount_FailsWithUsage(string[] args)
    {
        // Act
        var stderr = new StringWriter();
        int exitCode = CommandLine.Run(args, TextWriter.Null, stderr);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.ToString().Should().Contain("Usage:");
    }

    [TestMethod]
    [DataRow("52,3", "4.9", "not a number", DisplayName = "comma decimal lat (used to be read as 523)")]
    [DataRow("abc", "4.9", "not a number", DisplayName = "non-numeric lat")]
    [DataRow("NaN", "4.9", "not a number", DisplayName = "NaN lat")]
    [DataRow("95", "4.9", "between -90 and 90", DisplayName = "lat out of range")]
    [DataRow("0", "4.9", "must not be 0", DisplayName = "lat on the equator")]
    [DataRow("52.3", "181", "between -180 and 180", DisplayName = "long out of range")]
    public void Run_InvalidCoordinate_FailsWithoutWritingOutput(string lat, string @long, string message)
    {
        // Arrange
        string output = Path.Combine(_outputDir, "out.nmp");

        // Act
        int exitCode = Run(out string stderr, Sample, lat, @long, output);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().Contain(message);
        File.Exists(output).Should().BeFalse();
    }

    [TestMethod]
    public void Run_MissingInputFile_FailsWithoutWritingOutput()
    {
        // Arrange
        string output = Path.Combine(_outputDir, "out.nmp");

        // Act
        int exitCode = Run(out string stderr, Path.Combine(_outputDir, "missing.asc"), "52.3", "4.9", output);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().StartWith("Error:").And.Contain("missing.asc");
        File.Exists(output).Should().BeFalse();
    }

    [TestMethod]
    public void Run_MalformedRaster_FailsWithoutWritingOutput()
    {
        // Arrange - header promises 4 values, data has 3
        string input = Path.Combine(_outputDir, "bad.asc");
        File.WriteAllLines(input, ["ncols 2", "nrows 2", "xllcorner 0", "yllcorner 0", "cellsize 1", "1 2 3"]);
        string output = Path.Combine(_outputDir, "out.nmp");

        // Act
        int exitCode = Run(out string stderr, input, "52.3", "4.9", output);

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().StartWith("Error:");
        File.Exists(output).Should().BeFalse();
    }

    [TestMethod]
    public void Run_OutputDirectoryMissing_Fails()
    {
        // Act
        int exitCode = Run(out string stderr, Sample, "52.3", "4.9", Path.Combine(_outputDir, "no-such-dir", "out.nmp"));

        // Assert
        exitCode.Should().Be(CommandLine.Failure);
        stderr.Should().StartWith("Error:");
    }

    /// <summary>
    /// Compares an output file with a golden file: identical text, except that coordinates may differ by 1e-12
    /// degrees (~0.1 µm), because trigonometric functions can round differently in the last bit across platforms.
    /// </summary>
    private static void AssertMatchesGolden(string output, string golden)
    {
        string actualText = File.ReadAllText(output);
        actualText.Replace("\r\n", "").Should().NotContain("\n", "every line ends with CRLF");

        string[] actual = actualText.Split("\r\n");
        string[] expected = File.ReadAllText(golden).Split("\r\n");
        actual.Should().HaveSameCount(expected);

        for (int i = 0; i < expected.Length; i++)
        {
            if (TryParsePoint(expected[i], out var e) && TryParsePoint(actual[i], out var a))
            {
                a.X.Should().BeApproximately(e.X, 1e-12, $"longitude on line {i + 1}");
                a.Y.Should().BeApproximately(e.Y, 1e-12, $"latitude on line {i + 1}");
                a.Value.Should().Be(e.Value, $"value on line {i + 1}");
            }
            else
            {
                actual[i].Should().Be(expected[i], $"line {i + 1}");
            }
        }
    }

    // Parses a DPAL point line: "(x,y) value", written with the invariant culture.
    private static bool TryParsePoint(string line, out (double X, double Y, double Value) point)
    {
        point = default;
        if (!line.StartsWith('(')) return false;

        int close = line.IndexOf(')');
        string[] xy = line[1..close].Split(',');
        var inv = CultureInfo.InvariantCulture;
        point = (double.Parse(xy[0], inv), double.Parse(xy[1], inv), double.Parse(line[(close + 1)..], inv));
        return true;
    }

    private static int Run(out string stderr, params string[] args)
    {
        var errors = new StringWriter();
        int exitCode = CommandLine.Run(args, TextWriter.Null, errors);
        stderr = errors.ToString();
        return exitCode;
    }
}
