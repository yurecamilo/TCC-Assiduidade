using System;
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
            ValidarAluno(aluno);

            _alunoRepository.Adicionar(aluno);
        }

        public void Adicionar(List<Aluno> alunos)
        {
            if (alunos == null || alunos.Count == 0) return;
            foreach (Aluno a in alunos)
            {
                ValidarAluno(a);
            }

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



        public bool ValidarEmailInstitucional(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var enderecoEmail = new System.Net.Mail.MailAddress(email.Trim());

                return enderecoEmail.Address.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ValidarAluno(Aluno aluno)
        {
            if (aluno == null)
                throw new ArgumentException("Aluno inválido.");

            if (string.IsNullOrWhiteSpace(aluno.Matricula))
                throw new ArgumentException("Informe a matrícula do aluno.");

            if (string.IsNullOrWhiteSpace(aluno.Nome))
                throw new ArgumentException("Informe o nome do aluno.");

            if (!ValidarEmailInstitucional(aluno.Email))
                throw new ArgumentException("Informe um e-mail institucional válido (@univap.br).");

            if (aluno.TurmaId <= 0)
                throw new ArgumentException("Selecione uma turma válida.");
        }

        public void Atualizar(Aluno aluno)
        {
            if (aluno == null) return;
            ValidarAluno(aluno);
            _alunoRepository.Atualizar(aluno);
        }

        public void Excluir(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                throw new ArgumentException("Informe a matrícula do aluno para exclusão.");
            _alunoRepository.Excluir(matricula);
        }
    }
}
