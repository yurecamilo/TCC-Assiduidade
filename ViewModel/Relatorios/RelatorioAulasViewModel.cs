using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Relatorios
{
    public class RelatorioAulasViewModel : BaseViewModel
    {
        private List<AulaExibicaoDTO> _listaOriginalDoBanco = new List<AulaExibicaoDTO>();
        private readonly RelatorioService _relatorioService = new RelatorioService();

        private IEnumerable<TurmaExibicaoDTO> _turmas;
        private TurmaExibicaoDTO _turmaSelecionada;
        private IEnumerable<AulaExibicaoDTO> _aulas;
        private AulaExibicaoDTO _aulaSelecionada;
        private DateTime? _dataInicio;
        private DateTime? _dataFim;
        private List<RelatorioAusente> _alunosAusentes;

        public List<RelatorioAusente> AlunosAusentes
        {
            get => _alunosAusentes;
            set { _alunosAusentes = value; OnPropertyChanged(); }
        }
        public IEnumerable<TurmaExibicaoDTO> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public TurmaExibicaoDTO TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                _turmaSelecionada = value;
                OnPropertyChanged();
                ExecutarBusca(); // Busca automática quando muda a turma
            }
        }

        public IEnumerable<AulaExibicaoDTO> Aulas
        {
            get => _aulas;
            set { _aulas = value; OnPropertyChanged(); }
        }

        public AulaExibicaoDTO AulaSelecionada
        {
            get => _aulaSelecionada;
            set { _aulaSelecionada = value; OnPropertyChanged(); }
        }

        public DateTime? DataInicio
        {
            get => _dataInicio;
            set
            {
                _dataInicio = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }

        public DateTime? DataFim
        {
            get => _dataFim;
            set
            {
                _dataFim = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }

        public ICommand GerarHtmlCommand { get; private set; }
        public ICommand LimparDatasCommand { get; private set; }
        public ICommand VoltarCommand { get; }

        private readonly RelatoriosViewModel _pai;


        public RelatorioAulasViewModel(RelatoriosViewModel pai)
        {
            _pai = pai;
            DataInicio = null;
            DataFim = null;

            VoltarCommand = new RelayCommand(Voltar);
            GerarHtmlCommand = new RelayCommand(GerarHtml);
            LimparDatasCommand = new RelayCommand(ExecutarLimparDatas);

            InicializarComboBoxTurmas();
            _ = CarregarDadosIniciaisAsync();
        }

        private void Voltar()
        {
            _pai.ConteudoAtual = null;
        }

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.AulasCache ?? new List<AulaExibicaoDTO>();
                Aulas = _listaOriginalDoBanco;

                InicializarComboBoxTurmas();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel carregar as aulas. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }

        private void InicializarComboBoxTurmas()
        {
            var listaCombo = new List<TurmaExibicaoDTO>();
            listaCombo.Add(new TurmaExibicaoDTO
            {
                Nome = "Todas as turmas",
                TurmaOriginal = new Turma { Nome = "Todas as turmas" }
            });

            if (DataCacheService.TurmasCache != null)
            {
                listaCombo.AddRange(DataCacheService.TurmasCache);
            }

            Turmas = listaCombo;
            TurmaSelecionada = listaCombo[0];
        }

        private void ExecutarBusca()
        {
            try
            {
                string turmaFiltro = TurmaSelecionada?.Nome ?? "Todas as turmas";

                Aulas = _listaOriginalDoBanco.Where(a =>
                {
                    bool bateTurma = (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    bool bateDataInicio = !DataInicio.HasValue || a.Data.Date >= DataInicio.Value.Date;

                    bool bateDataFim = !DataFim.HasValue || a.Data.Date <= DataFim.Value.Date;

                    return bateTurma && bateDataInicio && bateDataFim;
                }).ToList();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel filtrar as aulas.", ex);
            }
        }

        private void ExecutarLimparDatas()
        {
            DataInicio = null;
            DataFim = null;
        }

        private void GerarHtml(object parametro)
        {
            try
            {
                var dados = _relatorioService.ObterDadosRelatorio(AulaSelecionada.AulaId);

                AlunosAusentes = dados ?? new List<RelatorioAusente>();
                string html = _relatorioService.RelatorioPorAula(AulaSelecionada, AlunosAusentes);

                string caminho = ArquivoService.SalvarHtmlEAbrir(
                    html,
                    "Relatorio_Aula_" + AulaSelecionada.AulaId + "_" + AulaSelecionada.Turma + ".html");

                if (caminho != null)
                    MessageBox.Show("Relatorio HTML gerado com sucesso.");
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel gerar o relatorio HTML.", ex);
            }
        }

    }
}
