using System;
using System.Collections.Generic;
using System.Linq;
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
            if (alunos.ConvertAll(a => a.TurmaId).Distinct().Count() != 1)
                throw new ArgumentException("O lote deve associar todos os alunos a uma única turma.");
            var turma = new TurmaService().ObterTurmaPorId(alunos[0].TurmaId);
            new TurmaService().GarantirTurmaAtiva(alunos[0].TurmaId);
            _alunoRepository.ImportarCadastroAtomico(turma.Nome, alunos, false);
        }

        public void ImportarCadastroAtomico(string turmaNome, List<Aluno> alunos, bool criarTurma)
        {
            foreach (var aluno in alunos) ValidarAluno(aluno);
            _alunoRepository.ImportarCadastroAtomico(turmaNome, alunos, criarTurma);
        }

        public int ImportarAulaEAusenciasAtomico(DateTime data, int turmaId, List<string> presentes)
        {
            return _alunoRepository.ImportarAulaEAusenciasAtomico(data, turmaId, presentes);
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

        public List<AlunoExibicaoDTO> ObterPerfilAluno(bool incluirArquivados = false)
        {
            var alunos = _alunoRepository.ObterPerfilAluno(incluirArquivados);
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

            ValidarDadosPessoais(aluno);
            _alunoRepository.Atualizar(aluno);
        }

        private void ValidarDadosPessoais(Aluno aluno)
        {
            if (string.IsNullOrWhiteSpace(aluno.Matricula)) throw new ArgumentException("Informe a matrícula do aluno.");
            if (string.IsNullOrWhiteSpace(aluno.Nome)) throw new ArgumentException("Informe o nome do aluno.");
            if (!ValidarEmailInstitucional(aluno.Email)) throw new ArgumentException("Informe um endereço de e-mail válido.");
        }

        public void Excluir(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                throw new ArgumentException("Informe a matrícula do aluno para exclusão.");
            _alunoRepository.Excluir(matricula);
        }

        public void Excluir(List<string> matriculas)
        {
            if (matriculas == null || matriculas.Count == 0)
                throw new ArgumentException("Selecione pelo menos um aluno para exclusão.");
            if (matriculas.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("A seleção contém matrícula inválida. Nenhum aluno foi excluído.");
            _alunoRepository.Excluir(matriculas);
        }
    }
}
