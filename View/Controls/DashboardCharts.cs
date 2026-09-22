using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using TCC_Assiduidade.Modelos.Dashboard;

namespace TCC_Assiduidade.View.Controls
{
    internal static class CoresDashboard
    {
        public static readonly Brush Texto = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(82, 100, 105));
        public static readonly Brush TextoForte = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(24, 54, 58));
        public static readonly Brush Grade = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(207, 224, 221));
        public static readonly Brush Azul = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(62, 120, 178));
        public static readonly Brush Verde = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(47, 143, 115));
        public static readonly Brush VerdeEscuro = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(35, 107, 115));
        public static readonly Brush Amarelo = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(212, 148, 58));
        public static readonly Brush Vermelho = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(200, 94, 98));
        public static readonly Brush Fundo = PrimitivasGraficas.Pincel(PrimitivasGraficas.Cor(215, 231, 228));
    }

    public class DashboardLineChart : FrameworkElement
    {
        public bool MostrarSerieSecundaria { get; set; }

        public DashboardLineChart()
        {
            DataContextChanged += AtualizarGrafico;
        }

        protected override void OnRender(DrawingContext desenho)
        {
            base.OnRender(desenho);

            List<PontoGraficoDashboard> pontos = DataContext as List<PontoGraficoDashboard>;
            if (pontos == null || pontos.Count == 0 || ActualWidth < 160 || ActualHeight < 100)
            {
                DesenharSemDados(desenho, ActualWidth, ActualHeight);
                return;
            }

            const double margemEsquerda = 48;
            const double margemDireita = 18;
            const double margemTopo = 18;
            const double margemInferior = 34;
            double largura = Math.Max(1, ActualWidth - margemEsquerda - margemDireita);
            double altura = Math.Max(1, ActualHeight - margemTopo - margemInferior);
            int[] percentuais = { 100, 75, 50, 25, 0 };

            foreach (int percentual in percentuais)
            {
                double y = CalcularY(percentual, margemTopo, altura);
                PrimitivasGraficas.Reta(
                    desenho,
                    PrimitivasGraficas.Caneta(CoresDashboard.Grade),
                    margemEsquerda,
                    y,
                    margemEsquerda + largura,
                    y);

                PrimitivasGraficas.DesenharTexto(
                    desenho,
                    percentual + "%",
                    new Point(6, y - 7),
                    CoresDashboard.Texto,
                    11);
            }

            double passo = pontos.Count == 1 ? 0 : largura / (pontos.Count - 1);
            var linhaPrincipal = new List<Point>();
            var linhaSecundaria = new List<Point>();

            for (int i = 0; i < pontos.Count; i++)
            {
                double x = margemEsquerda + i * passo;
                linhaPrincipal.Add(new Point(x, CalcularY(pontos[i].Valor, margemTopo, altura)));

                if (pontos[i].ValorSecundario.HasValue)
                    linhaSecundaria.Add(new Point(x, CalcularY(pontos[i].ValorSecundario.Value, margemTopo, altura)));
            }

            if (MostrarSerieSecundaria && linhaSecundaria.Count == pontos.Count)
                DesenharLinha(desenho, linhaSecundaria, CoresDashboard.Verde);

            DesenharLinha(desenho, linhaPrincipal, CoresDashboard.Azul);

            int intervaloRotulo = pontos.Count <= 8 ? 1 : (int)Math.Ceiling(pontos.Count / 8.0);
            for (int i = 0; i < pontos.Count; i++)
            {
                Point ponto = linhaPrincipal[i];
                PrimitivasGraficas.Circulo(desenho, CoresDashboard.Azul, null, ponto, 4);

                if (MostrarSerieSecundaria && linhaSecundaria.Count == pontos.Count)
                    PrimitivasGraficas.Circulo(desenho, CoresDashboard.Verde, null, linhaSecundaria[i], 3);

                if (i % intervaloRotulo == 0 || i == pontos.Count - 1)
                {
                    FormattedText texto = PrimitivasGraficas.Texto(pontos[i].Rotulo, CoresDashboard.Texto, 11);
                    double textoX = Math.Max(
                        margemEsquerda,
                        Math.Min(ActualWidth - margemDireita - texto.Width, ponto.X - texto.Width / 2));

                    PrimitivasGraficas.DesenharTexto(
                        desenho,
                        texto,
                        new Point(textoX, ActualHeight - margemInferior + 10));
                }
            }
        }

        private void AtualizarGrafico(object sender, DependencyPropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private static double CalcularY(double percentual, double margemTopo, double altura)
        {
            double valor = Math.Max(0, Math.Min(100, percentual));
            return margemTopo + (100 - valor) * altura / 100.0;
        }

        private static void DesenharLinha(DrawingContext desenho, List<Point> pontos, Brush cor)
        {
            PrimitivasGraficas.Polilinha(
                desenho,
                PrimitivasGraficas.Caneta(cor, 3),
                pontos);
        }

        internal static void DesenharSemDados(DrawingContext desenho, double largura, double altura)
        {
            FormattedText texto = PrimitivasGraficas.Texto(
                "Sem dados para o período selecionado.",
                CoresDashboard.Texto,
                12);

            PrimitivasGraficas.DesenharTexto(
                desenho,
                texto,
                new Point(
                    Math.Max(0, (largura - texto.Width) / 2),
                    Math.Max(0, (altura - texto.Height) / 2)));
        }
    }

    public class DashboardBarChart : FrameworkElement
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");

        public DashboardBarChart()
        {
            DataContextChanged += AtualizarGrafico;
        }

        protected override void OnRender(DrawingContext desenho)
        {
            base.OnRender(desenho);

            List<PontoGraficoDashboard> origem = DataContext as List<PontoGraficoDashboard>;
            var pontos = new List<PontoGraficoDashboard>();
            if (origem != null)
            {
                int quantidade = Math.Min(12, origem.Count);
                for (int i = 0; i < quantidade; i++)
                    pontos.Add(origem[i]);
            }

            if (pontos.Count == 0 || ActualWidth < 220 || ActualHeight < 100)
            {
                DashboardLineChart.DesenharSemDados(desenho, ActualWidth, ActualHeight);
                return;
            }

            double alturaLinha = ActualHeight / pontos.Count;
            double alturaBarra = Math.Min(14, alturaLinha * 0.42);
            double larguraRotulo = Math.Min(150, Math.Max(95, ActualWidth * 0.28));
            double inicioBarra = larguraRotulo + 12;
            double larguraBarra = Math.Max(20, ActualWidth - inicioBarra - 56);

            for (int i = 0; i < pontos.Count; i++)
            {
                PontoGraficoDashboard ponto = pontos[i];
                double centroY = i * alturaLinha + alturaLinha / 2;
                double valor = Math.Max(0, Math.Min(100, ponto.Valor));
                string rotulo = Abreviar(ponto.Rotulo, 22);

                FormattedText textoRotulo = PrimitivasGraficas.Texto(rotulo, CoresDashboard.TextoForte, 11);
                PrimitivasGraficas.DesenharTexto(
                    desenho,
                    textoRotulo,
                    new Point(0, centroY - textoRotulo.Height / 2));

                var fundo = new Rect(inicioBarra, centroY - alturaBarra / 2, larguraBarra, alturaBarra);
                PrimitivasGraficas.RetanguloArredondado(
                    desenho,
                    CoresDashboard.Fundo,
                    null,
                    fundo,
                    alturaBarra / 2);

                var preenchimento = new Rect(
                    inicioBarra,
                    centroY - alturaBarra / 2,
                    larguraBarra * valor / 100.0,
                    alturaBarra);

                PrimitivasGraficas.RetanguloArredondado(
                    desenho,
                    ObterCor(valor),
                    null,
                    preenchimento,
                    alturaBarra / 2);

                string percentual = valor.ToString("0.#", Cultura) + "%";
                FormattedText textoValor = PrimitivasGraficas.Texto(percentual, CoresDashboard.TextoForte, 11);
                PrimitivasGraficas.DesenharTexto(
                    desenho,
                    textoValor,
                    new Point(ActualWidth - textoValor.Width, centroY - textoValor.Height / 2));
            }
        }

        private void AtualizarGrafico(object sender, DependencyPropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        private static Brush ObterCor(double valor)
        {
            if (valor < 75)
                return CoresDashboard.Vermelho;

            if (valor < 85)
                return CoresDashboard.Amarelo;

            return CoresDashboard.VerdeEscuro;
        }

        private static string Abreviar(string valor, int limite)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            if (valor.Length <= limite)
                return valor;

            return valor.Substring(0, limite - 1) + "…";
        }
    }

    public class DashboardDonutChart : FrameworkElement
    {
        private static readonly CultureInfo Cultura = new CultureInfo("pt-BR");

        public DashboardDonutChart()
        {
            DataContextChanged += AtualizarGrafico;
        }

        protected override void OnRender(DrawingContext desenho)
        {
            base.OnRender(desenho);

            ResultadoDashboardBase resultado = DataContext as ResultadoDashboardBase;
            int presentes = resultado == null ? 0 : resultado.Presentes;
            int ausentes = resultado == null ? 0 : resultado.Ausentes;
            int total = Math.Max(0, presentes) + Math.Max(0, ausentes);
            double percentual = total == 0 ? 0 : Math.Max(0, presentes) * 100.0 / total;

            double tamanho = Math.Max(20, Math.Min(ActualWidth, ActualHeight) - 26);
            double espessura = Math.Max(12, tamanho * 0.14);
            double raio = (tamanho - espessura) / 2;
            var centro = new Point(ActualWidth / 2, ActualHeight / 2);

            Pen canetaFundo = PrimitivasGraficas.Caneta(CoresDashboard.Fundo, espessura);
            PrimitivasGraficas.Circulo(desenho, null, canetaFundo, centro, raio);

            if (percentual >= 99.999)
            {
                Pen canetaCompleta = PrimitivasGraficas.Caneta(CoresDashboard.Verde, espessura);
                PrimitivasGraficas.Circulo(desenho, null, canetaCompleta, centro, raio);
            }
            else if (percentual > 0)
            {
                Pen caneta = PrimitivasGraficas.CanetaArredondada(CoresDashboard.Verde, espessura);
                PrimitivasGraficas.Arco(desenho, caneta, centro, raio, -90, percentual * 3.6);
            }

            FormattedText valor = PrimitivasGraficas.Texto(
                percentual.ToString("0.#", Cultura) + "%",
                CoresDashboard.TextoForte,
                25);
            FormattedText legenda = PrimitivasGraficas.Texto("presença", CoresDashboard.Texto, 11);

            PrimitivasGraficas.DesenharTexto(
                desenho,
                valor,
                new Point(centro.X - valor.Width / 2, centro.Y - valor.Height));
            PrimitivasGraficas.DesenharTexto(
                desenho,
                legenda,
                new Point(centro.X - legenda.Width / 2, centro.Y + 5));
        }

        private void AtualizarGrafico(object sender, DependencyPropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }
    }
}
