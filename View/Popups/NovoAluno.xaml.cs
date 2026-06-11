using System.Windows;
using TCC_Assiduidade.ViewModel.Popups;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Lógica interna para NovoAluno.xaml
    /// </summary>
    public partial class NovoAluno : Window
    {
        public NovoAluno()
        {
            InitializeComponent();
            this.DataContext = new NovoAlunoViewModel(this.Close);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
