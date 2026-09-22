using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class DashboardHtmlServiceTestes
    {
        [TestMethod]
        public void GerarHtmlAluno_SemDados_LancaInvalidOperationException()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
                DashboardHtmlService.GerarHtmlAluno(
                    new ResultadoDashboardAluno(),
                    null,
                    null));
        }

        [TestMethod]
        public void GerarHtmlAluno_CodificaConteudoDinamico()
        {
            var resultado = new ResultadoDashboardAluno
            {
                TemDados = true,
                Aluno = "Ana <Silva>",
                Matricula = "A&1",
                Turma = "Turma > A",
                Presentes = 1,
                Ausentes = 1,
                TotalAulas = 2,
                Assiduidade = 50,
                MediaTurma = 75,
                Situacao = "Em risco",
                Evolucao = new List<PontoGraficoDashboard>
                {
                    new PontoGraficoDashboard
                    {
                        Rotulo = "<jan>",
                        Valor = 50,
                        ValorSecundario = 75
                    }
                }
            };

            string html = DashboardHtmlService.GerarHtmlAluno(resultado, null, null);

            StringAssert.Contains(html, "Ana &lt;Silva&gt;");
            StringAssert.Contains(html, "A&amp;1");
            StringAssert.Contains(html, "&lt;jan&gt;");
            Assert.IsFalse(html.Contains("Ana <Silva>"));
        }
    }
}
