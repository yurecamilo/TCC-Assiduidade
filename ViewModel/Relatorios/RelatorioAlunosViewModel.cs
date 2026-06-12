using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            while (!DataCacheService.IsCarregado)
            {
                await Task.Delay(100);
            }

            _listaOriginalDoBanco = DataCacheService.AlunosCache ?? new List<AlunoExibicaoDTO>();
            InicializarComboBoxTurmas();
            ExecutarBusca();
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
                MessageBox.Show("Erro ao carregar turmas do cache: " + ex.Message);
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
                MessageBox.Show("Erro durante a filtragem combinada de alunos: " + ex.Message);
            }
        }

        private void ExecutarLimparBusca()
        {
            TextoBusca = string.Empty;
        }

        private void GerarRelatorioLote()
        {
            var selecionados = Alunos.Where(a => a.IsSelected).ToList();
            if (!selecionados.Any()) return;

            var dialog = new SaveFileDialog { Filter = "HTML|*.html", FileName = "Relatorios.html" };
            if (dialog.ShowDialog() == true)
            {
                var html = _relatorioService.RelatorioPorAluno(selecionados);

                File.WriteAllText(dialog.FileName, html.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
    }
}