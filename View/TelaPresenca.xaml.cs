using System.Configuration;
using System.Windows;

namespace TCC_Assiduidade.View
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