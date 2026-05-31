using System.Windows;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
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
