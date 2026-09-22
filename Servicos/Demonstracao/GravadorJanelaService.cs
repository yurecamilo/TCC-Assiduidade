using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TCC_Assiduidade.Servicos.Demonstracao
{
    internal sealed class GravadorJanelaService : IDisposable
    {
        private const int Largura = 1280;
        private const int Altura = 720;
        private const int QuadrosPorSegundo = 4;

        private readonly Window _janelaPrincipal;
        private readonly MjpegAviWriter _avi;
        private readonly string _caminhoCapa;
        private bool _capaSalva;

        public GravadorJanelaService(Window janelaPrincipal, string caminhoVideo, string caminhoCapa)
        {
            _janelaPrincipal = janelaPrincipal ?? throw new ArgumentNullException(nameof(janelaPrincipal));
            _caminhoCapa = caminhoCapa;
            _avi = new MjpegAviWriter(caminhoVideo, Largura, Altura, QuadrosPorSegundo);
        }

        public async Task MostrarPorAsync(string legenda, double segundos)
        {
            int quantidade = Math.Max(1, (int)Math.Round(segundos * QuadrosPorSegundo));
            for (int i = 0; i < quantidade; i++)
            {
                CapturarQuadro(legenda);
                await Task.Delay(1000 / QuadrosPorSegundo);
            }
        }

        public void Dispose()
        {
            _avi.Dispose();
        }

        private void CapturarQuadro(string legenda)
        {
            _janelaPrincipal.UpdateLayout();
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(25, 34, 41)), null,
                    new Rect(0, 0, Largura, Altura));

                FrameworkElement conteudoPrincipal = _janelaPrincipal.Content as FrameworkElement;
                if (conteudoPrincipal != null && conteudoPrincipal.ActualWidth > 0 && conteudoPrincipal.ActualHeight > 0)
                    DesenharVisualAjustado(dc, conteudoPrincipal, new Rect(0, 0, Largura, Altura));

                Window popup = Application.Current.Windows.Cast<Window>()
                    .LastOrDefault(w => w != _janelaPrincipal && w.IsVisible);
                if (popup != null)
                {
                    popup.UpdateLayout();
                    FrameworkElement conteudoPopup = popup.Content as FrameworkElement;
                    if (conteudoPopup != null && conteudoPopup.ActualWidth > 0 && conteudoPopup.ActualHeight > 0)
                    {
                        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(125, 0, 0, 0)), null,
                            new Rect(0, 0, Largura, Altura));
                        Rect areaPopup = CalcularAreaCentral(conteudoPopup.ActualWidth, conteudoPopup.ActualHeight, 900, 570);
                        dc.DrawRoundedRectangle(Brushes.White,
                            new Pen(new SolidColorBrush(Color.FromRgb(176, 200, 214)), 2),
                            new Rect(areaPopup.X - 3, areaPopup.Y - 3, areaPopup.Width + 6, areaPopup.Height + 6), 10, 10);
                        DesenharVisualAjustado(dc, conteudoPopup, areaPopup);
                    }
                }

                DesenharLegenda(dc, legenda);
            }

            var bitmap = new RenderTargetBitmap(Largura, Altura, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            var jpeg = new JpegBitmapEncoder { QualityLevel = 86 };
            jpeg.Frames.Add(BitmapFrame.Create(bitmap));
            using (var memoria = new MemoryStream())
            {
                jpeg.Save(memoria);
                _avi.AdicionarQuadro(memoria.ToArray());
            }

            if (!_capaSalva && !string.IsNullOrWhiteSpace(_caminhoCapa))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_caminhoCapa));
                var png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(bitmap));
                using (var arquivo = File.Create(_caminhoCapa))
                    png.Save(arquivo);
                _capaSalva = true;
            }
        }

        private static void DesenharVisualAjustado(DrawingContext dc, FrameworkElement elemento, Rect destino)
        {
            double escala = Math.Min(destino.Width / elemento.ActualWidth, destino.Height / elemento.ActualHeight);
            double largura = elemento.ActualWidth * escala;
            double altura = elemento.ActualHeight * escala;
            double x = destino.X + (destino.Width - largura) / 2;
            double y = destino.Y + (destino.Height - altura) / 2;
            var brush = new VisualBrush(elemento) { Stretch = Stretch.Fill };
            dc.DrawRectangle(brush, null, new Rect(x, y, largura, altura));
        }

        private static Rect CalcularAreaCentral(double larguraOriginal, double alturaOriginal, double maxLargura, double maxAltura)
        {
            double escala = Math.Min(maxLargura / larguraOriginal, maxAltura / alturaOriginal);
            escala = Math.Min(1.0, escala);
            double largura = larguraOriginal * escala;
            double altura = alturaOriginal * escala;
            return new Rect((Largura - largura) / 2, (Altura - altura) / 2 - 20, largura, altura);
        }

        private static void DesenharLegenda(DrawingContext dc, string legenda)
        {
            const double margem = 22;
            const double alturaPainel = 62;
            var fundo = new SolidColorBrush(Color.FromArgb(225, 17, 24, 28));
            dc.DrawRoundedRectangle(fundo, null,
                new Rect(margem, Altura - alturaPainel - margem, Largura - margem * 2, alturaPainel), 10, 10);

            var texto = new FormattedText(
                legenda ?? string.Empty,
                new CultureInfo("pt-BR"),
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                22,
                Brushes.White,
                1.0);
            texto.MaxTextWidth = Largura - margem * 4;
            texto.MaxTextHeight = alturaPainel - 14;
            dc.DrawText(texto, new Point(margem * 2, Altura - alturaPainel - margem + 15));
        }
    }
}
