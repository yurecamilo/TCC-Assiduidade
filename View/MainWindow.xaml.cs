using System.Windows;
using System.Windows.Controls;

namespace TCC_Assiduidade.View
{
    /// <summary>
    /// Lógica interna para Dashboard.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConteudoPrincipal.Content = new DashboardView();
            AlternarBotaoAtivo(BtnDashboard);
        }
        private void AlternarBotaoAtivo(Button botaoClicado)
        {
            Style estiloNormal = (Style)FindResource("MenuButtonStyle");
            BtnDashboard.Style = estiloNormal;
            BtnTurmas.Style = estiloNormal;
            BtnAlunos.Style = estiloNormal;
            BtnAulas.Style = estiloNormal;
            BtnRelatorios.Style = estiloNormal;
            BtnConfiguracoes.Style = estiloNormal;

            Style estiloAtivo = (Style)FindResource("ActiveMenuButtonStyle");
            botaoClicado.Style = estiloAtivo;
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new DashboardView();
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

        private void BtnAulas_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new AulasView();
            AlternarBotaoAtivo(BtnAulas);
        }

        private void BtnRelatorios_Click(object sender, RoutedEventArgs e)
        {
            ConteudoPrincipal.Content = new RelatoriosView();
            AlternarBotaoAtivo(BtnRelatorios);
        }

        private void BtnConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            //ConteudoPrincipal.Content = new ConfiguracoesView();
            AlternarBotaoAtivo(BtnConfiguracoes);
        }
    }
}
