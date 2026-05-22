using System.Windows;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    public partial class TelaRelatoriosAulas : Window
    {
        public TelaRelatoriosAulas()
        {
            InitializeComponent();
            DataContext = new RelatoriosAulasViewModel();
        }
    }
}
