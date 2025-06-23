using System;
using System.Windows.Input;

namespace OvfViewer.ViewModels {
    public class RelayCommand : ICommand {
        // --- Fields ---
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        // --- Constructor ---
        public RelayCommand(Action execute, Func<bool>? canExecute = null) {
            // assign the private member to the execute function but throw an exception if it is null
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // --- ICommand Members ---
        public event EventHandler? CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object? parameter) {
            _execute();
        }
    }
}