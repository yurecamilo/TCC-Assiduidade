using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Servicos;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class ResumoAulaViewModel : BaseViewModel
    {
        private readonly RelatorioService _relatorioService;
        private AulaExibicaoDTO _aulaVisualizar;
        private List<RelatorioAusente> _alunosAusentes;

        public AulaExibicaoDTO AulaVisualizar
        {
            get => _aulaVisualizar;
            set { _aulaVisualizar = value; OnPropertyChanged(); }
        }

        public List<RelatorioAusente> AlunosAusentes
        {
            get => _alunosAusentes;
            set { _alunosAusentes = value; OnPropertyChanged(); }
        }

        public ICommand GerarHtmlCommand { get; private set; }
        public ICommand FecharVisualizacaoCommand { get; private set; }

        public ResumoAulaViewModel(AulaExibicaoDTO aula)
        {
            _relatorioService = new RelatorioService();
            AulaVisualizar = aula;
            GerarHtmlCommand = new RelayCommand(GerarHtml);
            CarregarDadosAusentes(aula);
        }

        private void CarregarDadosAusentes(AulaExibicaoDTO aula)
        {
            try
            {
                var dados = _relatorioService.ObterDadosRelatorio(aula.AulaId);

                AlunosAusentes = dados ?? new List<RelatorioAusente>();
            }
            catch (Exception ex)
            {
                AlunosAusentes = new List<RelatorioAusente>();
                MessageBox.Show("Erro ao buscar ausentes: " + ex.Message);
            }
        }

        private void GerarHtml(object parametro)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Salvar relatorio HTML",
                    Filter = "Arquivos HTML (*.html)|*.html|Todos os arquivos (*.*)|*.*",
                    FileName = SanitizarNomeArquivo("Relatorio_Aula_" + AulaVisualizar.AulaId + "_" + AulaVisualizar.Turma + ".html")
                };

                if (dialog.ShowDialog() != true) return;

                // Usa as propriedades locais gerenciadas de forma independente pela Window
                string html = MontarHtml(AulaVisualizar, AlunosAusentes);

                File.WriteAllText(dialog.FileName, html, Encoding.UTF8);
                Process.Start(new ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });

                MessageBox.Show("Relatorio HTML gerado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao gerar relatorio HTML: " + ex.Message);
            }
        }

        // 🌟 SEU MÉTODO ORIGINAL INTACTO (Copiado e colado linha por linha)
        private string MontarHtml(AulaExibicaoDTO aula, List<RelatorioAusente> ausentes)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine("    <title>Relatorio de Ausentes</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
            html.AppendLine("        h1 { margin-bottom: 8px; }");
            html.AppendLine("        .info { margin-bottom: 24px; font-size: 16px; }");
            html.AppendLine("        table { width: 100%; border-collapse: collapse; }");
            html.AppendLine("        th, td { border: 1px solid #cbd2d9; padding: 10px; text-align: left; }");
            html.AppendLine("        th { background: #f0f4f8; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <h1>Relatorio de Ausentes</h1>");
            html.AppendLine("    <div class=\"info\">");
            html.AppendLine("        <p><strong>Data da aula:</strong> " + aula.Data.ToString("dd/MM/yyyy") + "</p>");
            html.AppendLine("        <p><strong>Turma:</strong> " + WebUtility.HtmlEncode(aula.Turma) + "</p>");
            html.AppendLine("        <p><strong>Total de ausentes:</strong> " + aula.NumeroAusentes + "</p>");
            html.AppendLine("    </div>");

            if (ausentes == null || ausentes.Count == 0)
            {
                html.AppendLine("    <p>Nao houve ausencias nesta aula.</p>");
            }
            else
            {
                html.AppendLine("    <table>");
                html.AppendLine("        <thead>");
                html.AppendLine("            <tr>");
                html.AppendLine("                <th>Matricula</th>");
                html.AppendLine("                <th>Nome</th>");
                html.AppendLine("                <th>Email</th>");
                html.AppendLine("            </tr>");
                html.AppendLine("        </thead>");
                html.AppendLine("        <tbody>");

                foreach (RelatorioAusente ausente in ausentes)
                {
                    html.AppendLine("            <tr>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Matricula) + "</td>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Aluno) + "</td>");
                    html.AppendLine("                <td>" + WebUtility.HtmlEncode(ausente.Email) + "</td>");
                    html.AppendLine("            </tr>");
                }

                html.AppendLine("        </tbody>");
                html.AppendLine("    </table>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private string SanitizarNomeArquivo(string nome)
        {
            foreach (char caractere in Path.GetInvalidFileNameChars())
            {
                nome = nome.Replace(caractere, '_');
            }
            return nome;
        }
    }
}