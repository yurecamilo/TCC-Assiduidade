using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.Repositories;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Garante que cada código de erro do MySQL é traduzido para o tipo de exceção
    /// de domínio correto — a base de "uma mensagem personalizada para cada tipo de
    /// erro" pedida para o sistema. Usa <see cref="MySqlExceptionTranslator.TraduzirPorNumero"/>
    /// para não depender de uma conexão real com o banco.
    /// </summary>
    [TestClass]
    public class MySqlExceptionTranslatorTestes
    {
        [TestMethod]
        public void EntradaDuplicada_1062_DeveVirarRegistroDuplicadoException()
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(1062, "Duplicate entry", "a turma");

            Assert.IsInstanceOfType(excecao, typeof(RegistroDuplicadoException));
            StringAssert.Contains(excecao.Message, "Já existe");
        }

        [TestMethod]
        public void LinhaReferenciadaPai_1451_DeveVirarIntegridadeReferencialException()
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(1451, "Cannot delete or update a parent row", "a turma");

            Assert.IsInstanceOfType(excecao, typeof(IntegridadeReferencialException));
        }

        [TestMethod]
        public void LinhaReferenciadaFilho_1452_DeveVirarIntegridadeReferencialException()
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(1452, "Cannot add or update a child row", "a aula");

            Assert.IsInstanceOfType(excecao, typeof(IntegridadeReferencialException));
            StringAssert.Contains(excecao.Message, "não existe mais");
        }

        [DataTestMethod]
        [DataRow(1045)] // acesso negado
        [DataRow(1049)] // banco desconhecido
        [DataRow(2003)] // servidor inacessível
        [DataRow(2006)] // servidor indisponível
        [DataRow(2013)] // conexão perdida
        [DataRow(2055)] // conexão perdida (mensagem estendida)
        public void ErrosDeConexao_DevemVirarConexaoBancoDadosException(int numeroErro)
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(numeroErro, "erro de conexão", "o aluno");

            Assert.IsInstanceOfType(excecao, typeof(ConexaoBancoDadosException));
        }

        [TestMethod]
        public void ErroDesconhecido_CaiNoFallbackGenericoComDetalheTecnico()
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(9999, "algo estranho aconteceu", "o aluno");

            StringAssert.Contains(excecao.Message, "algo estranho aconteceu");
        }

        [TestMethod]
        public void Traduzir_ComContextoVazio_UsaTextoPadrao()
        {
            var excecao = MySqlExceptionTranslator.TraduzirPorNumero(1062, "Duplicate entry", "");

            StringAssert.Contains(excecao.Message, "o registro");
        }

        [TestMethod]
        public void Traduzir_ComExcecaoNula_LancaArgumentNullException()
        {
            Assert.ThrowsException<System.ArgumentNullException>(() =>
                MySqlExceptionTranslator.Traduzir(null, "o registro"));
        }
    }
}
