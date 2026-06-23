using System;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public class AulaService
    {
        private readonly AulaRepository _aulaRepository;

        public AulaService()
        {
            _aulaRepository = new AulaRepository();
        }

        public int Adicionar(DateTime dataAula, int turmaId)
        {
            if (dataAula > DateTime.Now) throw new ArgumentException("A data da aula não pode ser uma data futura.", nameof(dataAula));
            if (turmaId <= 0) throw new ArgumentException("O ID da turma é inválido.", nameof(turmaId));
            return _aulaRepository.Adicionar(dataAula, turmaId);
        }

        public List<AulaExibicaoDTO> ObterResumoAulas()
        {
            return _aulaRepository.ObterResumoAulas();
        }
    }
}
