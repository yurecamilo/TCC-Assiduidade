using System.Collections.Generic;

namespace TCC_Assiduidade.Modelos.Resultados
{
    public class ResultadoImportacaoPresenca
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }
        public List<RelatorioAusente> Ausentes { get; set; } = new List<RelatorioAusente>();
    }
}
