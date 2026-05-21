using System.Collections.Generic;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public class RelatorioService
    {
        private readonly AusenciaRepository _ausenciaRepository;
        public RelatorioService()
        {
            _ausenciaRepository = new AusenciaRepository();
        }

        public List<RelatorioAusente> ObterDadosRelatorio(int aulaId)
        {
            if (aulaId <= 0) return new List<RelatorioAusente>();
            return _ausenciaRepository.ObterAusentesPorAula(aulaId);
        }
    }
}
