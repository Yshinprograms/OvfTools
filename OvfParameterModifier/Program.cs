namespace OvfParameterModifier {
    public class Program {
        public static void Main(string[] args) {
            var ui = new ConsoleUI();
            var editor = new JobEditor();
            var app = new ParameterEditorApp(ui, editor);

            app.Run();
        }
    }
}