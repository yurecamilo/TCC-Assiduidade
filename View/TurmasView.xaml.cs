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
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para TurmasView.xam
    /// </summary>
    public partial class TurmasView : UserControl
    {
        private readonly TurmasViewModel _viewModel;
        public TurmasView()
        {
            InitializeComponent();
            _viewModel = new TurmasViewModel();
            this.DataContext = _viewModel;
        }

        private void BtnVisualizarTurma_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botaoClicado && botaoClicado.DataContext is TurmaExibicaoDTO turma)
            {
                // Passa a aula direto pro construtor
                ResumoTurma janelaPopUp = new ResumoTurma(turma);

                janelaPopUp.Owner = Window.GetWindow(this);
                janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                janelaPopUp.ShowDialog();
            }
        }

        private void BtnCadastrarTurma_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botaoClicado)
            {
                // Passa a aula direto pro construtor
                NovaTurma janelaPopUp = new NovaTurma();

                janelaPopUp.Owner = Window.GetWindow(this);
                janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                janelaPopUp.ShowDialog();
            }
        }
    }
}
