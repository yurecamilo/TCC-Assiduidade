using System;

namespace TCC_Assiduidade.Modelos
{
    public class ResumoAulaRelatorio
    {
        public int AulaId { get; set; }
        public DateTime Data { get; set; }
        public string Turma { get; set; }
        public int NumeroAusentes { get; set; }
    }
}
