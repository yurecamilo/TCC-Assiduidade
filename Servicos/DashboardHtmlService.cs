using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using TCC_Assiduidade.Modelos.Dashboard;

namespace TCC_Assiduidade.Servicos
{
    public static class DashboardHtmlService
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");

        public static string GerarHtmlAluno(
            ResultadoDashboardAluno resultado,
            DateTime? dataInicio,
            DateTime? dataFim)
        {
            if (resultado == null || !resultado.TemDados)
                throw new InvalidOperationException("Não há dados de aluno para exportar.");

            var conteudo = new StringBuilder("<section class='metrics'>");
            AdicionarMetrica(conteudo, "Assiduidade", FormatarPercentual(resultado.Assiduidade));
            AdicionarMetrica(conteudo, "Presenças", resultado.Presentes + "/" + resultado.TotalAulas);
            AdicionarMetrica(conteudo, "Média da turma", FormatarPercentual(resultado.MediaTurma));
            AdicionarMetrica(conteudo, "Situação", resultado.Situacao);
            conteudo.Append("</section>");

            conteudo.Append("<section class='grid'>");
            conteudo.Append("<article class='chart'><h2>Frequência no período</h2>");
            conteudo.Append(GerarRosca(resultado.Presentes, resultado.Ausentes));
            conteudo.Append("<div class='legend'><span><i class='green'></i>Presenças: " + resultado.Presentes + "</span>");
            conteudo.Append("<span><i class='soft'></i>Ausências: " + resultado.Ausentes + "</span></div>");
            conteudo.Append("</article>");
            conteudo.Append("<article class='chart'><h2>Evolução de assiduidade</h2>");
            conteudo.Append("<p class='caption'>Aluno comparado à média da turma</p>");
            conteudo.Append(GerarGraficoLinha(resultado.Evolucao, true));
            conteudo.Append("</article></section>");

            string subtitulo = resultado.Aluno + " • Matrícula " +
                               resultado.Matricula + " • " + resultado.Turma;
            return GerarDocumentoHtml(
                "Dashboard de Aluno",
                subtitulo,
                dataInicio,
                dataFim,
                conteudo.ToString());
        }

        public static string GerarHtmlTurmas(
            ResultadoDashboardTurmas resultado,
            DateTime? dataInicio,
            DateTime? dataFim)
        {
            if (resultado == null || !resultado.TemDados)
                throw new InvalidOperationException("Não há dados de turmas para exportar.");

            var conteudo = new StringBuilder("<section class='metrics'>");
            AdicionarMetrica(conteudo, "Assiduidade geral", FormatarPercentual(resultado.AssiduidadeGeral));
            AdicionarMetrica(conteudo, "Turmas analisadas", resultado.TotalTurmas.ToString());
            AdicionarMetrica(conteudo, "Alunos analisados", resultado.TotalAlunos.ToString());
            AdicionarMetrica(conteudo, "Alunos abaixo de 75%", resultado.AlunosEmRisco.ToString());
            conteudo.Append("</section>");
            conteudo.Append("<section class='grid'>");
            conteudo.Append("<article class='chart'><h2>Frequência geral</h2>");
            conteudo.Append(GerarRosca(resultado.Presentes, resultado.Ausentes));
            conteudo.Append("<div class='legend'><span><i class='green'></i>Presenças: " + resultado.Presentes + "</span>");
            conteudo.Append("<span><i class='soft'></i>Ausências: " + resultado.Ausentes + "</span></div>");
            conteudo.Append("</article>");
            conteudo.Append("<article class='chart'><h2>Assiduidade por turma</h2>");
            conteudo.Append(GerarBarras(resultado.Ranking));
            conteudo.Append("</article>");
            conteudo.Append("<article class='chart wide'><h2>Evolução geral</h2>");
            conteudo.Append(GerarGraficoLinha(resultado.Evolucao, false));
            conteudo.Append("</article></section>");

            return GerarDocumentoHtml(
                "Dashboard de Turmas",
                "Comparativo de assiduidade",
                dataInicio,
                dataFim,
                conteudo.ToString());
        }

