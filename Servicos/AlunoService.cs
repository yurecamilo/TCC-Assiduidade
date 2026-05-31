using System.Collections.Generic;
using TCC_Assiduidade.Modelos;
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

        public List<Aluno> ObterAlunos()
        {
            return _alunoRepository.ObterTodos();
        }

        public List<Aluno> ObterPorTurma(int turmaId)
        {
            if (turmaId <= 0) return null;

            return _alunoRepository.ObterPorTurma(turmaId);
        }

        public Aluno ObterAlunoPorMatricula(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula)) return null;
            if (matricula.Length != 8) return null;

            return _alunoRepository.ObterPorMatricula(matricula);
        }

        public List<AlunoExibicaoDTO> ObterPerfilAluno()
        {
            return _alunoRepository.ObterPerfilAluno();
        }
    }
}
