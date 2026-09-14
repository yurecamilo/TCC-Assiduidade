using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Testes de consistência de dados para turmas: nome obrigatório e
    /// identificadores válidos antes de qualquer alteração ou exclusão. Como as
    /// validações abaixo disparam antes de qualquer acesso ao repositório, os
    /// testes não dependem de conexão real com o banco.
    /// </summary>
    [TestClass]
    public class TurmaServiceTestes
    {
        private readonly TurmaService _service = new TurmaService();

        [TestMethod]
        public void Adicionar_ComNomeVazio_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(""));
        }

        [TestMethod]
        public void Adicionar_ComNomeNulo_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(null));
        }

        [TestMethod]
        public void Atualizar_ComIdInvalido_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Atualizar(0, "Nova turma"));
        }

        [TestMethod]
        public void Atualizar_ComNomeVazio_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Atualizar(1, "  "));
        }

        [TestMethod]
        public void Excluir_ComIdInvalido_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Excluir(-1));
        }

        [TestMethod]
        public void Excluir_ComIdZero_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Excluir(0));
        }

        [TestMethod]
        public void ObterTurmaPorNome_ComNomeVazio_RetornaNuloSemAcessarBanco()
        {
            // Consistência: nome em branco não deve gerar uma busca no banco, e sim
            // já retornar "não encontrado" imediatamente.
            var resultado = _service.ObterTurmaPorNome("   ");
            Assert.IsNull(resultado);
        }

        [TestMethod]
        public void ObterTurmaPorId_ComIdInvalido_RetornaNuloSemAcessarBanco()
        {
            var resultado = _service.ObterTurmaPorId(0);
            Assert.IsNull(resultado);
        }
    }
}
