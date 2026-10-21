using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Testes de consistência de dados para o registro de aulas: uma aula não pode
    /// ser criada com data futura (assiduidade só faz sentido para aulas que já
    /// aconteceram) nem vinculada a uma turma inválida.
    /// </summary>
    [TestClass]
    public class AulaServiceTestes
    {
        private readonly AulaService _service = new AulaService();

        [TestMethod]
        public void Adicionar_ComDataFutura_DeveLancarArgumentException()
        {
            DateTime dataFutura = DateTime.Now.AddDays(5);

            var ex = Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(dataFutura, 1));
            StringAssert.Contains(ex.Message, "futura");
        }

        [TestMethod]
        public void Adicionar_ComTurmaInvalida_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _service.Adicionar(DateTime.Now.AddDays(-1), 0));
        }

        [TestMethod]
        public void AdicionarComAusencias_ComListaNula_DeveLancarArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                _service.AdicionarComAusencias(DateTime.Today, 1, null));
        }

        [TestMethod]
        public void AdicionarComAusencias_ComMatriculaRepetida_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                _service.AdicionarComAusencias(DateTime.Today, 1,
                    new List<string> { "2026001", "2026001" }));
        }
    }
}
