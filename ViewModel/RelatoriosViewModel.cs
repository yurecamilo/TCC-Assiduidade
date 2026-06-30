using System.Windows.Input;
using TCC_Assiduidade.View.Relatórios;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{

    public class RelatoriosViewModel : BaseViewModel
    {
        private object _conteudoAtual;
        public object ConteudoAtual
        {
            get => _conteudoAtual;
            set
            {
                _conteudoAtual = value;
                OnPropertyChanged(nameof(ConteudoAtual)); // <--- ISTO Ã‰ OBRIGATÃ“RIO
            }
        }

        // Exemplo de comando do botÃ£o
        public ICommand AbrirRelatorioAulasCommand => new RelayCommand(() => {
            // Ao trocar a ViewModel, o DataTemplate do WPF substitui a View automaticamente
            ConteudoAtual = new RelatorioAulasView(this);
        });

        public ICommand AbrirRelatorioAlunosCommand => new RelayCommand(() =>
        {
            ConteudoAtual = new RelatorioAlunosView(this);
        });

        public ICommand AbrirRelatorioTurmasCommand => new RelayCommand(() =>
        {
            ConteudoAtual = new RelatorioTurmasView(this);
        });
    }
}