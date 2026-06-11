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
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.View.Popups;
using TCC_Assiduidade.ViewModel;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para AulasView.xam
    /// </summary>
    public partial class RelatorioAulasView : UserControl
    {
        private readonly RelatoriosAulaViewModel _viewModel;
        public RelatorioAulasView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatoriosAulaViewModel(pai);
            DataContext = _viewModel;
        }

        private void BtnVisualizarAula_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botaoClicado && botaoClicado.DataContext is AulaExibicaoDTO aula)
            {
                // Passa a aula direto pro construtor
                ResumoAula janelaPopUp = new ResumoAula(aula);

                janelaPopUp.Owner = Window.GetWindow(this);
                janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                janelaPopUp.ShowDialog();
            }
        }
    }
}
