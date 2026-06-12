using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Repositories;
using TCC_Assiduidade.ViewModel.Relatorios;

namespace TCC_Assiduidade.Servicos
{
    public class RelatorioService
    {
        private readonly AusenciaRepository _ausenciaRepository;
        public RelatorioService()
        {
            _ausenciaRepository = new AusenciaRepository();
        }

        public List<RelatorioAusente> ObterDadosRelatorio(int aulaId)
        {
            if (aulaId <= 0) return new List<RelatorioAusente>();
            return _ausenciaRepository.ObterAusentesPorAula(aulaId);
        }

        public string RelatorioPorAluno(List<AlunoRelatorioItem> selecionados)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine($"    <title>Relatório Por Aluno</title>");
            html.AppendLine("<style>");
            html.AppendLine("    body { font-family: Arial, sans-serif; margin: 32px; color: #1f2933; }");
            html.AppendLine("    h1 { margin-bottom: 8px; color: #236B73; }");
            html.AppendLine("    .card-perf { background: #ECF8F6; border-radius: 8px; padding: 20px; border: 1px solid #B0C8D6; margin-top: 15px; }");
            html.AppendLine("    p { font-size: 15px; line-height: 1.6; margin: 6px 0; }");
            html.AppendLine("    .destaque { font-size: 20px; font-weight: bold; color: #236B73; }");
            html.AppendLine("    .falta { color: #AA3333; font-weight: bold; }");
            html.AppendLine("</style></head><body>");
            html.AppendLine("<h1>Ficha de Rendimento e Assiduidade</h1>");

            foreach (var aluno in selecionados)
            {
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
            }
            html.AppendLine("</body></html>");
            return html.ToString();
        }
        public string RelatorioPorAula(AulaExibicaoDTO aula, List<RelatorioAusente> ausentes)
        {
            var html = new StringBuilder();

            html.AppendLine(RelatorioBase("Relatório de Ausentes"));
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

            html.AppendLine("</body></html>");
            return html.ToString();
        }
        public string RelatorioPorTurma(List<TurmaRelatorioItem> selecionadas)
        {
            var html = new StringBuilder();

            html.AppendLine(RelatorioBase("Relatório Geral Por Turma"));
            html.AppendLine($"    <h1>Relatório de Histórico por Turma</h1>");

            foreach (var turma in selecionadas)
            {
                List<AulaExibicaoDTO> aulasDaTurma = DataCacheService.AulasCache?
                        .Where(a => a.Turma != null && a.Turma.Equals(turma.Nome, StringComparison.OrdinalIgnoreCase))
                        .ToList() ?? new List<AulaExibicaoDTO>();

                html.AppendLine("    <div class=\"quadro-turma\" style=\"border: 1px solid #ccc; border-radius: 8px; padding: 15px; margin-bottom: 25px; background-color: #fff; box-shadow: 0 2px 4px rgba(0,0,0,0.05);\">");

                html.AppendLine("        <div class=\"info\">");
                html.AppendLine($"            <p><strong>Turma analisada:</strong> {WebUtility.HtmlEncode(turma.Nome)}</p>");
                html.AppendLine($"            <p><strong>Total de diários de classe mapeados:</strong> {aulasDaTurma.Count}</p>");
                html.AppendLine("        </div>");

                if (aulasDaTurma.Count == 0)
                {
                    html.AppendLine("        <p style=\"color: #666; font-style: italic;\">Não há histórico de chamadas salvas para esta turma no sistema.</p>");
                }
                else
                {
                    html.AppendLine("        <table style=\"width: 100%; border-collapse: collapse; margin-top: 10px;\">");
                    html.AppendLine("            <thead>");
                    html.AppendLine("                <tr style=\"background-color: #f5f5f5; border-bottom: 2px solid #ddd;\">");
                    html.AppendLine("                    <th style=\"text-align: left; padding: 8px;\">Data da Aula</th>");
                    html.AppendLine("                    <th style=\"text-align: left; padding: 8px;\">Número Absoluto de Ausências</th>");
                    html.AppendLine("                </tr>");
                    html.AppendLine("            </thead>");
                    html.AppendLine("            <tbody>");

                    foreach (var aula in aulasDaTurma)
                    {
                        html.AppendLine("                <tr style=\"border-bottom: 1px solid #eee;\">");
                        html.AppendLine($"                    <td style=\"padding: 8px;\">{aula.Data:dd/MM/yyyy}</td>");
                        html.AppendLine($"                    <td style=\"padding: 8px;\">{aula.NumeroAusentes} alunos faltantes</td>");
                        html.AppendLine("                </tr>");
                    }
                    html.AppendLine("            </tbody>");
                    html.AppendLine("        </table>");
                }

                html.AppendLine("    </div>"); 

                html.AppendLine("    <hr style=\"border: 0; height: 1px; background: #ddd; margin: 30px 0;\" />");
            }

            html.AppendLine("</body></html>");
            return html.ToString();
        }

        public string RelatorioBase(string Titulo)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"pt-BR\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\">");
            html.AppendLine($"    <title>{Titulo}</title>");
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

            return html.ToString();
        }
    }
}
