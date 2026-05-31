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
using TCC_Assiduidade.View.Popups;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para DashboardView.xam
    /// </summary>
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        private void BtnImportarCadastro_Click(object sender, RoutedEventArgs e)
        {
            ImportarCadastro janelaPopUp = new ImportarCadastro();

            Window janelaPrincipal = Window.GetWindow(this);
            janelaPopUp.Owner = janelaPrincipal;
            janelaPopUp.ShowDialog();
        }

        private void BtnImportarChamada_Click(object sender, RoutedEventArgs e)
        {
            ImportarPresenca janelaPopUp = new ImportarPresenca();

            Window janelaPrincipal = Window.GetWindow(this);
            janelaPopUp.Owner = janelaPrincipal;
            janelaPopUp.ShowDialog();
        }

        private void BtnGerarRelatorio_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
