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
    }
}
