using System;
using TCC_Assiduidade.Modelos.Banco;

namespace TCC_Assiduidade.Modelos.Dashboard
{
    public class RegistroFrequenciaDashboard
    {
        public int AulaId { get; set; }
        public DateTime Data { get; set; }
        public int TurmaId { get; set; }
        public EStatusTurma StatusTurma { get; set; }
        public string Turma { get; set; }
        public string Matricula { get; set; }
        public string Aluno { get; set; }
        public bool Presente { get; set; }
    }
}
