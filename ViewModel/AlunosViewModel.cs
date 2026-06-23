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
    public class AlunosViewModel : BaseViewModel
    {
        private List<AlunoExibicaoDTO> _listaOriginalDoBanco = new List<AlunoExibicaoDTO>();

        private IEnumerable<AlunoExibicaoDTO> _alunos;
        private AlunoExibicaoDTO _alunoSelecionado;
        private string _textoBusca;
        private List<Turma> _turmas;
        private Turma _turmaSelecionada;

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

        public IEnumerable<AlunoExibicaoDTO> Alunos
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

        public AlunosViewModel()
        {
            Alunos = new List<AlunoExibicaoDTO>();
            Turmas = new List<Turma>();

            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);
            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);
            FecharPerfilCommand = new RelayCommand(ExecutarFecharPerfil);
            VisualizarDadosCommand = new RelayCommand(ExecutarVisualizar);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarDadosIniciaisAsync();
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

        private async Task CarregarDadosIniciaisAsync()
        {
            try
            {
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();
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
                }).Select(a => new AlunoRelatorioItem
                {
                    Nome = a.Nome,
                    Matricula = a.Matricula,
                    Turma = a.Turma,
                    Email = a.Email,
                    DadosFrequencia = a.DadosFrequencia,
                    DataEntrada = a.DataEntrada,
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
        }

        private void ExecutarExcluir(object obj)
        {
        }
    }
}
