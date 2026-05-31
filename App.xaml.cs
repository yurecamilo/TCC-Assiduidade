using System.Windows;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DataCacheService.InicializarCargaBackground();
        }
    }
}