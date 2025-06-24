using OvfParameterModifier;
using OpenVectorFormat;
using OpenVectorFormat.OVFReaderWriter;

namespace OvfParameterModifier.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Initialize the UI and editor
            var ui = new ConsoleUI();
            var editor = new JobEditor();
            
            // Create and run the application
            var app = new ParameterEditorApp(ui, editor);
            app.Run();
        }
    }
}