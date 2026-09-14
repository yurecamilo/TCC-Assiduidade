using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Testes de consistência de dados para o cadastro de alunos: garantem que a
    /// camada de serviço rejeita, ANTES de tocar no banco, qualquer aluno com dados
    /// incompletos ou inválidos. Todas as validações aqui disparam
    /// <see cref="ArgumentException"/> antes de qualquer chamada ao repositório,
    /// então os testes não dependem de conexão real com o MySQL.
    /// </summary>
    [TestClass]
    public class AlunoServiceTestes
    {
        private readonly AlunoService _service = new AlunoService();

        private static Aluno AlunoValido()
        {
            return new Aluno
            {
                Matricula = "2026001",
                Nome = "Aluno de Teste",
                Email = "aluno.teste@escola.com",
                TurmaId = 1,
                DataEntrada = DateTime.Today
            };
        }

        [TestMethod]
        public void Adicionar_ComMatriculaVazia_DeveLancarArgumentException()
        {
            var aluno = AlunoValido();
            aluno.Matricula = "";

            var ex = Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(aluno));
            StringAssert.Contains(ex.Message, "matrícula");
        }

        [TestMethod]
        public void Adicionar_ComNomeVazio_DeveLancarArgumentException()
        {
            var aluno = AlunoValido();
            aluno.Nome = "   ";

            var ex = Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(aluno));
            StringAssert.Contains(ex.Message, "nome");
        }

        [TestMethod]
        public void Adicionar_ComEmailInvalido_DeveLancarArgumentException()
        {
            var aluno = AlunoValido();
            aluno.Email = "isso-nao-e-um-email";

            var ex = Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(aluno));
            StringAssert.Contains(ex.Message, "e-mail");
        }

        [TestMethod]
        public void Adicionar_SemTurmaSelecionada_DeveLancarArgumentException()
        {
            var aluno = AlunoValido();
            aluno.TurmaId = 0;

            var ex = Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(aluno));
            StringAssert.Contains(ex.Message, "turma");
        }

        [TestMethod]
        public void Adicionar_ComAlunoNulo_NaoLancaExcecao()
        {
            // Consistência: chamar Adicionar(null) deve ser uma operação segura
            // (nenhum efeito), nunca um NullReferenceException.
            _service.Adicionar((Aluno)null);
        }

        [TestMethod]
        public void Atualizar_ComDadosInvalidos_DeveLancarArgumentException()
        {
            var aluno = AlunoValido();
            aluno.Nome = "";

            Assert.ThrowsException<ArgumentException>(() => _service.Atualizar(aluno));
        }

        [TestMethod]
        public void Excluir_ComMatriculaVazia_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Excluir(""));
        }

        [DataTestMethod]
        [DataRow("aluno@escola.com", true)]
        [DataRow("aluno.sobrenome@escola.com.br", true)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("sem-arroba-nem-dominio", false)]
        [DataRow("@dominio.com", false)]
        [DataRow("usuario@", false)]
        public void ValidarEmailInstitucional_ClassificaCorretamente(string email, bool esperado)
        {
            bool resultado = _service.ValidarEmailInstitucional(email);
            Assert.AreEqual(esperado, resultado, $"E-mail '{email}' deveria ser {(esperado ? "válido" : "inválido")}.");
        }
    }
}
