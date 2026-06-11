using System.Windows;
using System.Windows.Controls;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Lógica interna para Dashboard.xaml
    /// </summary>
    public partial class BaseWindow : Window
    {
        public BaseWindow()
        {
            InitializeComponent();
            ConteudoPrincipal.Content = new InicioView();
            AlternarBotaoAtivo(BtnDashboard);
        }
        private void AlternarBotaoAtivo(Button botaoClicado)
        {
            Style estiloNormal = (Style)FindResource("MenuButtonStyle");
            BtnDashboard.Style = estiloNormal;
            BtnTurmas.Style = estiloNormal;
            BtnAlunos.Style = estiloNormal;
            BtnRelatorios.Style = estiloNormal;

            Style estiloAtivo = (Style)FindResource("ActiveMenuButtonStyle");
            botaoClicado.Style = estiloAtivo;
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new InicioView();
            AlternarBotaoAtivo(BtnDashboard);
        }


        private void BtnTurmas_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new TurmasView();
            AlternarBotaoAtivo(BtnTurmas);
        }

        private void BtnAlunos_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new AlunosView();
            AlternarBotaoAtivo(BtnAlunos);
        }


        private void BtnRelatorios_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new RelatoriosView();
            AlternarBotaoAtivo(BtnRelatorios);
        }

    }
}
