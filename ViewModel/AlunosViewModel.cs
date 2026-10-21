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
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.ViewModel
{
    public class AlunoListaItem : AlunoExibicaoDTO
    {
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }
    }

    public class AlunosViewModel : BaseViewModel, IDisposable
    {
        private List<AlunoListaItem> _listaOriginalDoBanco = new List<AlunoListaItem>();
        private readonly AlunoService _alunoService;

        private IEnumerable<AlunoListaItem> _alunos;
        private AlunoExibicaoDTO _alunoSelecionado;
        private string _textoBusca;
        private List<Turma> _turmas;
        private Turma _turmaSelecionada;
        private bool _modoSelecaoMultipla;

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

        public IEnumerable<AlunoListaItem> Alunos
        {
            get => _alunos;
            set { _alunos = value; OnPropertyChanged(); }
        }

        public AlunoExibicaoDTO AlunoSelecionado
        {
            get => _alunoSelecionado;
            set
            {
                _alunoSelecionado = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TemAulas));
            }
        }

        public bool TemAulas
        {
            get
            {
                return _alunoSelecionado?.DadosFrequencia != null && _alunoSelecionado.DadosFrequencia.TotalAulas > 0;
            }
        }

        public bool ModoSelecaoMultipla
        {
            get => _modoSelecaoMultipla;
            set
            {
                _modoSelecaoMultipla = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextoModoSelecao));
                OnPropertyChanged(nameof(IconeModoSelecao));
                OnPropertyChanged(nameof(MostrarAcoes));
                if (value) AlunoSelecionado = null;
                if (!value)
                {
                    SelecionarTodos = false;
                    foreach (var aluno in _listaOriginalDoBanco) aluno.IsSelected = false;
                }
            }
        }

        public string TextoModoSelecao => ModoSelecaoMultipla ? "Cancelar seleção" : "Selecionar múltiplos";
        public string IconeModoSelecao => ModoSelecaoMultipla ? "×" : "☷";
        public bool MostrarAcoes => !ModoSelecaoMultipla;

        private bool _selecionarTodos;
        public bool SelecionarTodos
        {
            get => _selecionarTodos;
            set
            {
                _selecionarTodos = value;
                OnPropertyChanged();
                foreach (var aluno in Alunos ?? Enumerable.Empty<AlunoListaItem>()) aluno.IsSelected = value;
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

        public ICommand FecharPerfilCommand { get; private set; }
        public ICommand BuscarCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }
        public ICommand EditarTurmaCommand { get; private set; }
        public ICommand ExcluirTurmaCommand { get; private set; }
        public ICommand VisualizarDadosCommand { get; private set; }
        public ICommand AlternarModoSelecaoCommand { get; private set; }
        public ICommand ExcluirSelecionadosCommand { get; private set; }

        public AlunosViewModel()
        {
            _alunoService = new AlunoService();
            Alunos = new List<AlunoListaItem>();
            Turmas = new List<Turma>();

            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);
            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);
            FecharPerfilCommand = new RelayCommand(ExecutarFecharPerfil);
            VisualizarDadosCommand = new RelayCommand(ExecutarVisualizar);
            AlternarModoSelecaoCommand = new RelayCommand(() => ModoSelecaoMultipla = !ModoSelecaoMultipla);
            ExcluirSelecionadosCommand = new RelayCommand(ExecutarExcluirSelecionados);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarDadosIniciaisAsync();
        }

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = (DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>()).Select(CriarItemLista).ToList();
                InicializarComboBoxTurmas();
                ExecutarBusca();
            });
        }

        /// <summary>Soltura do evento estático de cache quando a view sai da tela,
        /// evitando acúmulo de ViewModels vivos na memória (leak de assinatura).</summary>
        public void Dispose()
        {
            DataCacheService.CacheAtualizado -= OnCacheAtualizado;
        }

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

                _listaOriginalDoBanco = (DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>()).Select(CriarItemLista).ToList();
                Alunos = _listaOriginalDoBanco;

                InicializarComboBoxTurmas();
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
                var listaDoCache = DataCacheService.TurmaModeloCache ?? new List<Turma>();

                var _listaCombo = new List<Turma>
                {
                    new Turma { Id = 0, Nome = "Selecionar turma" }
                };

                _listaCombo.AddRange(listaDoCache);
                Turmas = _listaCombo;
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

                // LÓGICA CORRIGIDA: Filtra de forma inteligente combinando texto E turma
                Alunos = _listaOriginalDoBanco.Where(a =>
                {
                    // 1. Verifica se bate com a busca por texto (se o campo estiver vazio, retorna true)
                    bool bateTexto = string.IsNullOrWhiteSpace(buscaTexto) ||
                                     (a.Nome != null && a.Nome.ToLower().Contains(buscaTexto)) ||
                                     (a.Email != null && a.Email.ToLower().Contains(buscaTexto)) ||
                                     (a.Matricula != null && a.Matricula.ToLower().Contains(buscaTexto));

                    // 2. Verifica se bate com o filtro de turma selecionada
                    bool bateTurma = (turmaFiltro == "Selecionar turma") ||
                                     (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Trim().Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    // O aluno só entra na Grid se passar nas duas condições!
                    return bateTexto && bateTurma;
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

        private void ExecutarVisualizar(object obj)
        {
            if (obj is AlunoExibicaoDTO aluno)
            {
                AlunoSelecionado = aluno;
            }
        }

        private void ExecutarFecharPerfil(object obj)
        {
            AlunoSelecionado = null;
        }

        private void ExecutarEditar(object obj)
        {
            if (obj is AlunoExibicaoDTO aluno)
            {
                WindowService.AbrirEditarAluno(aluno);
            }
        }

        private void ExecutarExcluir(object obj)
        {
            MessageBoxResult resultado = PopupMensagem.Show("Tem certeza que deseja excluir este aluno? Essa ação excluirá todos os dados associados a ele", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (resultado == MessageBoxResult.Yes && obj is AlunoExibicaoDTO aluno)
            {
                try
                {
                    _alunoService.Excluir(aluno.Matricula);
                    AtualizarCacheAposMudanca();
                }
                catch (Exception ex)
                {
                    MostrarErro("Não foi possível excluir o aluno. Tente novamente.", ex);
                }
            }
        }

        private void ExecutarExcluirSelecionados()
        {
            var selecionados = _listaOriginalDoBanco.Where(a => a.IsSelected).ToList();
            if (selecionados.Count == 0)
            {
                PopupMensagem.Show("Selecione pelo menos um aluno para excluir.", "Nenhum aluno selecionado", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult resultado = PopupMensagem.Show(
                $"A exclusão removerá permanentemente {selecionados.Count} aluno(s), seus vínculos e registros de ausência. Deseja continuar?",
                "Excluir alunos selecionados",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                _alunoService.Excluir(selecionados.Select(a => a.Matricula).ToList());
                ModoSelecaoMultipla = false;
                AtualizarCacheAposMudanca();
            }
            catch (Exception ex) { MostrarErro("Não foi possível excluir os alunos selecionados. Nenhum aluno foi excluído.", ex); }
        }

        private static AlunoListaItem CriarItemLista(AlunoExibicaoDTO aluno)
        {
            return new AlunoListaItem
            {
                Nome = aluno.Nome,
                Matricula = aluno.Matricula,
                Turma = aluno.Turma,
                Email = aluno.Email,
                DadosFrequencia = aluno.DadosFrequencia,
                DataEntrada = aluno.DataEntrada
            };
        }

        private async void AtualizarCacheAposMudanca()
        {
            // O dado já foi gravado no banco; uma falha de conexão na atualização
            // do cache não deve derrubar a aplicação — apenas avisar o usuário.
            try
            {
                await DataCacheService.ForçarAtualizacaoAsync();
            }
            catch (Exception ex)
            {
                MostrarErro("A alteração foi salva no banco, mas a lista não conseguiu atualizar. Reabra a tela de alunos para ver os dados atualizados.", ex);
            }
        }
    }
}
