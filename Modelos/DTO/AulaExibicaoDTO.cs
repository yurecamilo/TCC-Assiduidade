using System;
using TCC_Assiduidade.Modelos.Banco;

namespace TCC_Assiduidade.Modelos.DTO
{
    public class AulaExibicaoDTO
    {
        public bool IsSelected { get; set; }
        public int AulaId { get; set; }
        public int TurmaId { get; set; }
        public EStatusTurma StatusTurma { get; set; }
        public DateTime Data { get; set; }
        public string Turma { get; set; }
        public int NumeroAusentes { get; set; }
    }
}
