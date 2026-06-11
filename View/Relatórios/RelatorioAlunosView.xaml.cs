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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para RelatorioAlunosView.xam
    /// </summary>
    public partial class RelatorioAlunosView : UserControl
    {
        private readonly RelatorioAlunosViewModel _viewModel;
        public RelatorioAlunosView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatorioAlunosViewModel(pai);
            DataContext = _viewModel;
        }
    }
}
