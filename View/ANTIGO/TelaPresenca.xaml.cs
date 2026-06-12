using System.Configuration;
using System.Windows;

namespace TCC_Assiduidade.View.ANTIGO
{
    public partial class TelaPresenca : Window
    {
        public TelaPresenca()
        {
            InitializeComponent();
            this.DataContext = new PresencaViewModel();
        }
    }
}