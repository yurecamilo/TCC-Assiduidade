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
using TCC_Assiduidade.ViewModels.Base;

namespace TCC_Assiduidade.ViewModel
{
    public class RelatoriosAulasViewModel : BaseViewModel
    {
        private readonly AulaService _aulaService;
        private readonly RelatorioService _relatorioService;
        private List<ResumoAulaRelatorio> _aulas;

        public List<ResumoAulaRelatorio> Aulas
        {
            get { return _aulas; }
            set
            {
                _aulas = value;
                OnPropertyChanged();
            }
        }

        public ICommand GerarHtmlCommand { get; private set; }

        public RelatoriosAulasViewModel()
        {
            _aulaService = new AulaService();
            _relatorioService = new RelatorioService();
            GerarHtmlCommand = new RelayCommand(GerarHtml);
            CarregarAulas();
        }

        private void CarregarAulas()
        {
            try
            {
                Aulas = _aulaService.ObterResumoAulas();
            }
            catch (Exception ex)
            {
                Aulas = new List<ResumoAulaRelatorio>();
                MessageBox.Show("Erro ao carregar aulas: " + ex.Message);
            }
        }

        private void GerarHtml(object parametro)
        {
            try
            {
                var aula = parametro as ResumoAulaRelatorio;
                if (aula == null)
                {
                    MessageBox.Show("Selecione uma aula para gerar o relatorio.");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "Salvar relatorio HTML",
                    Filter = "Arquivos HTML (*.html)|*.html|Todos os arquivos (*.*)|*.*",
                    FileName = SanitizarNomeArquivo("Relatorio_Aula_" + aula.AulaId + "_" + aula.Turma + ".html")
                };

                if (dialog.ShowDialog() != true) return;

                List<RelatorioAusente> ausentes = _relatorioService.ObterDadosRelatorio(aula.AulaId);
                string html = MontarHtml(aula, ausentes);

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

        private string MontarHtml(ResumoAulaRelatorio aula, List<RelatorioAusente> ausentes)
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
            html.AppendLine("        <p><strong>Data da aula:</strong> " + WebUtility.HtmlEncode(aula.Data.ToString("dd/MM/yyyy")) + "</p>");
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
