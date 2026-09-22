using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using TCC_Assiduidade.Servicos.Demonstracao;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class MjpegAviWriterTestes
    {
        [TestMethod]
        public void Construtor_ComLarguraInvalida_InformaParametroCorreto()
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new MjpegAviWriter("video.avi", 0, 720, 4));

            Assert.AreEqual("largura", ex.ParamName);
        }

        [TestMethod]
        public void Construtor_ComAlturaInvalida_InformaParametroCorreto()
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new MjpegAviWriter("video.avi", 1280, 0, 4));

            Assert.AreEqual("altura", ex.ParamName);
        }

        [TestMethod]
        public void Construtor_ComFpsInvalido_InformaParametroCorreto()
        {
            var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new MjpegAviWriter("video.avi", 1280, 720, 0));

            Assert.AreEqual("fps", ex.ParamName);
        }

        [TestMethod]
        public void AdicionarQuadro_EFinalizar_GeraEstruturaAvi()
        {
            string caminho = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".avi");
            try
            {
                using (var writer = new MjpegAviWriter(caminho, 320, 240, 2))
                {
                    writer.AdicionarQuadro(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });
                }

                byte[] bytes = File.ReadAllBytes(caminho);
                string texto = Encoding.ASCII.GetString(bytes);

                Assert.AreEqual("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
                Assert.AreEqual("AVI ", Encoding.ASCII.GetString(bytes, 8, 4));
                StringAssert.Contains(texto, "movi");
                StringAssert.Contains(texto, "idx1");
            }
            finally
            {
                if (File.Exists(caminho))
                    File.Delete(caminho);
            }
        }
    }
}
