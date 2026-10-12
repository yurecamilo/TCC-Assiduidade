using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.ViewModel.Dashboards
{
    public class DashboardAlunosViewModel : DashboardBaseViewModel
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");
        private List<OpcaoTurmaDashboard> _turmas = new List<OpcaoTurmaDashboard>();
        private List<OpcaoAlunoDashboard> _alunos = new List<OpcaoAlunoDashboard>();
        private OpcaoTurmaDashboard _turmaSelecionada;
        private OpcaoAlunoDashboard _alunoSelecionado;
        private DateTime? _dataInicio;
        private DateTime? _dataFim;
        private ResultadoDashboardAluno _resultado = new ResultadoDashboardAluno();
        private bool _dadosCarregados;
        private bool _incluirArquivadas;

        public bool IncluirArquivadas
        {
            get => _incluirArquivadas;
            set { _incluirArquivadas = value; OnPropertyChanged(); AtualizarListaTurmas(); }
        }

        public DashboardAlunosViewModel(DashboardsViewModel pai, DashboardService dashboardService)
            : base(pai, dashboardService)
        {
            _ = InicializarAsync();
        }

        public List<OpcaoTurmaDashboard> Turmas
        {
            get => _turmas;
            private set { _turmas = value; OnPropertyChanged(); }
        }

        public List<OpcaoAlunoDashboard> Alunos
        {
            get => _alunos;
            private set { _alunos = value; OnPropertyChanged(); }
        }

        public OpcaoTurmaDashboard TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                if (_turmaSelecionada == value) return;
                _turmaSelecionada = value;
                OnPropertyChanged();
                AtualizarListaAlunos();
            }
        }

        public OpcaoAlunoDashboard AlunoSelecionado
        {
            get => _alunoSelecionado;
            set
            {
                if (_alunoSelecionado == value) return;
                _alunoSelecionado = value;
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

        public ResultadoDashboardAluno Resultado
        {
            get => _resultado;
            private set
            {
                _resultado = value ?? new ResultadoDashboardAluno();
                OnPropertyChanged();
                NotificarResultado();
            }
        }

        public string NomeAluno => Resultado.TemDados ? Resultado.Aluno : "Aluno não selecionado";
        public string DetalheAluno => Resultado.TemDados
            ? "Matrícula " + Resultado.Matricula + " • " + Resultado.Turma
            : "Selecione um aluno com aulas registradas.";
        public string AssiduidadeTexto => FormatarPercentual(Resultado.Assiduidade);
        public string PresencasTexto => Resultado.Presentes + "/" + Resultado.TotalAulas;
        public string MediaTurmaTexto => FormatarPercentual(Resultado.MediaTurma);
        public string PosicaoTexto => Resultado.PosicaoTurma > 0
            ? Resultado.PosicaoTurma + "º de " + Resultado.TotalAlunosTurma
            : "—";
        public string SituacaoTexto => Resultado.TemDados ? Resultado.Situacao : "—";
        public List<PontoGraficoDashboard> Evolucao => Resultado.Evolucao;

        protected override string GerarHtml()
        {
            return DashboardHtmlService.GerarHtmlAluno(Resultado, DataInicio, DataFim);
        }

        protected override string NomeArquivoHtml =>
            "Dashboard_Aluno_" + (Resultado.Aluno ?? "Aluno") + ".html";

        private async Task InicializarAsync()
        {
            DefinirMensagemSemDados("Carregando dados do dashboard...");
            try
            {
                await _dashboardService.CarregarDadosAsync();
                DataInicio = _dashboardService.ObterPrimeiraData();
                DataFim = _dashboardService.ObterUltimaData();
                _dadosCarregados = true;
                AtualizarListaTurmas();
                if (TurmaSelecionada == null) AtualizarListaAlunos();
            }
            catch (Exception ex)
            {
                DefinirMensagemSemDados("Não foi possível carregar os dados do dashboard.");
                MostrarErro("Não foi possível carregar os dados do dashboard de alunos.", ex);
            }
        }

        private void AtualizarListaTurmas()
        {
            if (!_dadosCarregados) return;
            var matriculasAtivas = new HashSet<string>(
                (DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>()).Select(a => a.Matricula),
                StringComparer.OrdinalIgnoreCase);
            var turmasArquivadasSoDosAlunosArquivados = new HashSet<int>(
                (DataCacheService.AlunosHistoricoCache ?? new List<AlunoExibicaoDTO>())
                    .Where(a => a.Turma != null && a.Turma.Contains("(arquivada)") && !matriculasAtivas.Contains(a.Matricula))
                    .Select(a => DataCacheService.TurmasHistoricoCache?.FirstOrDefault(t => t.Nome == a.Turma.Replace(" (arquivada)", ""))?.TurmaOriginal?.Id ?? 0)
                    .Where(id => id > 0));
            var turmas = _dashboardService.ObterTurmas(true)
                .Where(t => t.Id == 0 || IncluirArquivadas || !t.Arquivada || turmasArquivadasSoDosAlunosArquivados.Contains(t.Id))
                .ToList();
            Turmas = turmas;
            TurmaSelecionada = Turmas.FirstOrDefault();
        }

        private void AtualizarListaAlunos()
        {
            if (!_dadosCarregados) return;
            int turmaId = TurmaSelecionada?.Id ?? 0;
            Alunos = _dashboardService.ObterAlunos(turmaId, IncluirArquivadas || (turmaId > 0 && TurmaSelecionada?.Arquivada == true));
            AlunoSelecionado = Alunos.FirstOrDefault();
            if (AlunoSelecionado == null) AtualizarDashboard();
        }

        private void AtualizarDashboard()
        {
            if (!_dadosCarregados) return;

            Resultado = _dashboardService.ObterDashboardAluno(
                AlunoSelecionado?.Matricula,
                AlunoSelecionado?.TurmaId ?? TurmaSelecionada?.Id ?? 0,
                DataInicio,
                DataFim,
                IncluirArquivadas);

            DefinirMensagemSemDados(Resultado.TemDados ? string.Empty : Resultado.Mensagem);
        }

        private void NotificarResultado()
        {
            OnPropertyChanged(nameof(NomeAluno));
            OnPropertyChanged(nameof(DetalheAluno));
            OnPropertyChanged(nameof(AssiduidadeTexto));
            OnPropertyChanged(nameof(PresencasTexto));
            OnPropertyChanged(nameof(MediaTurmaTexto));
            OnPropertyChanged(nameof(PosicaoTexto));
            OnPropertyChanged(nameof(SituacaoTexto));
            OnPropertyChanged(nameof(Evolucao));
        }

        private static string FormatarPercentual(double valor)
        {
            return valor.ToString("0.#", Cultura) + "%";
        }
    }
}
