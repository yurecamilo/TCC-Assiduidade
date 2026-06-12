using System.Windows;
using System.Windows.Controls;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.View.Popups;
using TCC_Assiduidade.ViewModel;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.View.Relatórios
{
    /// <summary>
    /// Interação lógica para AulasView.xam
    /// </summary>
    public partial class RelatorioAulasView : UserControl
    {
        private readonly RelatorioAulasViewModel _viewModel;
        public RelatorioAulasView(RelatoriosViewModel pai)
        {
            InitializeComponent();
            _viewModel = new RelatorioAulasViewModel(pai);
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
