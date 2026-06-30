using System.Windows.Controls;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para InicioView.xaml
    /// </summary>
    public partial class InicioView : UserControl
    {
        private readonly InicioViewModel _viewModel;
        public InicioView()
        {
            InitializeComponent();
            _viewModel = new InicioViewModel();
            this.DataContext = _viewModel;
        }
    }
}
