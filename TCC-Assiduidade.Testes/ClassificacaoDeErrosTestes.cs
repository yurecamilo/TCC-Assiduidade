using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.ViewModel.Base;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Garante que cada tipo de exceção de domínio é exibido ao usuário com um
    /// título e uma mensagem próprios (não uma mensagem genérica única), e que
    /// qualquer exceção não classificada cai no texto padrão informado pelo
    /// chamador em vez de vazar detalhes técnicos.
    /// </summary>
    [TestClass]
    public class ClassificacaoDeErrosTestes
    {
        [TestMethod]
        public void RegistroDuplicado_UsaTituloETextoProprios()
        {
            var excecao = new RegistroDuplicadoException("Já existe a turma cadastrada.");

            BaseViewModel.ClassificarErro(excecao, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Registro duplicado", titulo);
            Assert.AreEqual(excecao.Message, mensagem);
        }

        [TestMethod]
        public void IntegridadeReferencial_UsaTituloETextoProprios()
        {
            var excecao = new IntegridadeReferencialException("Existem alunos vinculados.");

            BaseViewModel.ClassificarErro(excecao, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Ação bloqueada", titulo);
            Assert.AreEqual(excecao.Message, mensagem);
        }

        [TestMethod]
        public void RegistroNaoEncontrado_UsaTituloETextoProprios()
        {
            var excecao = new RegistroNaoEncontradoException("Aluno não encontrado.");

            BaseViewModel.ClassificarErro(excecao, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Registro não encontrado", titulo);
            Assert.AreEqual(excecao.Message, mensagem);
        }

        [TestMethod]
        public void FalhaDeConexao_UsaTituloETextoProprios()
        {
            var excecao = new ConexaoBancoDadosException("Sem conexão com o banco.");

            BaseViewModel.ClassificarErro(excecao, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Falha de conexão", titulo);
            Assert.AreEqual(excecao.Message, mensagem);
        }

        [TestMethod]
        public void DadosInvalidos_UsaTituloETextoProprios()
        {
            var excecao = new ArgumentException("O nome é obrigatório.");

            BaseViewModel.ClassificarErro(excecao, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Dados inválidos", titulo);
            Assert.AreEqual(excecao.Message, mensagem);
        }

        [TestMethod]
        public void ExcecaoNaoClassificada_UsaMensagemPadraoDoChamador()
        {
            var excecao = new InvalidOperationException("detalhe técnico interno");

            BaseViewModel.ClassificarErro(excecao, "Não foi possível concluir a operação.", out string titulo, out string mensagem);

            Assert.AreEqual("Erro", titulo);
            Assert.AreEqual("Não foi possível concluir a operação.", mensagem);
        }

        [TestMethod]
        public void SemExcecao_UsaMensagemPadraoDoChamador()
        {
            BaseViewModel.ClassificarErro(null, "mensagem padrão", out string titulo, out string mensagem);

            Assert.AreEqual("Erro", titulo);
            Assert.AreEqual("mensagem padrão", mensagem);
        }
    }
}
