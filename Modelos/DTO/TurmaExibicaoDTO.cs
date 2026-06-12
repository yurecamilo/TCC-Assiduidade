using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.Modelos.DTO
{
    public class TurmaExibicaoDTO: BaseViewModel
    {
        public Turma TurmaOriginal { get; set; }
        public string Nome { get; set; }
        public int QuantidadeAlunos { get; set; }
    }
}