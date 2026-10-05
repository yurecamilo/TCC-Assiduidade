using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class TurmasViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;

        private List<TurmaExibicaoDTO> _listaOriginalDoBanco = new List<TurmaExibicaoDTO>();

        private IEnumerable<TurmaExibicaoDTO> _turmas;
        private TurmaExibicaoDTO _turmaSelecionada;
        private string _textoBusca;

        public IEnumerable<TurmaExibicaoDTO> Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public TurmaExibicaoDTO TurmaSelecionada
        {
            get => _turmaSelecionada;
            set { _turmaSelecionada = value; OnPropertyChanged(); }
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

        public ICommand BuscarCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }
        public ICommand EditarTurmaCommand { get; private set; }
        public ICommand ExcluirTurmaCommand { get; private set; }
        public ICommand CadastrarTurmaCommand { get; private set; }
        public ICommand VisualizarTurmaCommand { get; private set; }

        public TurmasViewModel()
        {
            _turmaService = new TurmaService();
            Turmas = new List<TurmaExibicaoDTO>();

            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);

            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);
            VisualizarTurmaCommand = new RelayCommand(ExecutarVisualizar);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarTurmasAsync();
        }

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = DataCacheService.TurmasCache ?? new List<TurmaExibicaoDTO>();

                ExecutarBusca();
            });
        }

        private async Task CarregarTurmasAsync()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

                _listaOriginalDoBanco = DataCacheService.TurmasCache ?? new List<TurmaExibicaoDTO>();
                Turmas = _listaOriginalDoBanco;
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
                if (string.IsNullOrWhiteSpace(TextoBusca))
                {
                    Turmas = _listaOriginalDoBanco;
                    return;
                }

                string busca = TextoBusca.Trim().ToLower();

                Turmas = _listaOriginalDoBanco
                    .Where(t => t.Nome != null && t.Nome.ToLower().Contains(busca))
                    .ToList();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel filtrar as turmas.", ex);
            }
        }

        private void ExecutarLimparBusca()
        {
            TextoBusca = string.Empty;
        }

        private void ExecutarEditar(object obj)
        {
            if (obj is TurmaExibicaoDTO turma)
            {
                WindowService.AbrirEditarTurma(turma);
            }
            AtualizarCacheAposMudanca();
        }

        private void ExecutarExcluir(object obj)
        {
            if (!(obj is TurmaExibicaoDTO turma))
                return;

            MessageBoxResult resultado = MessageBox.Show(
                "Deseja excluir também todos os alunos desta turma?\n\n" +
                "Sim: exclui a turma, as chamadas e os alunos.\n" +
                "Não: arquiva a turma e preserva alunos, chamadas e histórico.\n" +
                "Cancelar: não exclui nada.",
                "Excluir turma",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Cancel)
            {
                try
                {
                    bool excluirAlunos = resultado == MessageBoxResult.Yes;
                    _turmaService.Excluir(turma.TurmaOriginal.Id, excluirAlunos);
                    AtualizarCacheAposMudanca();
                }
                catch (Exception ex)
                {
                    MostrarErro("Não foi possível excluir a turma. Tente novamente.", ex);
                }
            }
        }

        private void ExecutarVisualizar(object obj)
        {
            if (obj is TurmaExibicaoDTO turma)
            {
                WindowService.AbrirVisualizarTurma(turma);
            }
        }
        private async void AtualizarCacheAposMudanca()
        {
            await DataCacheService.ForçarAtualizacaoAsync();
        }

    }
}
