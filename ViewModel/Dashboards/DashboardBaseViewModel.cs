using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Dashboards
{
    public abstract class DashboardBaseViewModel : BaseViewModel
    {
        protected readonly DashboardsViewModel _pai;
        protected readonly DashboardService _dashboardService;

        private string _mensagemEstado;

        protected DashboardBaseViewModel(DashboardsViewModel pai, DashboardService dashboardService)
        {
            _pai = pai;
            _dashboardService = dashboardService;

            VoltarCommand = new RelayCommand(() => _pai.ConteudoAtual = null);
            GerarHtmlCommand = new RelayCommand(ExecutarExportacao);
        }

        public string MensagemEstado
        {
            get => _mensagemEstado;
            protected set
            {
                _mensagemEstado = value;
                OnPropertyChanged();
            }
        }

        public ICommand VoltarCommand { get; }
        public ICommand GerarHtmlCommand { get; }

        protected abstract string GerarHtml();
        protected abstract string NomeArquivoHtml { get; }

        protected void DefinirMensagemSemDados(string mensagem)
        {
            MensagemEstado = mensagem ?? string.Empty;
        }

        private void ExecutarExportacao()
        {
            try
            {
                string html = GerarHtml();
                string caminho = ArquivoService.SalvarHtmlEAbrir(html, NomeArquivoHtml);
                if (caminho == null) return;

                MessageBox.Show(
                    "Dashboard HTML gerado e aberto no navegador.\n\n" + Path.GetFileName(caminho),
                    "Dashboard gerado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MostrarErro("Não foi possível gerar o dashboard em HTML.", ex);
            }
        }
    }
}
