using System.Windows;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para NovaTurma.xaml
    /// </summary>
    public partial class NovaTurma : Window
    {
        private readonly NovaTurmaViewModel _viewModel;
        public NovaTurma()
        {
            InitializeComponent();
            _viewModel = new NovaTurmaViewModel(this.Close);
            DataContext = _viewModel;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
