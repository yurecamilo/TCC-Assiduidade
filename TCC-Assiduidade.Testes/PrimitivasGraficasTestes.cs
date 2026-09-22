using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Windows.Media;
using TCC_Assiduidade.View.Controls;

namespace TCC_Assiduidade.Testes
{
    [TestClass]
    public class PrimitivasGraficasTestes
    {
        [TestMethod]
        public void Cor_UsaOsComponentesInformados()
        {
            Color cor = PrimitivasGraficas.Cor(10, 20, 30);

            Assert.AreEqual((byte)10, cor.R);
            Assert.AreEqual((byte)20, cor.G);
            Assert.AreEqual((byte)30, cor.B);
            Assert.AreEqual((byte)255, cor.A);
        }

        [TestMethod]
        public void PincelECaneta_SaoCongeladosEReutilizaveis()
        {
            ExecutarEmSta(() =>
            {
                Brush pincel = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(1, 2, 3));
                Pen caneta = PrimitivasGraficas.CanetaArredondada(pincel, 3);

                Assert.IsTrue(pincel.IsFrozen);
                Assert.IsTrue(caneta.IsFrozen);
                Assert.AreEqual(3d, caneta.Thickness);
                Assert.AreEqual(PenLineCap.Round, caneta.StartLineCap);
                Assert.AreEqual(PenLineCap.Round, caneta.EndLineCap);
            });
        }

        [TestMethod]
        public void ControlesGraficos_AceitamColecoesPelasDependencyProperties()
        {
            ExecutarEmSta(() =>
            {
                var linha = new DashboardLineChart();
                var barras = new DashboardBarChart();
                var rosca = new DashboardDonutChart();

                Assert.IsNotNull(DashboardLineChart.PontosProperty);
                Assert.IsNotNull(DashboardBarChart.PontosProperty);
                Assert.IsNotNull(DashboardDonutChart.ResultadoProperty);
                Assert.IsNotNull(linha);
                Assert.IsNotNull(barras);
                Assert.IsNotNull(rosca);
            });
        }

        private static void ExecutarEmSta(Action acao)
        {
            Exception erro = null;
            var thread = new Thread(() =>
            {
                try
                {
                    acao();
                }
                catch (Exception ex)
                {
                    erro = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (erro != null)
                throw erro;
        }
    }
}
