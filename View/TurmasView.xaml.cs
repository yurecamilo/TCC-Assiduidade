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
using TCC_Assiduidade.ViewModel.Popups;

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
        }
    }
}
