// Options.cs

using CommandLine;

namespace OvfAnnotator {
    public class Options {
        [Option('i', "input", Required = true, HelpText = "Input OVF file to process.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = false, HelpText = "Optional: Output directory. If not specified, a folder will be created next to the input file.")]
        public string OutputDirectory { get; set; }
    }
}