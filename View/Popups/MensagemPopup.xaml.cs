using System.Windows;

namespace TCC_Assiduidade.View.Popups
{
    /// <summary>
    /// Janela de mensagem do sistema, no mesmo padrão visual dos demais popups
    /// (janela sem moldura, cantos arredondados e paleta verde-petróleo).
    /// Substitui o MessageBox do Windows exibindo título, ícone semântico e
    /// botões conforme a combinação de MessageBoxButton / MessageBoxImage.
    /// </summary>
    public partial class MensagemPopup : Window
    {
        /// <summary>Botão equivalente ao X da janela.</summary>
        private MessageBoxResult _resultadoAoFechar;

        /// <summary>Resultado escolhido pelo usuário, lido após o ShowDialog.</summary>
        public MessageBoxResult Resultado { get; private set; } = MessageBoxResult.None;

        public MensagemPopup(string mensagem, string titulo, MessageBoxButton botoes, MessageBoxImage icone)
        {
            InitializeComponent();

            TxtTitulo.Text = string.IsNullOrWhiteSpace(titulo) ? TituloPadrao(icone) : titulo;
            TxtMensagem.Text = mensagem;

            AplicarIcone(icone);
            AplicarBotoes(botoes);
        }

        /// <summary>Título usado quando o chamador não informa um.</summary>
        private static string TituloPadrao(MessageBoxImage icone)
        {
            switch (icone)
            {
                case MessageBoxImage.Error:
                    return "Erro";
                case MessageBoxImage.Warning:
                    return "Aviso";
                case MessageBoxImage.Question:
                    return "Confirmação";
                default:
                    return "Informação";
            }
        }

        /// <summary>Ícone semântico (informação, dúvida, aviso ou erro) e sua cor de destaque.</summary>
        private void AplicarIcone(MessageBoxImage icone)
        {
            switch (icone)
            {
                case MessageBoxImage.Error:
                    Icone.Data = System.Windows.Media.Geometry.Parse(PathIconeErro());
                    Icone.Fill = PincelDaCor("#A6524C");
                    break;
                case MessageBoxImage.Warning:
                    Icone.Data = System.Windows.Media.Geometry.Parse(PathIconeAviso());
                    Icone.Fill = PincelDaCor("#B07C3E");
                    break;
                case MessageBoxImage.Question:
                    Icone.Data = System.Windows.Media.Geometry.Parse(PathIconeDuvida());
                    Icone.Fill = PincelDaCor("#4A6984");
                    break;
                default:
                    Icone.Data = System.Windows.Media.Geometry.Parse(PathIconeInformacao());
                    Icone.Fill = PincelDaCor("#2A5C60");
                    break;
            }
        }

        /// <summary>Exibe os botões correspondentes à combinação informada.</summary>
        private void AplicarBotoes(MessageBoxButton botoes)
        {
            // Nenhum botão visível por padrão; a combinação escolhida ativa os que precisa.
            BtnOk.Visibility = Visibility.Collapsed;
            BtnSim.Visibility = Visibility.Collapsed;
            BtnNao.Visibility = Visibility.Collapsed;
            BtnCancelar.Visibility = Visibility.Collapsed;
            BtnOk.IsDefault = false;
            BtnSim.IsDefault = false;

            switch (botoes)
            {
                case MessageBoxButton.OK:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    // O X equivale a confirmar (fechar a mensagem de informação).
                    _resultadoAoFechar = MessageBoxResult.OK;
                    break;

                case MessageBoxButton.OKCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancelar.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    _resultadoAoFechar = MessageBoxResult.Cancel;
                    break;

                case MessageBoxButton.YesNo:
                    BtnSim.Visibility = Visibility.Visible;
                    BtnNao.Visibility = Visibility.Visible;
                    // Como no MessageBox do Windows, Yes/No não oferece cancelamento.
                    BtnFechar.Visibility = Visibility.Collapsed;
                    _resultadoAoFechar = MessageBoxResult.No;
                    break;

                case MessageBoxButton.YesNoCancel:
                    BtnSim.Visibility = Visibility.Visible;
                    BtnNao.Visibility = Visibility.Visible;
                    BtnCancelar.Visibility = Visibility.Visible;
                    _resultadoAoFechar = MessageBoxResult.Cancel;
                    break;
            }
        }

        private static System.Windows.Media.SolidColorBrush PincelDaCor(string codigoHex)
        {
            return (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(codigoHex);
        }

        private static string PathIconeInformacao()
        {
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,10.5H13V17H11V10.5M11,7H13V9H11V7Z";
        }

        private static string PathIconeDuvida()
        {
            return "M10,19H13V22H10V19M12,2C17.35,2.22 19.68,7.62 16.5,11.67C15.67,12.67 14.33,13.33 13.67,14.17C13,15 13,16 13,17H10C10,15.33 10,13.67 10.67,12.67C11.33,11.67 12.67,11 13.5,10C15.5,7.5 14.5,4.5 12,4.5A3,3 0 0,0 9,7.5H6C6,3.57 8.76,1.9 12,2Z";
        }

        private static string PathIconeAviso()
        {
            return "M13,14H11V9H13M13,18H11V16H13M1,21H23L12,2L1,21Z";
        }

        private static string PathIconeErro()
        {
            return "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,7H13V13H11V7M11,15H13V17H11V15Z";
        }

        private void Concluir(MessageBoxResult resultado)
        {
            Resultado = resultado;
            Close();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Concluir(MessageBoxResult.OK);
        private void BtnSim_Click(object sender, RoutedEventArgs e) => Concluir(MessageBoxResult.Yes);
        private void BtnNao_Click(object sender, RoutedEventArgs e) => Concluir(MessageBoxResult.No);
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Concluir(MessageBoxResult.Cancel);

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            Concluir(_resultadoAoFechar);
        }
    }
}
