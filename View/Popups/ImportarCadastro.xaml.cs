using System.Windows;
using Microsoft.Win32;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    public partial class ImportarCadastro : Window
    {
        private readonly ImportarCadastroViewModel _viewModel;

        public ImportarCadastro()
        {
            InitializeComponent();
            _viewModel = new ImportarCadastroViewModel();
            this.DataContext = _viewModel; 
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}