using System.Windows;
using TCC_Assiduidade.ViewModel.ANTIGO;

namespace TCC_Assiduidade.View.ANTIGO
{
    public partial class TelaMenu : Window
    {
        public TelaMenu()
        {
            InitializeComponent();
            DataContext = new MenuViewModel();
        }
    }
}
