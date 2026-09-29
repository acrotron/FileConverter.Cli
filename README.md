# FileConverter.Cli

A command-line tool that converts Esri ASCII Raster (`.asc`) files to NMPlot grid format.

The raster's coordinates are read as Lambert Conformal Conic meters relative to the origin (as in AEDT noise grids);
each cell center is converted to WGS84 longitude/latitude and written as an NMPlot `DPAL` point with the cell value.
NODATA cells are written with their NODATA value.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Build and test

```bash
dotnet build
dotnet test
```

## Usage

```bash
FileConverter.Cli <input file> <lat> <long> <output file>
```

### Arguments

| Argument      | Description                                                             |
|---------------|-------------------------------------------------------------------------|
| `input file`  | Path to the Esri ASCII Raster `.asc` file                               |
| `lat`         | Latitude of the origin point, in degrees (non-zero, between -90 and 90) |
| `long`        | Longitude of the origin point, in degrees (between -180 and 180)        |
| `output file` | Path for the generated NMPlot output file                               |

Use `.` as the decimal separator for `lat` and `long`, whatever the system's regional settings. The output always
uses `.` as the decimal separator and CRLF line endings.

### Exit codes

| Code | Meaning                                                                        |
|------|--------------------------------------------------------------------------------|
| 0    | The output file was written                                                    |
| 1    | Invalid arguments, or the input could not be read (no output file is written) |

Errors are written to stderr.

### Example

```bash
FileConverter.Cli input.asc 52.3676 4.9041 output.nmp
```

## License

MIT
