using System;
using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.View.Relatórios
{
    /// <summary>
    /// Interação lógica para RelatorioTurmasView.xam
    /// </summary>
    public partial class RelatorioTurmasView : UserControl
    {
        private readonly RelatorioTurmasViewModel _viewModel;
        public RelatorioTurmasView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatorioTurmasViewModel(pai);
            DataContext = _viewModel;

            // Ao sair da tela o ViewModel solta a assinatura do evento estático.
            Unloaded += (s, e) => (DataContext as IDisposable)?.Dispose();
        }
    }
}
