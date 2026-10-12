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

namespace TCC_Assiduidade.ViewModel
{
    public class TurmasViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;
        private readonly AlunoService _alunoService;

        private List<TurmaExibicaoDTO> _listaOriginalDoBanco = new List<TurmaExibicaoDTO>();

        private IEnumerable<TurmaExibicaoDTO> _turmas;
        private TurmaExibicaoDTO _turmaSelecionada;
        private string _textoBusca;
        private bool _mostrarArquivadas;
        private bool _modoSelecaoMultipla;

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

        public bool MostrarArquivadas
        {
            get => _mostrarArquivadas;
            set { _mostrarArquivadas = value; OnPropertyChanged(); ExecutarBusca(); }
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
                if (!value)
                {
                    SelecionarTodas = false;
                    foreach (var turma in _listaOriginalDoBanco) turma.IsSelected = false;
                }
            }
        }
        public string TextoModoSelecao => ModoSelecaoMultipla ? "Cancelar seleção" : "Selecionar múltiplos";
        public string IconeModoSelecao => ModoSelecaoMultipla ? "×" : "☷";
        public bool MostrarAcoes => !ModoSelecaoMultipla;

        private bool _selecionarTodas;
        public bool SelecionarTodas
        {
            get => _selecionarTodas;
            set
            {
                _selecionarTodas = value;
                OnPropertyChanged();
                foreach (var turma in Turmas ?? Enumerable.Empty<TurmaExibicaoDTO>()) turma.IsSelected = value;
            }
        }

        public ICommand BuscarCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }
        public ICommand EditarTurmaCommand { get; private set; }
        public ICommand ExcluirTurmaCommand { get; private set; }
        public ICommand CadastrarTurmaCommand { get; private set; }
        public ICommand VisualizarTurmaCommand { get; private set; }
        public ICommand ArquivarTurmaCommand { get; private set; }
        public ICommand AlternarModoSelecaoCommand { get; private set; }
        public ICommand ExcluirSelecionadasCommand { get; private set; }
        public ICommand ArquivarSelecionadasCommand { get; private set; }
        public ICommand DesarquivarSelecionadasCommand { get; private set; }

        public TurmasViewModel()
        {
            _turmaService = new TurmaService();
            _alunoService = new AlunoService();
            Turmas = new List<TurmaExibicaoDTO>();

            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);

            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);
            VisualizarTurmaCommand = new RelayCommand(ExecutarVisualizar);
            ArquivarTurmaCommand = new RelayCommand(ExecutarArquivar);
            AlternarModoSelecaoCommand = new RelayCommand(() => ModoSelecaoMultipla = !ModoSelecaoMultipla);
            ExcluirSelecionadasCommand = new RelayCommand(ExcluirSelecionadas);
            ArquivarSelecionadasCommand = new RelayCommand(() => AlterarSelecionadas(EStatusTurma.Arquivada));
            DesarquivarSelecionadasCommand = new RelayCommand(() => AlterarSelecionadas(EStatusTurma.Ativa));

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarTurmasAsync();
        }

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = DataCacheService.TurmasHistoricoCache ?? new List<TurmaExibicaoDTO>();

                ExecutarBusca();
            });
        }

        private async Task CarregarTurmasAsync()
        {
            try
            {
                await DataCacheService.AguardarCarregamentoAsync();

                _listaOriginalDoBanco = DataCacheService.TurmasHistoricoCache ?? new List<TurmaExibicaoDTO>();
                ExecutarBusca();
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
                    Turmas = FiltrarPorStatus(_listaOriginalDoBanco);
                    return;
                }

                string busca = TextoBusca.Trim().ToLower();

                Turmas = FiltrarPorStatus(_listaOriginalDoBanco)
                    .Where(t => t.Nome != null && t.Nome.ToLower().Contains(busca))
                    .ToList();
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel filtrar as turmas.", ex);
            }
        }

        private IEnumerable<TurmaExibicaoDTO> FiltrarPorStatus(IEnumerable<TurmaExibicaoDTO> turmas)
        {
            return turmas.Where(t => t.TurmaOriginal.Status == EStatusTurma.Ativa ||
                (MostrarArquivadas && t.TurmaOriginal.Status == EStatusTurma.Arquivada));
        }

        private void ExecutarLimparBusca()
        {
            TextoBusca = string.Empty;
        }

        private void ExecutarEditar(object obj)
        {
            if (obj is TurmaExibicaoDTO turma)
            {
                if (turma.TurmaOriginal.Status != EStatusTurma.Ativa) return;
                WindowService.AbrirEditarTurma(turma);
            }
        }

        private void ExecutarExcluir(object obj)
        {
            if (!(obj is TurmaExibicaoDTO turma)) return;
            MessageBoxResult resultado = MessageBox.Show("Ao excluir esta turma, seus dados deixarão de estar disponíveis nas consultas e filtros. Alunos, vínculos, aulas e ausências serão preservados no banco. Essa ação não pode ser desfeita. Deseja confirmar?", "Excluir turma", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _turmaService.Excluir(turma.TurmaOriginal.Id);
                    AtualizarCacheAposMudanca();
                }
                catch (Exception ex)
                {
                    MostrarErro("Não foi possível excluir a turma. Tente novamente.", ex);
                }
            }
        }

        private void ExecutarArquivar(object obj)
        {
            if (!(obj is TurmaExibicaoDTO turma)) return;
            bool desarquivar = turma.TurmaOriginal.Status == EStatusTurma.Arquivada;
            var texto = desarquivar
                ? "Ao desarquivar esta turma, ela voltará a aparecer entre as turmas ativas e poderá receber novos vínculos e aulas. Deseja continuar?"
                : "Ao arquivar esta turma, seus dados continuarão disponíveis nas consultas de turmas e aulas arquivadas. Deseja confirmar a arquivação?";
            var resultado = MessageBox.Show(texto, desarquivar ? "Desarquivar turma" : "Arquivar turma", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado != MessageBoxResult.Yes) return;
            try
            {
                if (desarquivar) _turmaService.Desarquivar(turma.TurmaOriginal.Id);
                else _turmaService.Arquivar(turma.TurmaOriginal.Id);
                AtualizarCacheAposMudanca();
            }
            catch (Exception ex) { MostrarErro(desarquivar ? "Não foi possível desarquivar a turma." : "Não foi possível arquivar a turma.", ex); }
        }

        private List<TurmaExibicaoDTO> TurmasSelecionadas => _listaOriginalDoBanco.Where(t => t.IsSelected).ToList();

        private void ExcluirSelecionadas()
        {
            var selecionadas = TurmasSelecionadas.Where(t => t.TurmaOriginal.Status != EStatusTurma.Excluida).ToList();
            if (selecionadas.Count == 0) { MessageBox.Show("Selecione pelo menos uma turma."); return; }
            var confirmacao = MessageBox.Show($"Excluir {selecionadas.Count} turma(s)? Os alunos, vínculos, aulas e ausências serão preservados, mas ficarão indisponíveis. Essa ação não pode ser desfeita.", "Excluir turmas selecionadas", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirmacao != MessageBoxResult.Yes) return;
            ExecutarAlteracaoLote(selecionadas, EStatusTurma.Excluida, "A exclusão foi concluída.");
        }

        private void AlterarSelecionadas(EStatusTurma destino)
        {
            var selecionadas = TurmasSelecionadas.Where(t =>
                destino == EStatusTurma.Arquivada ? t.TurmaOriginal.Status == EStatusTurma.Ativa : t.TurmaOriginal.Status == EStatusTurma.Arquivada).ToList();
            if (selecionadas.Count == 0) { MessageBox.Show(destino == EStatusTurma.Arquivada ? "Selecione turmas ativas para arquivar." : "Selecione turmas arquivadas para desarquivar."); return; }
            string acao = destino == EStatusTurma.Arquivada ? "arquivar" : "desarquivar";
            var confirmar = MessageBox.Show($"{acao} {selecionadas.Count} turma(s)? Os dados associados serão preservados.", "Confirmar operação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmar != MessageBoxResult.Yes) return;
            ExecutarAlteracaoLote(selecionadas, destino, "A operação foi concluída.");
        }

        private void ExecutarAlteracaoLote(List<TurmaExibicaoDTO> selecionadas, EStatusTurma destino, string mensagemSucesso)
        {
            try
            {
                var alteracoes = selecionadas.Select(t => Tuple.Create(t.TurmaOriginal.Id, t.TurmaOriginal.Status, destino)).ToList();
                _turmaService.AlterarStatusEmLote(alteracoes);
                ModoSelecaoMultipla = false;
                AtualizarCacheAposMudanca();
                MessageBox.Show(mensagemSucesso, "Turmas atualizadas", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MostrarErro("Não foi possível atualizar as turmas. Nenhuma turma foi alterada.", ex); }
        }

        private void ExecutarVisualizar(object obj)
        {
            if (obj is TurmaExibicaoDTO turma)
            {
                try { WindowService.AbrirVisualizarTurma(turma); }
                catch (Exception ex) { MostrarErro("Não foi possível abrir os detalhes da turma.", ex); }
            }
        }

        public bool PodeArquivar(TurmaExibicaoDTO turma) => turma?.TurmaOriginal?.Status == EStatusTurma.Ativa;
        public bool PodeEditar(TurmaExibicaoDTO turma) => turma?.TurmaOriginal?.Status == EStatusTurma.Ativa;
        public bool PodeExcluir(TurmaExibicaoDTO turma) => turma?.TurmaOriginal?.Status != null && turma.TurmaOriginal.Status != EStatusTurma.Excluida;
        private async void AtualizarCacheAposMudanca()
        {
            try { await DataCacheService.ForçarAtualizacaoAsync(); }
            catch (Exception ex) { MostrarErro("A alteração foi salva, mas a lista não conseguiu atualizar. Recarregue a tela para consultar os dados.", ex); }
        }

    }
}
