using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.Modelos
{
    public class AlunoExibicaoDTO : BaseViewModel
    {
        public RelatorioFrequencia DadosFrequencia { get; set; }

        public string Matricula { get; set; }
        public string Nome { get; set; }
        public string Turma { get; set; }
        public string Email { get; set; }
    }
}