using System.Windows;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para ImportarPresenca.xaml
    /// </summary>
    public partial class ImportarPresenca : Window
    {
        private readonly ImportarPresencaViewModel _viewModel;
        public ImportarPresenca()
        {
            InitializeComponent();
            _viewModel = new ImportarPresencaViewModel(this.Close);
            this.DataContext = _viewModel;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
