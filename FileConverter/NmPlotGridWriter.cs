using AsciiRaster.Parser;
using NetTopologySuite.Geometries;

namespace FileConverter;

/// <summary>
/// Converts an AEDT Esri ASCII raster to an NMPlot grid file.
/// </summary>
public static class NmPlotGridWriter
{
    /// <summary>
    /// Reads the raster, converts its cell centers to WGS84 longitude/latitude around the given origin, and writes
    /// them as an NMPlot <c>DPAL</c> point list. Numbers are always written with a '.' decimal separator.
    /// </summary>
    /// <param name="inputPath">Path of the <c>.asc</c> file.</param>
    /// <param name="latitude">Latitude of the projection origin in degrees.</param>
    /// <param name="longitude">Longitude of the projection origin in degrees.</param>
    /// <param name="output">Writer for the NMPlot file.</param>
    public static void Convert(string inputPath, double latitude, double longitude, TextWriter output)
    {
        List<CoordinateM> coordinates = new ExtFileReader().Read(inputPath, latitude, longitude);

        output.WriteLine($"{{TITL Grid Vers 2 3}}\r\n{{MTRC \"DNL\" \"dB\"}}\r\n{{DPAL {coordinates.Count}");

        foreach (var coordinate in coordinates)
        {
            output.WriteLine(FormattableString.Invariant($"({coordinate.X},{coordinate.Y}) {coordinate.M}"));
        }

        output.WriteLine("}\r\n{ENDF}\r\n");
    }
}
