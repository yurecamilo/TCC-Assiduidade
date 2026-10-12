using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Relatorios
{
    public class AlunoRelatorioItem : AlunoExibicaoDTO
    {
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public void NotificarSelecaoAlterada()
        {
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public class RelatorioAlunosViewModel : BaseViewModel
    {
        private List<AlunoExibicaoDTO> _listaOriginalDoBanco = new List<AlunoExibicaoDTO>();
        private readonly RelatorioService _relatorioService = new RelatorioService();
        private readonly RelatoriosViewModel _pai;

        private List<Turma> _turmas;
        private Turma _turmaSelecionada;
        private List<AlunoRelatorioItem> _alunos;
        private bool _selecionarTodos;
        private string _textoBusca;
        private bool _incluirArquivadas;

        public bool IncluirTurmasArquivadas
        {
            get => _incluirArquivadas;
            set { _incluirArquivadas = value; OnPropertyChanged(); InicializarComboBoxTurmas(); ExecutarBusca(); }
        }

        public List<Turma> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }
        public Turma TurmaSelecionada
        {
            get => _turmaSelecionada;
            set
            {
                _turmaSelecionada = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }
        public bool SelecionarTodos
        {
            get => _selecionarTodos;
            set
            {
                _selecionarTodos = value;
                OnPropertyChanged(nameof(SelecionarTodos));

                if (Alunos != null)
                {
                    foreach (var aluno in Alunos)
                    {
                        aluno.IsSelected = _selecionarTodos;
                        aluno.NotificarSelecaoAlterada();
                    }
                }
            }
        }

        public string TextoBusca
        {
            get => _textoBusca;
            set
            {
                _textoBusca = value;
                OnPropertyChanged();
                ExecutarBusca();
            }
        }

        public List<AlunoRelatorioItem> Alunos
        {
            get => _alunos;
            set { _alunos = value; OnPropertyChanged(); }
        }

        public ICommand VoltarCommand { get; }
        public ICommand GerarRelatorioLoteCommand { get; }
        public ICommand BuscarCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }

        public RelatorioAlunosViewModel(RelatoriosViewModel pai)
        {
            _pai = pai;

            VoltarCommand = new RelayCommand(() => _pai.ConteudoAtual = null);
            GerarRelatorioLoteCommand = new RelayCommand(GerarRelatorioLote);
            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarDadosAsync();
        }

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();
                InicializarComboBoxTurmas();
                ExecutarBusca();
            });
        }

        private async Task CarregarDadosAsync()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

                _listaOriginalDoBanco = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();
                InicializarComboBoxTurmas();
                ExecutarBusca();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel carregar os alunos. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }

        private void InicializarComboBoxTurmas()
        {
            try
            {
                var ativas = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();
                var historico = DataCacheService.AlunosHistoricoCache ?? new List<AlunoExibicaoDTO>();
                var turmasHistorico = DataCacheService.TurmasHistoricoCache ?? new List<TurmaExibicaoDTO>();
                var matriculasAtivas = new HashSet<string>(ativas.Select(a => a.Matricula), StringComparer.OrdinalIgnoreCase);
                var matriculasSomenteArquivadas = new HashSet<string>(
                    historico.Where(a => a.Turma != null && a.Turma.Contains("(arquivada)") && !matriculasAtivas.Contains(a.Matricula))
                        .Select(a => a.Matricula), StringComparer.OrdinalIgnoreCase);

                var nomesTurmasArquivadasDisponiveis = new HashSet<string>(
                    historico.Where(a => matriculasSomenteArquivadas.Contains(a.Matricula) && a.Turma != null && a.Turma.Contains("(arquivada)"))
                        .Select(a => a.Turma), StringComparer.OrdinalIgnoreCase);

                var turmasDisponiveis = turmasHistorico
                    .Where(t => t.TurmaOriginal.Status == EStatusTurma.Ativa ||
                        (t.TurmaOriginal.Status == EStatusTurma.Arquivada &&
                         (IncluirTurmasArquivadas || nomesTurmasArquivadasDisponiveis.Contains(t.Nome))))
                    .Select(t => new Turma { Id = t.TurmaOriginal.Id, Nome = t.Nome, Status = t.TurmaOriginal.Status })
                    .ToList();

                Turmas = new List<Turma> { new Turma { Id = 0, Nome = "Selecionar turma" } };
                Turmas.AddRange(turmasDisponiveis);
                if (IncluirTurmasArquivadas)
                    _listaOriginalDoBanco = historico;
                else
                {
                    _listaOriginalDoBanco = ativas.Concat(historico.Where(a => a.Turma != null && a.Turma.Contains("(arquivada)") && matriculasSomenteArquivadas.Contains(a.Matricula))).ToList();
                }
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel carregar as turmas. Verifique a conexao com o banco e tente novamente.", ex);
            }
        }

        private void ExecutarBusca()
        {
            try
            {
                string buscaTexto = (TextoBusca ?? "").Trim().ToLower();
                string turmaFiltro = (TurmaSelecionada?.Nome ?? "Selecionar turma").Trim();

                // CORRIGIDO: Mesma lógica unificada e inteligente para o filtro funcionar sempre!
                Alunos = _listaOriginalDoBanco.Where(a =>
                {
                    bool bateTexto = string.IsNullOrWhiteSpace(buscaTexto) ||
                                     (a.Nome != null && a.Nome.ToLower().Contains(buscaTexto)) ||
                                     (a.Email != null && a.Email.ToLower().Contains(buscaTexto)) ||
                                     (a.Matricula != null && a.Matricula.ToLower().Contains(buscaTexto));

                    bool bateTurma = (turmaFiltro == "Selecionar turma") ||
                                     (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Trim().Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    return bateTexto && bateTurma;
                }).Select(a => new AlunoRelatorioItem
                {
                    Nome = a.Nome,
                    Matricula = a.Matricula,
                    Turma = a.Turma,
                    Email = a.Email,
                    DadosFrequencia = a.DadosFrequencia,
                    DataEntrada = a.DataEntrada, // Repassando a data de histórico para o DTO
                    IsSelected = false
                }).ToList();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel filtrar os alunos.", ex);
            }
        }

        private void ExecutarLimparBusca()
        {
            TextoBusca = string.Empty;
        }

        private void GerarRelatorioLote()
        {
            try
            {
                var selecionados = Alunos.Where(a => a.IsSelected).ToList();
                if (!selecionados.Any()) return;

                string html = _relatorioService.RelatorioPorAluno(selecionados);
                ArquivoService.SalvarHtmlEAbrir(html, "Relatorios.html");
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel gerar o relatorio de alunos.", ex);
            }
        }
    }
}
