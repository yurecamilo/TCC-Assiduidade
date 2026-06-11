using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

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
        public ICommand EditarTurmaCommand { get; private set; }
        public ICommand ExcluirTurmaCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }
        public ICommand VisualizarDadosCommand { get; private set; }

        public AlunosViewModel()
        {
            Alunos = new List<AlunoExibicaoDTO>();
            Turmas = new List<Turma>(); 

            BuscarCommand = new RelayCommand(ExecutarBusca);
            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);
            FecharPerfilCommand = new RelayCommand(ExecutarFecharPerfil);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);
            VisualizarDadosCommand = new RelayCommand(ExecutarVisualizar);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarDadosIniciaisAsync();
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
                MessageBox.Show("Erro ao carregar alunos do cache: " + ex.Message);
            }
        }

        private void InicializarComboBoxTurmas()
        {
            try
            {
                var _listaCombo = DataCacheService.TurmaModeloCache ?? new List<Turma>();
                Turmas = _listaCombo;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar turmas do cache: " + ex.Message);
            }
        }

        private void ExecutarBusca()
        {
            try
            {
                string buscaTexto = (TextoBusca ?? "").Trim().ToLower();
                string turmaFiltro = (TurmaSelecionada?.Nome ?? "Todas as turmas").Trim();

                Alunos = _listaOriginalDoBanco.Where(a =>
                {
                    bool bateTexto = string.IsNullOrWhiteSpace(buscaTexto) ||
                                     (a.Nome != null && a.Nome.ToLower().Contains(buscaTexto)) ||
                                     (a.Email != null && a.Email.ToLower().Contains(buscaTexto)) ||
                                     (a.Matricula != null && a.Matricula.ToLower().Contains(buscaTexto));

                    bool bateTurma = (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Trim().Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    return bateTexto && bateTurma;
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro durante a filtragem combinada de alunos: " + ex.Message);
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
                AlunoSelecionado = aluno; // Atribui o aluno clicado, forçando a aba a abrir!
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

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();

                InicializarComboBoxTurmas();

                ExecutarBusca();
            });
        }
    }
}