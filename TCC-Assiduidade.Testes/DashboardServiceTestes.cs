using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TCC_Assiduidade.Modelos.Dashboard;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class DashboardServiceTestes
    {
        [TestMethod]
        public void ObterDashboardAluno_ComTransferencia_RespeitaTurmaSelecionada()
        {
            var registros = new List<RegistroFrequenciaDashboard>
            {
                Registro(1, new DateTime(2026, 8, 10), 1, "Turma A", "001", "Ana", true),
                Registro(2, new DateTime(2026, 8, 17), 1, "Turma A", "001", "Ana", false),
                Registro(1, new DateTime(2026, 8, 10), 1, "Turma A", "002", "Bruno", true),
                Registro(2, new DateTime(2026, 8, 17), 1, "Turma A", "002", "Bruno", true),
                Registro(3, new DateTime(2026, 9, 2), 2, "Turma B", "001", "Ana", true)
            };
            var service = new DashboardService(registros);

            ResultadoDashboardAluno resultado = service.ObterDashboardAluno(
                "001", 1, null, null);

            Assert.IsTrue(resultado.TemDados);
            Assert.AreEqual("Turma A", resultado.Turma);
            Assert.AreEqual(2, resultado.TotalAulas);
            Assert.AreEqual(1, resultado.Presentes);
            Assert.AreEqual(50d, resultado.Assiduidade);
            Assert.AreEqual(75d, resultado.MediaTurma);
            Assert.AreEqual(2, resultado.PosicaoTurma);
        }

        [TestMethod]
        public void ObterAlunos_ComMesmaMatriculaEmDuasTurmas_DistingueAsOpcoes()
        {
            var service = new DashboardService(new[]
            {
                Registro(1, new DateTime(2026, 8, 10), 1, "Turma A", "001", "Ana", true),
                Registro(2, new DateTime(2026, 9, 10), 2, "Turma B", "001", "Ana", true)
            });

            List<OpcaoAlunoDashboard> alunos = service.ObterAlunos(0);

            Assert.AreEqual(2, alunos.Count);
            Assert.AreNotEqual(alunos[0].Descricao, alunos[1].Descricao);
            StringAssert.Contains(alunos[0].Descricao + alunos[1].Descricao, "Turma A");
            StringAssert.Contains(alunos[0].Descricao + alunos[1].Descricao, "Turma B");
        }

        [TestMethod]
        public void ObterDashboardTurmas_CalculaRankingERisco()
        {
            var service = new DashboardService(new[]
            {
                Registro(1, new DateTime(2026, 8, 1), 1, "Turma A", "001", "Ana", true),
                Registro(2, new DateTime(2026, 8, 2), 1, "Turma A", "001", "Ana", false),
                Registro(3, new DateTime(2026, 8, 1), 2, "Turma B", "002", "Bruno", true),
                Registro(4, new DateTime(2026, 8, 2), 2, "Turma B", "002", "Bruno", true)
            });

            ResultadoDashboardTurmas resultado = service.ObterDashboardTurmas(0, null, null);

            Assert.AreEqual(75d, resultado.AssiduidadeGeral);
            Assert.AreEqual(2, resultado.TotalTurmas);
            Assert.AreEqual(2, resultado.TotalAlunos);
            Assert.AreEqual(1, resultado.AlunosEmRisco);
            Assert.AreEqual("Turma B", resultado.Ranking.First().Rotulo);
            Assert.AreEqual(100d, resultado.Ranking.First().Valor);
        }

        [TestMethod]
        public void ObterDashboardAulas_ComQuatorzeAulas_ExibeAsDozeMaisRecentes()
        {
            var registros = Enumerable.Range(1, 14)
                .Select(dia => Registro(
                    dia,
                    new DateTime(2026, 1, dia),
                    1,
                    "Turma A",
                    "001",
                    "Ana",
                    true))
                .ToList();
            var service = new DashboardService(registros);

            ResultadoDashboardAulas resultado = service.ObterDashboardAulas(1, null, null);

            Assert.AreEqual(14, resultado.TotalAulas);
            Assert.AreEqual(12, resultado.Aulas.Count);
            Assert.AreEqual("03/01", resultado.Aulas.First().Rotulo);
            Assert.AreEqual("14/01", resultado.Aulas.Last().Rotulo);
        }

        [TestMethod]
        public void FiltroDePeriodo_IncluiAsDatasLimite()
        {
            var service = new DashboardService(new[]
            {
                Registro(1, new DateTime(2026, 1, 1), 1, "Turma A", "001", "Ana", true),
                Registro(2, new DateTime(2026, 1, 2), 1, "Turma A", "001", "Ana", false),
                Registro(3, new DateTime(2026, 1, 3), 1, "Turma A", "001", "Ana", true)
            });

            ResultadoDashboardAluno resultado = service.ObterDashboardAluno(
                "001",
                1,
                new DateTime(2026, 1, 2),
                new DateTime(2026, 1, 3));

            Assert.AreEqual(2, resultado.TotalAulas);
            Assert.AreEqual(1, resultado.Presentes);
        }

        private static RegistroFrequenciaDashboard Registro(
            int aulaId,
            DateTime data,
            int turmaId,
            string turma,
            string matricula,
            string aluno,
            bool presente)
        {
            return new RegistroFrequenciaDashboard
            {
                AulaId = aulaId,
                Data = data,
                TurmaId = turmaId,
                Turma = turma,
                Matricula = matricula,
                Aluno = aluno,
                Presente = presente
            };
        }
    }
}
