using Microsoft.Win32; // Use este, é o padrão do WPF
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly RelatoriosViewModel _pai;
        private List<AlunoRelatorioItem> _alunos;
        private bool _selecionarTodos;
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
                        // Altera o estado do aluno
                        aluno.IsSelected = _selecionarTodos;

                        // IMPORTANTE: Isso força a linha do DataGrid a atualizar o checkbox dela
                        aluno.NotificarSelecaoAlterada();
                    }
                }
            }
        }
        public List<AlunoRelatorioItem> Alunos
        {
            get => _alunos;
            set { _alunos = value; OnPropertyChanged(); }
        }

        public ICommand VoltarCommand { get; }
        public ICommand GerarRelatorioLoteCommand { get; }

        public RelatorioAlunosViewModel(RelatoriosViewModel pai)
        {
            _pai = pai;
            VoltarCommand = new RelayCommand(() => _pai.ConteudoAtual = null);
            GerarRelatorioLoteCommand = new RelayCommand(GerarRelatorioLote);

            _ = CarregarDadosAsync();
        }

        private async Task CarregarDadosAsync()
        {
            while (!DataCacheService.IsCarregado)
            {
                await Task.Delay(100);
            }

            // Carrega direto na lista única
            Alunos = DataCacheService.AlunosCache?.Select(a => new AlunoRelatorioItem
            {
                Nome = a.Nome,
                Matricula = a.Matricula,
                Turma = a.Turma,
                Email = a.Email,
                DadosFrequencia = a.DadosFrequencia,
                IsSelected = false
            }).ToList() ?? new List<AlunoRelatorioItem>();
        }

        private void GerarRelatorioLote()
        {
            var selecionados = Alunos.Where(a => a.IsSelected).ToList();
            if (!selecionados.Any()) return;

            var dialog = new SaveFileDialog { Filter = "HTML|*.html", FileName = "Relatorios.html" };
            if (dialog.ShowDialog() == true)
            {
                var html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
                // CSS IDÊNTICO AO SEU ORIGINAL
                html.AppendLine("<style>");
                html.AppendLine("    body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
                html.AppendLine("    h1 { margin-bottom: 8px; color: #236B73; }");
                html.AppendLine("    .card-perf { background: #ECF8F6; border-radius: 8px; padding: 20px; border: 1px solid #B0C8D6; margin-top: 15px; }");
                html.AppendLine("    p { font-size: 15px; line-height: 1.6; margin: 6px 0; }");
                html.AppendLine("    .destaque { font-size: 20px; font-weight: bold; color: #236B73; }");
                html.AppendLine("    .falta { color: #AA3333; font-weight: bold; }");
                html.AppendLine("</style></head><body>");

                // TÍTULO ÚNICO
                html.AppendLine("<h1>Ficha de Rendimento e Assiduidade</h1>");

                // LOOP APENAS DOS CARTÕES
                foreach (var aluno in selecionados)
                {
                    html.AppendLine(GerarCartaoAluno(aluno));
                }

                html.AppendLine("</body></html>");
                File.WriteAllText(dialog.FileName, html.ToString(), Encoding.UTF8);
                Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
        }

        private string GerarCartaoAluno(AlunoExibicaoDTO aluno)
        {
            var html = new StringBuilder();
            html.AppendLine("    <div class=\"card-perf\">");
            html.AppendLine($"        <p><strong>Nome Completo:</strong> {WebUtility.HtmlEncode(aluno.Nome)}</p>");
            html.AppendLine($"        <p><strong>Número de Matrícula:</strong> {WebUtility.HtmlEncode(aluno.Matricula)}</p>");
            html.AppendLine($"        <p><strong>Vínculo de Turma:</strong> {WebUtility.HtmlEncode(aluno.Turma)}</p>");
            html.AppendLine($"        <p><strong>E-mail Institucional:</strong> {WebUtility.HtmlEncode(aluno.Email)}</p>");
            html.AppendLine("        <hr style='border: 0; border-top: 1px solid #B0C8D6; margin: 15px 0;'>");
            html.AppendLine($"        <p class='destaque'>Assiduidade Geral do Aluno: {aluno.DadosFrequencia?.Assiduidade}%</p>");
            html.AppendLine($"        <p>Total de Aulas Computadas: {aluno.DadosFrequencia?.TotalAulas} aulas</p>");
            html.AppendLine($"        <p class='falta'>Total de Ausências Registradas: {aluno.DadosFrequencia?.TotalFaltas} faltas</p>");
            html.AppendLine("    </div>");
            return html.ToString();
        }
    }
}