// Options.cs

using CommandLine;

namespace OvfAnnotator {
    public class Options {
        [Option('i', "input", Required = true, HelpText = "Input OVF file to process.")]
        public string? InputFile { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory to save the DXF files.")]
        public string? OutputDirectory { get; set; }
    }
}