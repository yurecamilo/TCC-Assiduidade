using System;

namespace TCC_Assiduidade.Modelos.Relatorios
{
    public class RelatorioAusente
    {
        public int AulaId { get; set; }
        public DateTime Data { get; set; }
        public string Turma { get; set; }
        public string Matricula { get; set; }
        public string Aluno { get; set; }
        public string Email { get; set; }
    }
}
