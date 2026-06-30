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
            AlternarBotaoAtivo(BtnInicio);
        }
        private void AlternarBotaoAtivo(Button botaoClicado)
        {
            Style estiloNormal = (Style)FindResource("MenuButtonStyle");
            BtnInicio.Style = estiloNormal;
            BtnTurmas.Style = estiloNormal;
            BtnAlunos.Style = estiloNormal;
            BtnRelatorios.Style = estiloNormal;

            Style estiloAtivo = (Style)FindResource("ActiveMenuButtonStyle");
            botaoClicado.Style = estiloAtivo;
        }

        private void TrocarConteudoPrincipal(UserControl novaView, Button botaoClicado)
        {
            ConteudoPrincipal.Content = novaView;
            AlternarBotaoAtivo(botaoClicado);
        }
        private void BtnInicio_Click(object sender, RoutedEventArgs e) => TrocarConteudoPrincipal(new InicioView(), BtnInicio);
        private void BtnTurmas_Click(object sender, RoutedEventArgs e) => TrocarConteudoPrincipal(new TurmasView(), BtnTurmas);
        private void BtnAlunos_Click(object sender, RoutedEventArgs e) => TrocarConteudoPrincipal(new AlunosView(), BtnAlunos);
        private void BtnRelatorios_Click(object sender, RoutedEventArgs e) => TrocarConteudoPrincipal(new RelatoriosView(), BtnRelatorios);

    }
}
