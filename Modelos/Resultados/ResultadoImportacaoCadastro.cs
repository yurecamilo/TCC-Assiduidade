using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Banco;

namespace TCC_Assiduidade.Modelos.Resultados
{
    public class ResultadoImportacaoCadastro
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public List<Aluno> Alunos { get; set; } = new List<Aluno>();
    }
}
