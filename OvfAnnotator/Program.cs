// Program.cs

using CommandLine;
using OpenVectorFormat.OVFReaderWriter;
using OvfAnnotator;
using ShellProgressBar;
using System;
using System.IO;

public class Program {
    public static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args)
               .WithParsed(RunOptionsAndReturnExitCode);
    }

    public static void RunOptionsAndReturnExitCode(Options options) {
        Console.WriteLine("=================================================");
        Console.WriteLine("||              OVF Annotator                  ||");
        Console.WriteLine("=================================================");
        Console.WriteLine();

        try {
            // --- Preparation Stage ---
            if (!File.Exists(options.InputFile)) throw new FileNotFoundException("Input OVF file not found.", options.InputFile);

            string outputDirectory = GetOutputDirectory(options);
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine($"Processing file: {options.InputFile}");
            Console.WriteLine($"Output will be saved to: {outputDirectory}");

            using var reader = new OVFFileReader();
            reader.OpenJob(options.InputFile);
            var jobShell = reader.JobShell;
            int totalLayersInFile = jobShell.NumWorkPlanes;

            (int startLayer, int endLayer) = ParseLayerRange(options.LayerRange, totalLayersInFile);
            int layersToProcessCount = endLayer - startLayer + 1;
            Console.WriteLine($"Found {totalLayersInFile} layers. Will process layers {startLayer} through {endLayer}.");

            var converter = new OvfToDxfConverter();

            int totalBlocksProcessed = 0;

            var progressBarOptions = new ProgressBarOptions {
                ForegroundColor = ConsoleColor.Yellow,
                BackgroundColor = ConsoleColor.DarkGray,
                ProgressCharacter = '█',
                ProgressBarOnBottom = true
            };

            using (var pbar = new ProgressBar(layersToProcessCount, "Starting...", progressBarOptions)) {
                for (int i = startLayer; i <= endLayer; i++) {
                    var workPlane = reader.GetWorkPlane(i);
                    totalBlocksProcessed += workPlane.VectorBlocks.Count;

                    string inputFileName = Path.GetFileNameWithoutExtension(options.InputFile);
                    string outputFileName = $"{inputFileName}_Layer_{i}.dxf";
                    string fullOutputPath = Path.Combine(outputDirectory, outputFileName);

                    var dxfDocument = converter.Convert(workPlane, options);
                    dxfDocument.Save(fullOutputPath);

                    pbar.Tick($"Processed Layer {i} -> {outputFileName}");
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=================================================");
            Console.WriteLine("             Processing Complete!");
            Console.WriteLine("=================================================");
            Console.WriteLine($"       Total Layers Processed: {layersToProcessCount}");
            Console.WriteLine($"        Total Vector Blocks: {totalBlocksProcessed}");
            Console.WriteLine($"  Total DXF Files Created: {layersToProcessCount}");
            Console.WriteLine($"         Output Location: {outputDirectory}");
            Console.WriteLine("=================================================");
            Console.ResetColor();
        } catch (Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static string GetOutputDirectory(Options options) {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory)) {
            return options.OutputDirectory;
        }

        string inputDirectory = Path.GetDirectoryName(options.InputFile);
        string inputFileName = Path.GetFileNameWithoutExtension(options.InputFile);
        return Path.Combine(inputDirectory, $"{inputFileName}_DXF_Output");
    }

    private static (int, int) ParseLayerRange(string rangeString, int totalLayersInFile) {
        // If the user provides no input, we process everything.
        // The internal representation for "everything" is 0 to the last index.
        if (string.IsNullOrWhiteSpace(rangeString)) {
            return (0, totalLayersInFile - 1);
        }

        // Case 1: User enters a single number, e.g., "--layers 5"
        if (int.TryParse(rangeString, out int singleLayer)) {
            if (singleLayer < 1 || singleLayer > totalLayersInFile) {
                throw new ArgumentOutOfRangeException($"Layer index {singleLayer} is out of the valid range (1-{totalLayersInFile}).");
            }
            return (singleLayer - 1, singleLayer - 1);
        }

        // Case 2: User enters a range, e.g., "--layers 10-20"
        var parts = rangeString.Split('-');
        if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end)) {
            if (start > end) {
                throw new ArgumentException("Start layer cannot be greater than end layer in the specified range.");
            }
            if (start < 1 || end > totalLayersInFile) {
                throw new ArgumentOutOfRangeException($"Layer range {start}-{end} is out of the valid range (1-{totalLayersInFile}).");
            }
            // Convert both start and end to 0-based indices.
            return (start - 1, end - 1);
        }

        throw new FormatException($"The layer range format '{rangeString}' is invalid. Please use a 1-based format like \"10-20\" or \"5\".");
    }
}