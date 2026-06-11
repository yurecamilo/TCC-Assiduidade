using Microsoft.Win32; // Padrão do WPF
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    // Elemento do DataGrid que herda do seu DTO de Turma (Ajuste o nome do DTO caso seja diferente, ex: TurmaExibicaoDTO ou Turma)
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
                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang=\"pt-BR\">");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset=\"utf-8\">");
                html.AppendLine($"    <title>Relatório Geral Por Turma</title>");
                html.AppendLine("    <style>");
                html.AppendLine("        body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
                html.AppendLine("        h1 { margin-bottom: 8px; color: #236B73; }");
                html.AppendLine("        .info { margin-bottom: 24px; font-size: 16px; }");
                html.AppendLine("        table { width: 100%; border-collapse: collapse; margin-top: 15px; }");
                html.AppendLine("        th, td { border: 1px solid #cbd2d9; padding: 10px; text-align: left; }");
                html.AppendLine("        th { background: #f0f4f8; }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");
                html.AppendLine($"    <h1>Relatório de Histórico por Turma</h1>");

                foreach (var turma in selecionadas)
                {
                    html.AppendLine(GerarCartaoTurma(turma));
                }

                html.AppendLine("</body></html>");
                File.WriteAllText(dialog.FileName, html.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }

        private string GerarCartaoTurma(TurmaExibicaoDTO turma)
        {
            List<AulaExibicaoDTO> aulasDaTurma = DataCacheService.AulasCache?
                        .Where(a => a.Turma != null && a.Turma.Equals(turma.TurmaOriginal.Id.ToString(), StringComparison.OrdinalIgnoreCase))
                        .ToList() ?? new List<AulaExibicaoDTO>();


            var html = new StringBuilder();
            html.AppendLine("    <div class=\"info\">");
            html.AppendLine($"        <p><strong>Turma analisada:</strong> {WebUtility.HtmlEncode(turma.Nome)}</p>");
            html.AppendLine($"        <p><strong>Total de diários de classe mapeados:</strong> {aulasDaTurma.Count}</p>");
            html.AppendLine("    </div>");

            if (aulasDaTurma.Count == 0)
            {
                html.AppendLine("    <p>Não há histórico de chamadas salvas para esta turma no sistema.</p>");
            }
            else
            {
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>Data da Aula</th>");
                html.AppendLine("                <th>Número Absoluto de Ausências</th>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                foreach (var aula in aulasDaTurma)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine($"                <td>{aula.Data:dd/MM/yyyy}</td>");
                    html.AppendLine($"                <td>{aula.NumeroAusentes} alunos faltantes</td>");
                    html.AppendLine("            </tr>");
                }
                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
            }

            html.AppendLine("</body></html>");
            return html.ToString();

        }
    }
}