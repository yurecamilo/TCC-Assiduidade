using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCC_Assiduidade.Modelos;
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
            if (dataAula == null || dataAula > DateTime.Now) return -1;
            if (turmaId <= 0) return -1;
            return _aulaRepository.Adicionar(dataAula, turmaId);
        }

        public List<AulaExibicaoDTO> ObterResumoAulas()
        {
            return _aulaRepository.ObterResumoAulas();
        }
    }
}
