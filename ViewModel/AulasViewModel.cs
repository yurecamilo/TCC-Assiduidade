using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.View.Popups; // 🌟 Certifique-se de que a pasta dos seus pop-ups está importada
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class AulasViewModel : BaseViewModel
    {
        private List<AulaExibicaoDTO> _listaOriginalDoBanco = new List<AulaExibicaoDTO>();

        private IEnumerable<TurmaExibicaoDTO> _turmas;
        private TurmaExibicaoDTO _turmaSelecionada;
        private IEnumerable<AulaExibicaoDTO> _aulas;
        private AulaExibicaoDTO _aulaSelecionada;

        private DateTime? _dataInicio;
        private DateTime? _dataFim;

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

        // Comandos expostos para a View
        public ICommand LimparDatasCommand { get; private set; }

        public AulasViewModel()
        {
            // Inicialização vazia (null) para exibir o placeholder "Selecionar data"
            DataInicio = null;
            DataFim = null;

            // Instanciação dos Comandos
            LimparDatasCommand = new RelayCommand(ExecutarLimparDatas);

            InicializarComboBoxTurmas();
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

                _listaOriginalDoBanco = DataCacheService.AulasCache ?? new List<AulaExibicaoDTO>();
                Aulas = _listaOriginalDoBanco;

                InicializarComboBoxTurmas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar aulas do cache: " + ex.Message);
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
                    // 1. Filtro de Turma
                    bool bateTurma = (turmaFiltro == "Todas as turmas") ||
                                     (a.Turma != null && a.Turma.Equals(turmaFiltro, StringComparison.OrdinalIgnoreCase));

                    // 2. Se estiver nulo, aceita tudo. Se tiver data, filtra maior ou igual (>=)
                    bool bateDataInicio = !DataInicio.HasValue || a.Data.Date >= DataInicio.Value.Date;

                    // 3. Se estiver nulo, aceita tudo. Se tiver data, filtra menor ou igual (<=)
                    bool bateDataFim = !DataFim.HasValue || a.Data.Date <= DataFim.Value.Date;

                    return bateTurma && bateDataInicio && bateDataFim;
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro durante a filtragem por nome de turma: " + ex.Message);
            }
        }

        private void ExecutarLimparDatas()
        {
            DataInicio = null;
            DataFim = null;
        }

    }
}