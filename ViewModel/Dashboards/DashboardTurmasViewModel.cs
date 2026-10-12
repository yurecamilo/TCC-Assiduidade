using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.ViewModel.Dashboards
{
    public class DashboardTurmasViewModel : DashboardBaseViewModel
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");
        private List<OpcaoTurmaDashboard> _turmas = new List<OpcaoTurmaDashboard>();
        private OpcaoTurmaDashboard _turmaSelecionada;
        private DateTime? _dataInicio;
        private DateTime? _dataFim;
        private ResultadoDashboardTurmas _resultado = new ResultadoDashboardTurmas();
        private bool _dadosCarregados;
        private bool _mostrarArquivadas;

        public bool MostrarArquivadas
        {
            get => _mostrarArquivadas;
            set { _mostrarArquivadas = value; OnPropertyChanged(); AtualizarListaTurmas(); }
        }

        public DashboardTurmasViewModel(DashboardsViewModel pai, DashboardService dashboardService)
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

        public ResultadoDashboardTurmas Resultado
        {
            get => _resultado;
            private set
            {
                _resultado = value ?? new ResultadoDashboardTurmas();
                OnPropertyChanged();
                OnPropertyChanged(nameof(AssiduidadeTexto));
                OnPropertyChanged(nameof(TotalTurmasTexto));
                OnPropertyChanged(nameof(TotalAlunosTexto));
                OnPropertyChanged(nameof(AlunosRiscoTexto));
                OnPropertyChanged(nameof(Ranking));
                OnPropertyChanged(nameof(Evolucao));
            }
        }

        public string AssiduidadeTexto => Resultado.AssiduidadeGeral.ToString("0.#", Cultura) + "%";
        public string TotalTurmasTexto => Resultado.TotalTurmas.ToString();
        public string TotalAlunosTexto => Resultado.TotalAlunos.ToString();
        public string AlunosRiscoTexto => Resultado.AlunosEmRisco.ToString();
        public List<PontoGraficoDashboard> Ranking => Resultado.Ranking;
        public List<PontoGraficoDashboard> Evolucao => Resultado.Evolucao;

        protected override string GerarHtml()
        {
            return DashboardHtmlService.GerarHtmlTurmas(Resultado, DataInicio, DataFim);
        }

        protected override string NomeArquivoHtml => "Dashboard_Turmas.html";

        private async Task InicializarAsync()
        {
            DefinirMensagemSemDados("Carregando dados do dashboard...");
            try
            {
                await _dashboardService.CarregarDadosAsync();
                AtualizarListaTurmas();
                DataInicio = _dashboardService.ObterPrimeiraData();
                DataFim = _dashboardService.ObterUltimaData();
                _dadosCarregados = true;
                TurmaSelecionada = Turmas.FirstOrDefault();
                if (TurmaSelecionada == null) AtualizarDashboard();
            }
            catch (Exception ex)
            {
                DefinirMensagemSemDados("Não foi possível carregar os dados do dashboard.");
                MostrarErro("Não foi possível carregar o dashboard de turmas.", ex);
            }
        }

        private void AtualizarListaTurmas()
        {
            if (!_dadosCarregados && _dashboardService == null) return;
            var anterior = TurmaSelecionada?.Id ?? 0;
            var turmas = _dashboardService.ObterTurmas(true).Where(t => t.Id == 0 || MostrarArquivadas || !t.Arquivada).ToList();
            Turmas = turmas;
            TurmaSelecionada = Turmas.FirstOrDefault(t => t.Id == anterior) ?? Turmas[0];
        }

        private void AtualizarDashboard()
        {
            if (!_dadosCarregados) return;

            Resultado = _dashboardService.ObterDashboardTurmas(
                TurmaSelecionada?.Id ?? 0,
                DataInicio,
                DataFim,
                MostrarArquivadas);

            DefinirMensagemSemDados(Resultado.TemDados ? string.Empty : Resultado.Mensagem);
        }
    }
}
