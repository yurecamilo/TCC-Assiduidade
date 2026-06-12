using System.Windows;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.View.ANTIGO
{
    public partial class TelaRelatoriosAulas : Window
    {
        public TelaRelatoriosAulas()
        {
            InitializeComponent();
            DataContext = new RelatoriosViewModel();
        }
    }
}
