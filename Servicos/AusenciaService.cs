using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public class AusenciaService
    {
        private readonly AusenciaRepository _ausenciaRepository;

        public AusenciaService()
        {
            _ausenciaRepository = new AusenciaRepository();
        }

        public void Adicionar(int aulaId, List<string> alunosMatricula)
        {
            if (aulaId <= 0 || alunosMatricula == null || alunosMatricula.Count == 0)
                return;
            _ausenciaRepository.Adicionar(aulaId, alunosMatricula);
        }

        public List<RelatorioAusente> ObterAusentesPorAula(int aulaId)
        {
            if (aulaId <= 0) return new List<RelatorioAusente>();
            return _ausenciaRepository.ObterAusentesPorAula(aulaId);
        }
    }
}
