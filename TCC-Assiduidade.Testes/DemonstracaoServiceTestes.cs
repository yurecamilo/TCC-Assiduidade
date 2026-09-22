using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TCC_Assiduidade.Servicos.Demonstracao;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class DemonstracaoServiceTestes
    {
        [TestMethod]
        public void ValidarQuantidadeMassa_ComNenhumRegistro_Aceita()
        {
            DemonstracaoService.ValidarQuantidadeMassa(0, 5);
        }

        [TestMethod]
        public void ValidarQuantidadeMassa_ComMassaCompleta_Aceita()
        {
            DemonstracaoService.ValidarQuantidadeMassa(5, 5);
        }

        [TestMethod]
        public void ValidarQuantidadeMassa_ComMassaParcial_LancaErroExplicativo()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                DemonstracaoService.ValidarQuantidadeMassa(2, 5));

            StringAssert.Contains(ex.Message, "2 de 5");
            StringAssert.Contains(ex.Message, "incompleta");
        }

        [TestMethod]
        public void ValidarQuantidadeMassa_ComQuantidadeImpossivel_LancaArgumentOutOfRange()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                DemonstracaoService.ValidarQuantidadeMassa(6, 5));
        }
    }
}