        public static string GerarHtmlAulas(
            ResultadoDashboardAulas resultado,
            DateTime? dataInicio,
            DateTime? dataFim)
        {
            if (resultado == null || !resultado.TemDados)
                throw new InvalidOperationException("Não há dados de aulas para exportar.");

            var conteudo = new StringBuilder("<section class='metrics'>");
            AdicionarMetrica(conteudo, "Presença média", FormatarPercentual(resultado.PresencaMedia));
            AdicionarMetrica(conteudo, "Aulas analisadas", resultado.TotalAulas.ToString());
            AdicionarMetrica(conteudo, "Ausências registradas", resultado.TotalAusencias.ToString());
            AdicionarMetrica(conteudo, "Aula que exige atenção", resultado.AulaAtencao);
            conteudo.Append("</section>");
            conteudo.Append("<section class='grid'>");
            conteudo.Append("<article class='chart'><h2>Frequência das aulas</h2>");
            conteudo.Append(GerarRosca(resultado.Presentes, resultado.Ausentes));
            conteudo.Append("<div class='legend'><span><i class='green'></i>Presenças: " + resultado.Presentes + "</span>");
            conteudo.Append("<span><i class='soft'></i>Ausências: " + resultado.Ausentes + "</span></div>");
            conteudo.Append("</article>");
            conteudo.Append("<article class='chart'><h2>Assiduidade por aula</h2>");
            conteudo.Append(GerarBarras(resultado.Aulas));
            conteudo.Append("</article>");
            conteudo.Append("<article class='chart wide'><h2>Evolução mensal</h2>");
            conteudo.Append(GerarGraficoLinha(resultado.Evolucao, false));
            conteudo.Append("<p class='insight'><strong>Melhor aula:</strong> " + CodificarHtml(resultado.MelhorAula) + "</p>");
            conteudo.Append("</article></section>");

            return GerarDocumentoHtml(
                "Dashboard de Aulas",
                "Aderência por encontro",
                dataInicio,
                dataFim,
                conteudo.ToString());
        }

        private static string GerarDocumentoHtml(
            string titulo,
            string subtitulo,
            DateTime? dataInicio,
            DateTime? dataFim,
            string conteudo)
        {
            var html = new StringBuilder();
            html.Append(RelatorioService.RelatorioBase(titulo, ObterEstilosDashboard()));
            html.Append("<h1>" + CodificarHtml(titulo) + "</h1>");
            html.Append("<div class='info'><p>" + CodificarHtml(subtitulo) + "</p>");
            html.Append("<p><strong>" + CodificarHtml(FormatarPeriodo(dataInicio, dataFim)) + "</strong><br>");
            html.Append("Gerado em " + DateTime.Now.ToString("dd/MM/yyyy 'às' HH:mm") + "</p></div>");
            html.Append(conteudo);
            html.Append("</body></html>");
            return html.ToString();
        }

        private static string ObterEstilosDashboard()
        {
            return
                ".metrics{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:20px 0}" +
                ".metric{background:#ECF8F6;border:1px solid #B0C8D6;padding:16px;border-radius:8px}" +
                ".metric span{display:block;color:#617277;font-size:12px}" +
                ".metric strong{display:block;margin-top:6px;color:#236B73;font-size:20px;word-break:break-word}" +
                ".grid{display:grid;grid-template-columns:1.35fr 1fr;gap:16px}" +
                ".chart{border:1px solid #B0C8D6;padding:18px;border-radius:8px;background:#fff}" +
                ".chart h2{margin:0 0 4px;color:#236B73;font-size:18px}.wide{grid-column:1/-1}" +
                ".caption{color:#617277;font-size:13px}.bars{display:grid;gap:11px;margin-top:18px}" +
                ".bar-row{display:grid;grid-template-columns:130px 1fr 48px;gap:10px;align-items:center;font-size:12px}" +
                ".track{height:12px;background:#dce9e7;border-radius:8px;overflow:hidden}.fill{height:100%;background:#236B73}" +
                ".legend{display:flex;justify-content:center;gap:22px;font-size:13px}" +
                ".legend span{display:flex;align-items:center;gap:7px}.legend i{width:10px;height:10px;border-radius:2px}" +
                ".green{background:#2f8f73}.soft{background:#d7e7e4}" +
                ".donut{width:180px;height:180px;border-radius:50%;margin:18px auto;display:grid;place-items:center}" +
                ".donut:after{content:'';width:116px;height:116px;border-radius:50%;background:#fff}" +
                ".donut-value{position:absolute;text-align:center;color:#236B73;font-size:26px;font-weight:bold}" +
                ".insight{margin-top:16px;padding:12px;background:#ECF8F6;border-radius:8px;font-size:13px}" +
                "svg{width:100%;height:auto;margin-top:12px}.grid-line{stroke:#d7e4e1;stroke-width:1}" +
                ".axis{stroke:#9bb4b0;stroke-width:1}.primary{fill:none;stroke:#3e78b2;stroke-width:3}" +
                ".secondary{fill:none;stroke:#2f8f73;stroke-width:3}svg text{font:11px Arial,sans-serif;fill:#526469}" +
                "@media(max-width:760px){.metrics,.grid{grid-template-columns:1fr 1fr}}" +
                "@media print{.chart{break-inside:avoid}}";
        }

        private static void AdicionarMetrica(StringBuilder html, string nome, string valor)
        {
            html.Append("<div class='metric'><span>" + CodificarHtml(nome) +
                        "</span><strong>" + CodificarHtml(valor) + "</strong></div>");
        }

