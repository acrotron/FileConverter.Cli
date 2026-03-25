# FileConverter.Cli

A command-line tool that converts Esri ASCII Raster (`.asc`) files to NMPlot grid format.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```bash
dotnet build
```

## Usage

```bash
FileConverter <input file> <lat> <long> <output file>
```

### Arguments

| Argument      | Description                              |
|---------------|------------------------------------------|
| `input file`  | Path to the Esri ASCII Raster `.asc` file |
| `lat`         | Latitude of the origin point             |
| `long`        | Longitude of the origin point            |
| `output file` | Path for the generated NMPlot output file |

### Example

```bash
FileConverter input.asc 52.3676 4.9041 output.nmp
```
