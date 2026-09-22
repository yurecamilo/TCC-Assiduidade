using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class AusenciaServiceTestes
    {
        [TestMethod]
        public void Adicionar_ComAulaInvalida_LancaArgumentException()
        {
            var service = new AusenciaService();

            Assert.ThrowsException<ArgumentException>(() =>
                service.Adicionar(0, new List<string>()));
        }

        [TestMethod]
        public void Adicionar_ComListaNula_LancaArgumentNullException()
        {
            var service = new AusenciaService();

            Assert.ThrowsException<ArgumentNullException>(() =>
                service.Adicionar(1, null));
        }

        [TestMethod]
        public void Adicionar_ComListaVazia_NaoAcessaBanco()
        {
            var service = new AusenciaService();

            service.Adicionar(1, new List<string>());
        }
    }
}
