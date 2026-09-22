using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace TCC_Assiduidade.View.Controls
{
    /// <summary>Primitivas usadas nos gráficos da dashboard.</summary>
    internal static class PrimitivasGraficas
    {
        public static Color Cor(byte vermelho, byte verde, byte azul)
        {
            return Color.FromRgb(vermelho, verde, azul);
        }

        public static Brush Pincel(Color cor)
        {
            var pincel = new SolidColorBrush(cor);
            pincel.Freeze();
            return pincel;
        }

        public static Pen Caneta(Brush pincel)
        {
            return Caneta(pincel, 1);
        }

        public static Pen Caneta(Brush pincel, double espessura)
        {
            var caneta = new Pen(pincel, espessura);
            caneta.Freeze();
            return caneta;
        }

        public static Pen CanetaArredondada(Brush pincel, double espessura)
        {
            var caneta = new Pen(pincel, espessura)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round
            };
            caneta.Freeze();
            return caneta;
        }

        public static void Reta(
            DrawingContext desenho,
            Pen caneta,
            double xInicial,
            double yInicial,
            double xFinal,
            double yFinal)
        {
            desenho.DrawLine(
                caneta,
                new Point(xInicial, yInicial),
                new Point(xFinal, yFinal));
        }

        public static void Circulo(
            DrawingContext desenho,
            Brush preenchimento,
            Pen contorno,
            Point centro,
            double raio)
        {
            desenho.DrawEllipse(preenchimento, contorno, centro, raio, raio);
        }

        public static void RetanguloArredondado(
            DrawingContext desenho,
            Brush preenchimento,
            Pen contorno,
            Rect retangulo,
            double raio)
        {
            desenho.DrawRoundedRectangle(preenchimento, contorno, retangulo, raio, raio);
        }

        public static FormattedText Texto(string conteudo, Brush pincel, double tamanho)
        {
            return new FormattedText(
                conteudo ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                tamanho,
                pincel,
                1.0);
        }

        public static void DesenharTexto(
            DrawingContext desenho,
            string conteudo,
            Point posicao,
            Brush pincel,
            double tamanho)
        {
            desenho.DrawText(Texto(conteudo, pincel, tamanho), posicao);
        }

        public static void DesenharTexto(
            DrawingContext desenho,
            FormattedText texto,
            Point posicao)
        {
            desenho.DrawText(texto, posicao);
        }

        public static void Polilinha(
            DrawingContext desenho,
            Pen caneta,
            IList<Point> pontos)
        {
            if (pontos == null || pontos.Count == 0)
                return;

            for (int i = 1; i < pontos.Count; i++)
                desenho.DrawLine(caneta, pontos[i - 1], pontos[i]);
        }

        public static void Arco(
            DrawingContext desenho,
            Pen caneta,
            Point centro,
            double raio,
            double anguloInicial,
            double angulo)
        {
            if (angulo <= 0)
                return;

            double anguloLimitado = Math.Min(359.99, angulo);
            Point pontoInicial = PontoNoCirculo(centro, raio, anguloInicial);
            Point pontoFinal = PontoNoCirculo(centro, raio, anguloInicial + anguloLimitado);

            var figura = new PathFigure
            {
                StartPoint = pontoInicial,
                IsClosed = false
            };
            figura.Segments.Add(new ArcSegment
            {
                Point = pontoFinal,
                Size = new Size(raio, raio),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = anguloLimitado > 180
            });

            var geometria = new PathGeometry(new[] { figura });
            geometria.Freeze();
            desenho.DrawGeometry(null, caneta, geometria);
        }

        private static Point PontoNoCirculo(Point centro, double raio, double angulo)
        {
            double radianos = angulo * Math.PI / 180.0;
            return new Point(
                centro.X + raio * Math.Cos(radianos),
                centro.Y + raio * Math.Sin(radianos));
        }
    }
}
