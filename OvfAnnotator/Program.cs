// Program.cs - Now with more convenience and flair!

using CommandLine;
using OpenVectorFormat.OVFReaderWriter;
using OvfAnnotator;
using System;
using System.IO;

public class Program {
    public static void Main(string[] args) {
        Parser.Default.ParseArguments<Options>(args)
               .WithParsed(RunOptionsAndReturnExitCode);
    }

    public static void RunOptionsAndReturnExitCode(Options options) {
        // --- NEW: Our Awesome Welcome Banner! ---
        Console.WriteLine("=================================================");
        Console.WriteLine("||                OVF Annotator                ||");
        Console.WriteLine("=================================================");
        Console.WriteLine();

        try {
            // 1. Validate inputs and Handle Paths
            if (!File.Exists(options.InputFile)) {
                throw new FileNotFoundException("Input OVF file not found.", options.InputFile);
            }

            // --- NEW: Smart Default Path Logic! ---
            string outputDirectory = options.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outputDirectory)) {
                // The user didn't provide an output path, so we'll create one!
                string inputDirectory = Path.GetDirectoryName(options.InputFile);
                string inputFileName = Path.GetFileNameWithoutExtension(options.InputFile);
                outputDirectory = Path.Combine(inputDirectory, $"{inputFileName}_DXF_Output");
                Console.WriteLine("No output directory specified. Using smart default:");
            }

            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine($"Processing file: {options.InputFile}");
            Console.WriteLine($"Output will be saved to: {outputDirectory}");

            // 2. Read the OVF File
            using var reader = new OVFFileReader();
            reader.OpenJob(options.InputFile);
            var jobShell = reader.JobShell;
            int totalLayers = jobShell.NumWorkPlanes;
            Console.WriteLine($"File contains {totalLayers} layers.");

            var converter = new OvfToDxfConverter();

            // 3. THE BIG LOOP (unchanged, but now uses our new outputDirectory variable)
            for (int i = 0; i < totalLayers; i++) {
                Console.WriteLine($"--> Processing Layer {i}...");
                var workPlane = reader.GetWorkPlane(i);

                string inputFileName = Path.GetFileNameWithoutExtension(options.InputFile);
                string outputFileName = $"{inputFileName}_Layer_{i}.dxf";
                string fullOutputPath = Path.Combine(outputDirectory, outputFileName);

                var dxfDocument = converter.Convert(workPlane);
                dxfDocument.Save(fullOutputPath);
                Console.WriteLine($"    Successfully saved: {outputFileName}");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nProcessing complete! All layers have been converted.");
            Console.ResetColor();
        } catch (Exception ex) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }
}