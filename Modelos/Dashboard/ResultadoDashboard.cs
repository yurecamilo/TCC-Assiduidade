using System.Collections.Generic;

namespace TCC_Assiduidade.Modelos.Dashboard
{
    public abstract class ResultadoDashboardBase
    {
        public bool TemDados { get; set; }
        public string Mensagem { get; set; }
        public int Presentes { get; set; }
        public int Ausentes { get; set; }
    }

    public class ResultadoDashboardAluno : ResultadoDashboardBase
    {
        public string Aluno { get; set; }
        public string Matricula { get; set; }
        public string Turma { get; set; }
        public int TotalAulas { get; set; }
        public double Assiduidade { get; set; }
        public double MediaTurma { get; set; }
        public int PosicaoTurma { get; set; }
        public int TotalAlunosTurma { get; set; }
        public string Situacao { get; set; }
        public List<PontoGraficoDashboard> Evolucao { get; set; } = new List<PontoGraficoDashboard>();
    }

    public class ResultadoDashboardTurmas : ResultadoDashboardBase
    {
        public double AssiduidadeGeral { get; set; }
        public int TotalTurmas { get; set; }
        public int TotalAlunos { get; set; }
        public int AlunosEmRisco { get; set; }
        public List<PontoGraficoDashboard> Ranking { get; set; } = new List<PontoGraficoDashboard>();
        public List<PontoGraficoDashboard> Evolucao { get; set; } = new List<PontoGraficoDashboard>();
    }

    public class ResultadoDashboardAulas : ResultadoDashboardBase
    {
        public double PresencaMedia { get; set; }
        public int TotalAulas { get; set; }
        public int TotalAusencias { get; set; }
        public string MelhorAula { get; set; }
        public string AulaAtencao { get; set; }
        public List<PontoGraficoDashboard> Aulas { get; set; } = new List<PontoGraficoDashboard>();
        public List<PontoGraficoDashboard> Evolucao { get; set; } = new List<PontoGraficoDashboard>();
    }
}
