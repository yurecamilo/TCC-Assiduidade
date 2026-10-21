using System;
using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para TurmasView.xam
    /// </summary>
    public partial class TurmasView : UserControl
    {
        private readonly TurmasViewModel _viewModel;
        public TurmasView()
        {
            InitializeComponent();
            _viewModel = new TurmasViewModel();
            this.DataContext = _viewModel;

            // Ao sair da tela o ViewModel solta a assinatura do evento estático.
            Unloaded += (s, e) => (DataContext as IDisposable)?.Dispose();
        }
    }
}