        private static string GerarBarras(List<PontoGraficoDashboard> pontos)
        {
            var html = new StringBuilder("<div class='bars'>");
            int quantidade = pontos == null ? 0 : Math.Min(12, pontos.Count);

            for (int i = 0; i < quantidade; i++)
            {
                PontoGraficoDashboard ponto = pontos[i];
                double valor = Math.Max(0, Math.Min(100, ponto.Valor));
                html.Append("<div class='bar-row'><span>" + CodificarHtml(ponto.Rotulo) +
                            "</span><div class='track'><div class='fill' style='width:" +
                            FormatarNumeroCss(valor) + "%'></div></div><strong>" +
                            FormatarPercentual(valor) + "</strong></div>");
            }
            html.Append("</div>");
            return html.ToString();
        }

        private static string GerarGraficoLinha(
            List<PontoGraficoDashboard> pontos,
            bool mostrarSerieSecundaria)
        {
            if (pontos == null || pontos.Count == 0)
                return "<p class='muted'>Sem dados para o período.</p>";

            const double largura = 560;
            const double esquerda = 48;
            const double direita = 18;
            const double topo = 24;
            const double baseY = 205;
            double passo = pontos.Count == 1 ? 0 : (largura - esquerda - direita) / (pontos.Count - 1);

            var pontosPrincipais = new StringBuilder();
            var pontosSecundarios = new StringBuilder();
            bool possuiSerieSecundaria = true;

            for (int i = 0; i < pontos.Count; i++)
            {
                if (i > 0)
                {
                    pontosPrincipais.Append(" ");
                    pontosSecundarios.Append(" ");
                }

                double x = esquerda + i * passo;
                pontosPrincipais.Append(FormatarNumeroCss(x) + "," +
                                        FormatarNumeroCss(CalcularY(pontos[i].Valor, topo, baseY)));

                if (pontos[i].ValorSecundario.HasValue)
                {
                    pontosSecundarios.Append(FormatarNumeroCss(x) + "," +
                                             FormatarNumeroCss(CalcularY(pontos[i].ValorSecundario.Value, topo, baseY)));
                }
                else
                {
                    possuiSerieSecundaria = false;
                }
            }

            var svg = new StringBuilder("<svg viewBox='0 0 560 250' role='img' aria-label='Gráfico de evolução percentual'>");
            foreach (int valor in new[] { 100, 75, 50, 25, 0 })
            {
                double linhaY = CalcularY(valor, topo, baseY);
                svg.Append("<line class='grid-line' x1='48' y1='" + FormatarNumeroCss(linhaY) + "' x2='542' y2='" + FormatarNumeroCss(linhaY) + "'/>");
                svg.Append("<text x='10' y='" + FormatarNumeroCss(linhaY + 4) + "'>" + valor + "%</text>");
            }
            svg.Append("<line class='axis' x1='48' y1='205' x2='542' y2='205'/>");
            svg.Append("<polyline class='primary' points='" + pontosPrincipais + "'/>");
            if (mostrarSerieSecundaria && possuiSerieSecundaria)
                svg.Append("<polyline class='secondary' points='" + pontosSecundarios + "'/>");

            for (int i = 0; i < pontos.Count; i++)
            {
                double x = esquerda + i * passo;
                svg.Append("<circle cx='" + FormatarNumeroCss(x) + "' cy='" +
                           FormatarNumeroCss(CalcularY(pontos[i].Valor, topo, baseY)) +
                           "' r='4' fill='#3e78b2'/>");
                svg.Append("<text text-anchor='middle' x='" + FormatarNumeroCss(x) + "' y='228'>" + CodificarHtml(pontos[i].Rotulo) + "</text>");
            }
            svg.Append("</svg>");
            return svg.ToString();
        }

        private static string GerarRosca(int presentes, int ausentes)
        {
            int total = presentes + ausentes;
            double percentual = total == 0 ? 0 : presentes * 100.0 / total;
            return "<div style='position:relative;display:grid;place-items:center'>" +
                   "<div class='donut' style='background:conic-gradient(#2f8f73 0 " + FormatarNumeroCss(percentual) +
                   "%,#d7e7e4 " + FormatarNumeroCss(percentual) + "% 100%)'></div>" +
                   "<div class='donut-value'>" + FormatarPercentual(percentual) + "</div></div>";
        }

        private static double CalcularY(double valor, double topo, double baseY)
        {
            double percentual = Math.Max(0, Math.Min(100, valor));
            return baseY - percentual * (baseY - topo) / 100.0;
        }

        private static string FormatarPercentual(double valor)
        {
            return valor.ToString("0.#", Cultura) + "%";
        }

        private static string FormatarPeriodo(DateTime? dataInicio, DateTime? dataFim)
        {
            if (dataInicio.HasValue && dataFim.HasValue)
                return "Período: " + dataInicio.Value.ToString("dd/MM/yyyy") + " a " + dataFim.Value.ToString("dd/MM/yyyy");
            if (dataInicio.HasValue) return "A partir de " + dataInicio.Value.ToString("dd/MM/yyyy");
            if (dataFim.HasValue) return "Até " + dataFim.Value.ToString("dd/MM/yyyy");
            return "Todo o período disponível";
        }

        private static string CodificarHtml(string valor)
        {
            return WebUtility.HtmlEncode(valor ?? string.Empty);
        }

        private static string FormatarNumeroCss(double valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
