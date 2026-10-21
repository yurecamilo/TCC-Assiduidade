using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.Servicos.Demonstracao;

namespace TCC_Assiduidade
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DataCacheService.InicializarCargaBackground();

            bool demonstracao = e.Args.Any(a =>
                string.Equals(a, "--demonstracao", StringComparison.OrdinalIgnoreCase));
            bool demonstracaoDadosReais = e.Args.Any(a =>
                string.Equals(a, "--demonstracao-dados-reais", StringComparison.OrdinalIgnoreCase));

            if (demonstracao || demonstracaoDadosReais)
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    while (MainWindow == null || !MainWindow.IsLoaded)
                        await System.Threading.Tasks.Task.Delay(100);

                    await new DemonstracaoService().ExecutarAsync(MainWindow, demonstracaoDadosReais);
                }), DispatcherPriority.ApplicationIdle);
            }
        }

        /// <summary>
        /// Rede de segurança para exceções do fluxo da interface que escaparem do
        /// tratamento local (falhas fora de comandos com try/catch ou em async void).
        /// Mostra uma mensagem classificada no padrão do sistema e evita que a
        /// aplicação inteira feche por um erro pontual.
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string titulo;
            string mensagem;
            ViewModel.Base.BaseViewModel.ClassificarErro(
                e.Exception, "Ocorreu um erro inesperado e a operação não pôde ser concluída.", out titulo, out mensagem);

            PopupMensagem.Show(mensagem, "Erro inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
