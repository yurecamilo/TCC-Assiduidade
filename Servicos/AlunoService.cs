using System.Collections.Generic;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Servicos
{
    public class AlunoService
    {
        private readonly AlunoRepository _alunoRepository;

        public AlunoService()
        {
            _alunoRepository = new AlunoRepository();
        }

        public void Adicionar(Aluno aluno)
        {
            if (aluno == null) return;
            _alunoRepository.Adicionar(aluno);
        }

        public void Adicionar(List<Aluno> alunos)
        {
            if (alunos == null || alunos.Count == 0) return;

            _alunoRepository.Adicionar(alunos);
        }

        public List<Aluno> ObterPorTurma(int turmaId)
        {
            if (turmaId <= 0) return null;

            return _alunoRepository.ObterPorTurma(turmaId);
        }

        public int ObterQuantidadeMatriculasExistentes(List<string> matriculas)
        {
            return _alunoRepository.ContarMatriculasExistentes(matriculas);
        }

        public int ContarAlunosDeOutraTurma(int turmaId, List<string> matriculas)
        {
            if (turmaId <= 0 || matriculas == null || matriculas.Count == 0)
            {
                return 0;
            }

            return _alunoRepository.ContarAlunosDeOutraTurma(turmaId, matriculas);
        }

        public List<AlunoExibicaoDTO> ObterPerfilAluno()
        {
            return _alunoRepository.ObterPerfilAluno();
        }
    }
}
