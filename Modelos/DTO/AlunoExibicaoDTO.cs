using System;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.Modelos.DTO
{
    public class AlunoExibicaoDTO : BaseViewModel
    {
        public RelatorioFrequencia DadosFrequencia { get; set; }

        public string Matricula { get; set; }
        public string Nome { get; set; }
        public string Turma { get; set; }
        public string Email { get; set; }
        public DateTime? DataEntrada { get; set; }
    }
}