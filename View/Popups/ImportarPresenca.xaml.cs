using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            _viewModel = new ImportarPresencaViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
