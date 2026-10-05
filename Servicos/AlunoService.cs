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
            if (aluno == null)
                throw new ArgumentNullException(nameof(aluno), "Aluno inválido.");

            ValidarAluno(aluno);
            new TurmaService().GarantirTurmaAtiva(aluno.TurmaId);

            _alunoRepository.Adicionar(aluno);
        }

        public void Adicionar(List<Aluno> alunos)
        {
            if (alunos == null || alunos.Count == 0) return;
            foreach (Aluno a in alunos)
            {
                ValidarAluno(a);
            }

            foreach (int turmaId in new HashSet<int>(alunos.ConvertAll(a => a.TurmaId)))
                new TurmaService().GarantirTurmaAtiva(turmaId);

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
            var alunos = _alunoRepository.ObterPerfilAluno();
            foreach (var aluno in alunos)
            {
                var frequencia = aluno.DadosFrequencia;
                frequencia.Assiduidade = CalcularAssiduidade(frequencia.TotalAulas, frequencia.TotalFaltas);
            }
            return alunos;
        }

        internal static double? CalcularAssiduidade(int totalAulas, int totalFaltas)
        {
            if (totalAulas <= 0) return null;
            return Math.Round((double)(totalAulas - totalFaltas) / totalAulas * 100, 1);
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
                throw new ArgumentException("Informe um endereço de e-mail válido.");

            if (aluno.TurmaId <= 0)
                throw new ArgumentException("Selecione uma turma válida.");
        }

        public void Atualizar(Aluno aluno)
        {
            if (aluno == null)
                throw new ArgumentNullException(nameof(aluno), "Aluno inválido.");

            ValidarAluno(aluno);
            new TurmaService().GarantirTurmaAtiva(aluno.TurmaId);
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
