using System.Collections.Generic;
using System;
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
            if (aulaId <= 0)
                throw new ArgumentException("O ID da aula deve ser maior que zero.", nameof(aulaId));
            if (alunosMatricula == null)
                throw new ArgumentNullException(nameof(alunosMatricula));
            if (alunosMatricula.Count == 0)
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
