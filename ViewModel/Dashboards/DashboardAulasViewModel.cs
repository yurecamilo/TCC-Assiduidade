using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.ViewModel.Dashboards
{
    public class DashboardAulasViewModel : DashboardBaseViewModel
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");
        private List<OpcaoTurmaDashboard> _turmas = new List<OpcaoTurmaDashboard>();
        private OpcaoTurmaDashboard _turmaSelecionada;
        private DateTime? _dataInicio;
        private DateTime? _dataFim;
        private ResultadoDashboardAulas _resultado = new ResultadoDashboardAulas();
        private bool _dadosCarregados;

        public DashboardAulasViewModel(DashboardsViewModel pai, DashboardService dashboardService)
            : base(pai, dashboardService)
        {
            _ = InicializarAsync();
        }

        public List<OpcaoTurmaDashboard> Turmas
        {
            get => _turmas;
            private set { _turmas = value; OnPropertyChanged(); }
        }

        public OpcaoTurmaDashboard TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                if (_turmaSelecionada == value) return;
                _turmaSelecionada = value;
                OnPropertyChanged();
                AtualizarDashboard();
            }
        }

        public DateTime? DataInicio
        {
            get => _dataInicio;
            set
            {
                if (_dataInicio == value) return;
                _dataInicio = value;
                OnPropertyChanged();
                AtualizarDashboard();
            }
        }

        public DateTime? DataFim
        {
            get => _dataFim;
            set
            {
                if (_dataFim == value) return;
                _dataFim = value;
                OnPropertyChanged();
                AtualizarDashboard();
            }
        }

        public ResultadoDashboardAulas Resultado
        {
            get => _resultado;
            private set
            {
                _resultado = value ?? new ResultadoDashboardAulas();
                OnPropertyChanged();
                OnPropertyChanged(nameof(PresencaMediaTexto));
                OnPropertyChanged(nameof(TotalAulasTexto));
                OnPropertyChanged(nameof(AusenciasTexto));
                OnPropertyChanged(nameof(MelhorAulaTexto));
                OnPropertyChanged(nameof(AulaAtencaoTexto));
                OnPropertyChanged(nameof(Aulas));
                OnPropertyChanged(nameof(Evolucao));
            }
        }

        public string PresencaMediaTexto => Resultado.PresencaMedia.ToString("0.#", Cultura) + "%";
        public string TotalAulasTexto => Resultado.TotalAulas.ToString();
        public string AusenciasTexto => Resultado.TotalAusencias.ToString();
        public string MelhorAulaTexto => Resultado.TemDados ? Resultado.MelhorAula : "—";
        public string AulaAtencaoTexto => Resultado.TemDados ? Resultado.AulaAtencao : "—";
        public List<PontoGraficoDashboard> Aulas => Resultado.Aulas;
        public List<PontoGraficoDashboard> Evolucao => Resultado.Evolucao;

        protected override string GerarHtml()
        {
            return DashboardHtmlService.GerarHtmlAulas(Resultado, DataInicio, DataFim);
        }

        protected override string NomeArquivoHtml => "Dashboard_Aulas.html";

        private async Task InicializarAsync()
        {
            DefinirMensagemSemDados("Carregando dados do dashboard...");
            try
            {
                await _dashboardService.CarregarDadosAsync();
                Turmas = _dashboardService.ObterTurmas(true);
                DataInicio = _dashboardService.ObterPrimeiraData();
                DataFim = _dashboardService.ObterUltimaData();
                _dadosCarregados = true;
                TurmaSelecionada = Turmas.FirstOrDefault();
                if (TurmaSelecionada == null) AtualizarDashboard();
            }
            catch (Exception ex)
            {
                DefinirMensagemSemDados("Não foi possível carregar os dados do dashboard.");
                MostrarErro("Não foi possível carregar o dashboard de aulas.", ex);
            }
        }

        private void AtualizarDashboard()
        {
            if (!_dadosCarregados) return;

            Resultado = _dashboardService.ObterDashboardAulas(
                TurmaSelecionada?.Id ?? 0,
                DataInicio,
                DataFim);

            DefinirMensagemSemDados(Resultado.TemDados ? string.Empty : Resultado.Mensagem);
        }
    }
}
