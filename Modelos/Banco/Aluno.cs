using System;

namespace TCC_Assiduidade.Modelos
{
    public class Aluno
    {
        public string Matricula { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }

        public int TurmaId { get; set; }
        public DateTime? DataEntrada { get; set; }
    }
}
