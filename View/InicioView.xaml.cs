using System.Windows;
using System.Windows.Controls;
using TCC_Assiduidade.View.Popups;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Interação lógica para InicioView.xaml
    /// </summary>
    public partial class InicioView : UserControl
    {
        public InicioView()
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

        private void BtnCadastrarTurma_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button botaoClicado)
            {
                NovaTurma janelaPopUp = new NovaTurma();

                janelaPopUp.Owner = Window.GetWindow(this);
                janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                janelaPopUp.ShowDialog();
            }
        }

        public void BtnCadastrarAluno_Click(object sender, RoutedEventArgs e)
        {
            NovoAluno janelaPopUp = new NovoAluno();

            janelaPopUp.Owner = Window.GetWindow(this);
            janelaPopUp.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            janelaPopUp.ShowDialog();
        }
    }
}
