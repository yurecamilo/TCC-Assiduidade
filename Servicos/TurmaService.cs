using System.Collections.Generic;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public class TurmaService
    {
        private readonly TurmaRepository _turmaRepository;

        public TurmaService()
        {
            _turmaRepository = new TurmaRepository();
        }

        public int Adicionar(string turmaNome)
        {
            if (string.IsNullOrWhiteSpace(turmaNome))
                return -1;

            var turmaExistente = _turmaRepository.ObterTurmaPorNome(turmaNome);
            if (turmaExistente != null)
                return -1;

            return _turmaRepository.Adicionar(turmaNome);
        }

        public Turma ObterTurmaPorNome(string turmaNome)
        {
            if (string.IsNullOrWhiteSpace(turmaNome))
                return null;

            return _turmaRepository.ObterTurmaPorNome(turmaNome);
        }

        public List<Turma> ObterTodasTurmas()
        {
            return _turmaRepository.ObterTurmas();
        }

        public Turma ObterTurmaPorId(int turmaId)
        {
            if (turmaId <= 0)
                return null;

            return _turmaRepository.ObterTurmaPorId(turmaId);
        }
    }
}
