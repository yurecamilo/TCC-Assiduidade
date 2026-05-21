using System;
using System.Windows.Input;

namespace TCC_Assiduidade.ViewModels.Base
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        // Construtor: recebe a função que o botão vai executar
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // O WPF joga aqui para saber se o botão deve ficar ativo (true) ou cinza/desativado (false)
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        // É o método que o WPF chama fisicamente quando o usuário clica no botão
        public void Execute(object parameter)
        {
            _execute();
        }

        // Avisa o WPF para reavaliar se o botão deve ser ativado ou desativado
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}