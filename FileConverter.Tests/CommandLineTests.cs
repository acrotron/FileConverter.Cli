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
        File.ReadAllBytes(output).Should().Equal(File.ReadAllBytes(golden));
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
        File.ReadAllBytes(output).Should().Equal(File.ReadAllBytes("TestData/simplified-int_52.3676_4.9041.nmp"));
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

    private static int Run(out string stderr, params string[] args)
    {
        var errors = new StringWriter();
        int exitCode = CommandLine.Run(args, TextWriter.Null, errors);
        stderr = errors.ToString();
        return exitCode;
    }
}
