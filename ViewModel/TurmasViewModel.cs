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
    public class TurmasViewModel : BaseViewModel
    {
        private readonly TurmaService _turmaService;

        // Cache local fortemente tipado para a filtragem reativa (Busca)
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
        public ICommand EditarTurmaCommand { get; private set; }
        public ICommand ExcluirTurmaCommand { get; private set; }
        public ICommand LimparBuscaCommand { get; private set; }

        public TurmasViewModel()
        {
            _turmaService = new TurmaService();
            Turmas = new List<TurmaExibicaoDTO>();

            BuscarCommand = new RelayCommand(ExecutarBusca);
            EditarTurmaCommand = new RelayCommand(ExecutarEditar);
            ExcluirTurmaCommand = new RelayCommand(ExecutarExcluir);

            LimparBuscaCommand = new RelayCommand(ExecutarLimparBusca);

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
                while (!DataCacheService.IsCarregado)
                {
                    await Task.Delay(100);
                }

                _listaOriginalDoBanco = DataCacheService.TurmasCache ?? new List<TurmaExibicaoDTO>();
                Turmas = _listaOriginalDoBanco;
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
                MessageBox.Show("Erro durante a filtragem: " + ex.Message);
            }
        }

        private void ExecutarLimparBusca()
        {
            TextoBusca = string.Empty;
        }

        private void ExecutarEditar(object obj)
        {
            // Sua lógica de edição...
        }

        private void ExecutarExcluir(object obj)
        {
            // Sua lógica de exclusão...
        }

        private async void AtualizarCacheAposMudanca()
        {
            await DataCacheService.ForçarAtualizacaoAsync();
        }

    }
}