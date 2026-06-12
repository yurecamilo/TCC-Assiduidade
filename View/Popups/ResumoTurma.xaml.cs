using System.Windows;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para ResumoTurma.xaml
    /// </summary>
    public partial class ResumoTurma : Window
    {
        public ResumoTurma(TurmaExibicaoDTO turma)
        {
            InitializeComponent();
            DataContext = new ResumoTurmaViewModel(turma);
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
