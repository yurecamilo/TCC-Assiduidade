using Microsoft.Win32; // Padrão do WPF
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Relatorios
{
    public class TurmaRelatorioItem : TurmaExibicaoDTO
    {
        public void NotificarSelecaoAlterada()
        {
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public class RelatorioTurmasViewModel : BaseViewModel, IDisposable
    {
        private List<TurmaExibicaoDTO> _listaOriginalDoBanco = new List<TurmaExibicaoDTO>();
        private readonly RelatorioService _relatorioService = new RelatorioService();
        private readonly RelatoriosViewModel _pai;
        private bool _selecionarTodasTurmas;

        private ICollectionView _turmas;
        private List<TurmaRelatorioItem> _itensTurma = new List<TurmaRelatorioItem>();
        private ICollectionView _turmasView;
        private string _textoBusca;
        private bool _mostrarArquivadas;

        public bool MostrarArquivadas
        {
            get => _mostrarArquivadas;
            set
            {
                if (_mostrarArquivadas == value) return;
                _mostrarArquivadas = value;
                OnPropertyChanged();
                AtualizarFiltro();
            }
        }

        public string TextoBusca
        {
            get => _textoBusca;
            set
            {
                _textoBusca = value;
                OnPropertyChanged();
                AtualizarFiltro();
            }
        }

        public bool SelecionarTodasTurmas
        {
            get => _selecionarTodasTurmas;
            set
            {
                _selecionarTodasTurmas = value;
                OnPropertyChanged(nameof(SelecionarTodasTurmas));

                if (Turmas != null)
                {
                    foreach (var turma in Turmas.Cast<TurmaRelatorioItem>())
                    {
                        // Altera o estado da turma
                        turma.IsSelected = _selecionarTodasTurmas;

                        // Força a linha do DataGrid a atualizar o checkbox dela
                        turma.NotificarSelecaoAlterada();
                    }
                }
            }
        }

        public ICollectionView Turmas
        {
            get => _turmas;
            set { _turmas = value; OnPropertyChanged(); }
        }

        public ICommand BuscarCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }
        public ICommand VoltarCommand { get; }
        public ICommand GerarRelatorioTurmasLoteCommand { get; }

        public RelatorioTurmasViewModel(RelatoriosViewModel pai)
        {
            _pai = pai;

            VoltarCommand = new RelayCommand(() => _pai.ConteudoAtual = null);
            GerarRelatorioTurmasLoteCommand = new RelayCommand(GerarRelatorioTurmasLote);

            BuscarCommand = new RelayCommand(ExecutarBusca);
            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);

            DataCacheService.CacheAtualizado += OnCacheAtualizado;

            _ = CarregarDadosAsync();
        }

        private void OnCacheAtualizado()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _listaOriginalDoBanco = DataCacheService.TurmasHistoricoCache ?? new List<TurmaExibicaoDTO>();

                ExecutarBusca();
            });
        }

        /// <summary>Soltura do evento estático de cache quando a view sai da tela,
        /// evitando acúmulo de ViewModels vivos na memória (leak de assinatura).</summary>
        public void Dispose()
        {
            DataCacheService.CacheAtualizado -= OnCacheAtualizado;
        }

        private void ExecutarBusca()
        {
            try
            {
                _itensTurma = (_listaOriginalDoBanco ?? new List<TurmaExibicaoDTO>())
                    .Select(t => new TurmaRelatorioItem
                    {
                        TurmaOriginal = t.TurmaOriginal,
                        Nome = t.Nome,
                        QuantidadeAlunos = t.QuantidadeAlunos,
                        IsSelected = false
                    }).ToList();
                _turmasView = new ListCollectionView(_itensTurma);
                _turmasView.Filter = FiltrarTurma;
                Turmas = _turmasView;
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

        private bool FiltrarTurma(object item)
        {
            if (!(item is TurmaRelatorioItem turma)) return false;
            if (!MostrarArquivadas && turma.TurmaOriginal?.Status != EStatusTurma.Ativa) return false;
            string busca = (TextoBusca ?? string.Empty).Trim();
            return string.IsNullOrEmpty(busca) ||
                   (!string.IsNullOrEmpty(turma.Nome) && turma.Nome.IndexOf(busca, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private void AtualizarFiltro()
        {
            try
            {
                _turmasView?.Refresh();
            }
            catch (Exception ex)
            {
                MostrarErro("Não foi possível atualizar o filtro de turmas.", ex);
            }
        }

        private async Task CarregarDadosAsync()
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

        private bool _gerandoRelatorioLote;

        private async void GerarRelatorioTurmasLote()
        {
            if (_gerandoRelatorioLote) return;
            _gerandoRelatorioLote = true;

            try
            {
                var selecionadas = Turmas.Cast<TurmaRelatorioItem>().Where(t => t.IsSelected).ToList();
                if (!selecionadas.Any()) return;

                // Leitura do histórico no banco em trabalho de fundo; a interface não congela.
                string html = await Task.Run(() => _relatorioService.RelatorioPorTurma(selecionadas));
                ArquivoService.SalvarHtmlEAbrir(html, "Relatorio_Turmas.html");
            }
            catch (Exception ex)
            {
                MostrarErro("Nao foi possivel gerar o relatorio de turmas.", ex);
            }
            finally
            {
                _gerandoRelatorioLote = false;
            }
        }
    }
}
