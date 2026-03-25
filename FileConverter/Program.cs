using AsciiRaster.Parser;
using NetTopologySuite.Geometries;

Console.WriteLine("From Esri Ascii Raster to NMPlot");

// read the arguments from the command line
if (args.Length < 4)
{
    Console.WriteLine("Usage: FileConverter <input file> <lat> <long> <output file>");
    return;
}

double latitude = double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
double longitude = double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);

ExtFileReader reader = new ExtFileReader();
List<CoordinateM> coordinates = reader.Read(args[0], latitude, longitude);

// write the output to a file
using var writer = new StreamWriter(args[3]);
// header

writer.WriteLine($"{{TITL Grid Vers 2 3}}\r\n{{MTRC \"DNL\" \"dB\"}}\r\n{{DPAL {coordinates.Count}");

foreach (var coordinate in coordinates)
{
    writer.WriteLine($"({coordinate.X},{coordinate.Y}) {coordinate.M}");
}

writer.WriteLine("}\r\n{ENDF}\r\n");
