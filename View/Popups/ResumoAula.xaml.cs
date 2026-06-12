using System.Windows;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    public partial class ResumoAula : Window
    {
        // Apenas o construtor, sem Action
        public ResumoAula(AulaExibicaoDTO aula)
        {
            InitializeComponent();
            DataContext = new ResumoAulaViewModel(aula);
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}