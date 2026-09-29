using System.Globalization;

namespace FileConverter;

/// <summary>
/// Parses the command line and runs the conversion.
/// </summary>
public static class CommandLine
{
    /// <summary>
    /// Exit code for a successful conversion.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// Exit code for invalid arguments or a failed conversion.
    /// </summary>
    public const int Failure = 1;

    private const string Usage = "Usage: FileConverter.Cli <input file> <lat> <long> <output file>";

    /// <summary>
    /// Runs the converter.
    /// </summary>
    /// <param name="args">Input file, latitude, longitude and output file.</param>
    /// <param name="stdout">Writer for progress output.</param>
    /// <param name="stderr">Writer for errors.</param>
    /// <returns><see cref="Success"/> or <see cref="Failure"/>.</returns>
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        stdout.WriteLine("From Esri Ascii Raster to NMPlot");

        if (args.Length != 4)
        {
            stderr.WriteLine($"Error: expected 4 arguments but got {args.Length}.");
            stderr.WriteLine(Usage);
            return Failure;
        }

        if (!TryParseCoordinate(args[1], "lat", 90, stderr, out double latitude) ||
            !TryParseCoordinate(args[2], "long", 180, stderr, out double longitude))
        {
            stderr.WriteLine(Usage);
            return Failure;
        }

        if (latitude == 0)
        {
            // The tangential Lambert Conformal Conic projection degenerates at the equator.
            stderr.WriteLine("Error: lat must not be 0.");
            return Failure;
        }

        try
        {
            // Convert fully before touching the output file, so a bad input leaves no partial file behind.
            // NMPlot is Windows software: always write CRLF, whatever platform the converter runs on.
            var converted = new StringWriter { NewLine = "\r\n" };
            NmPlotGridWriter.Convert(args[0], latitude, longitude, converted);
            File.WriteAllText(args[3], converted.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            // FileNotFoundException and DirectoryNotFoundException are IOExceptions; InvalidDataException is a
            // malformed raster.
            stderr.WriteLine($"Error: {ex.Message}");
            return Failure;
        }

        stdout.WriteLine($"Wrote {args[3]}");
        return Success;
    }

    private static bool TryParseCoordinate(string text, string name, double limit, TextWriter stderr, out double value)
    {
        // NumberStyles.Float has no thousands separator, so "52,3" is rejected instead of read as 523.
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
            !double.IsFinite(value))
        {
            stderr.WriteLine($"Error: {name} \"{text}\" is not a number; use '.' as the decimal separator.");
            return false;
        }

        if (value <= -limit || value >= limit)
        {
            stderr.WriteLine($"Error: {name} {value.ToString(CultureInfo.InvariantCulture)} must be between -{limit} and {limit}.");
            return false;
        }

        return true;
    }
}
