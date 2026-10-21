using System;
using System.Collections.Generic;
using System.Linq;
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
            new TurmaService().GarantirTurmaAtiva(turmaId);
            return _aulaRepository.Adicionar(dataAula, turmaId);
        }

        public int AdicionarComAusencias(DateTime dataAula, int turmaId, List<string> alunosAusentes)
        {
            if (dataAula > DateTime.Now)
                throw new ArgumentException("A data da aula não pode ser uma data futura.", nameof(dataAula));
            if (turmaId <= 0)
                throw new ArgumentException("O ID da turma é inválido.", nameof(turmaId));
            if (alunosAusentes == null)
                throw new ArgumentNullException(nameof(alunosAusentes));
            if (alunosAusentes.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("A matrícula de um aluno ausente não pode ser vazia.", nameof(alunosAusentes));
            if (alunosAusentes.Count != alunosAusentes.Distinct().Count())
                throw new ArgumentException("A lista de ausentes contém matrículas repetidas.", nameof(alunosAusentes));

            new TurmaService().GarantirTurmaAtiva(turmaId);
            return _aulaRepository.AdicionarComAusencias(dataAula, turmaId, alunosAusentes);
        }

        public List<AulaExibicaoDTO> ObterResumoAulas()
        {
            return _aulaRepository.ObterResumoAulas();
        }

    }
}
