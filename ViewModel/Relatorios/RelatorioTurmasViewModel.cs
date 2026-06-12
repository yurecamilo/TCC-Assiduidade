using Microsoft.Win32; // Padrão do WPF
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel.Relatorios
{
    public class TurmaRelatorioItem : TurmaExibicaoDTO
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

    public class RelatorioTurmasViewModel : BaseViewModel
    {
        private List<TurmaExibicaoDTO> _listaOriginalDoBanco = new List<TurmaExibicaoDTO>();
        private readonly RelatorioService _relatorioService = new RelatorioService();
        private readonly RelatoriosViewModel _pai;
        private bool _selecionarTodasTurmas;

        private List<TurmaRelatorioItem> _turmas;
        private string _textoBusca;

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

        public bool SelecionarTodasTurmas
        {
            get => _selecionarTodasTurmas;
            set
            {
                _selecionarTodasTurmas = value;
                OnPropertyChanged(nameof(SelecionarTodasTurmas));

                if (Turmas != null)
                {
                    foreach (var turma in Turmas)
                    {
                        // Altera o estado da turma
                        turma.IsSelected = _selecionarTodasTurmas;

                        // Força a linha do DataGrid a atualizar o checkbox dela
                        turma.NotificarSelecaoAlterada();
                    }
                }
            }
        }

        public List<TurmaRelatorioItem> Turmas
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
                _listaOriginalDoBanco = DataCacheService.TurmasCache ?? new List<TurmaExibicaoDTO>();

                ExecutarBusca();
            });
        }

        private void ExecutarBusca()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TextoBusca))
                {
                    Turmas = _listaOriginalDoBanco.Select(t => new TurmaRelatorioItem
                    {
                        TurmaOriginal = t.TurmaOriginal,
                        Nome = t.Nome,
                        QuantidadeAlunos = t.QuantidadeAlunos,
                        IsSelected = false
                    }).ToList() ?? new List<TurmaRelatorioItem>();
                    return;
                }

                string busca = TextoBusca.Trim().ToLower();

                Turmas = _listaOriginalDoBanco
                    .Where(t => t.Nome != null && t.Nome.ToLower().Contains(busca))
                    .Select(t => new TurmaRelatorioItem
                    {
                        TurmaOriginal = t.TurmaOriginal,
                        Nome = t.Nome,
                        QuantidadeAlunos = t.QuantidadeAlunos,
                        IsSelected = false
                    })
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

        private async Task CarregarDadosAsync()
        {
            while (!DataCacheService.IsCarregado)
            {
                await Task.Delay(100);
            }

            _listaOriginalDoBanco = DataCacheService.TurmasCache ?? new List<TurmaExibicaoDTO>();

            Turmas = _listaOriginalDoBanco.Select(t => new TurmaRelatorioItem
            {
                TurmaOriginal = t.TurmaOriginal,
                Nome = t.Nome,
                QuantidadeAlunos = t.QuantidadeAlunos,
                IsSelected = false
            }).ToList() ?? new List<TurmaRelatorioItem>();
        }

        private void GerarRelatorioTurmasLote()
        {
            var selecionadas = Turmas.Where(t => t.IsSelected).ToList();
            if (!selecionadas.Any()) return;

            var dialog = new SaveFileDialog { Filter = "HTML|*.html", FileName = "Relatorio_Turmas.html" };
            if (dialog.ShowDialog() == true)
            {
                var html = _relatorioService.RelatorioPorTurma(selecionadas);

                File.WriteAllText(dialog.FileName, html.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }
    }
}