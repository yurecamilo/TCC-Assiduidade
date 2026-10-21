using System.Linq;
using System.Windows;

namespace TCC_Assiduidade
{
    /// <summary>
    /// Mensagem de interface do sistema, no padrão visual dos demais popups.
    /// Expõe sobrecargas de Show compatíveis com MessageBox.Show para que os
    /// pontos de chamada mantenham as mesmas combinações de botões, ícones e
    /// resultados (MessageBoxResult), incluindo os fluxos de confirmação
    /// (Sim/Não/Cancelar).
    /// </summary>
    public static class PopupMensagem
    {
        public static MessageBoxResult Show(string mensagem)
        {
            return Show(mensagem, null, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string mensagem, string titulo)
        {
            return Show(mensagem, titulo, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string mensagem, string titulo, MessageBoxButton botoes)
        {
            return Show(mensagem, titulo, botoes, MessageBoxImage.None);
        }

        public static MessageBoxResult Show(string mensagem, string titulo, MessageBoxButton botoes, MessageBoxImage icone)
        {
            var janela = new View.Popups.MensagemPopup(mensagem, titulo, botoes, icone);

            Window dono = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive);

            if (dono != null)
                janela.Owner = dono;

            janela.ShowDialog();
            return janela.Resultado;
        }
    }
}
