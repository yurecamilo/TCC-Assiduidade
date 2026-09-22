using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TCC_Assiduidade.Servicos.Demonstracao
{
    internal sealed class MjpegAviWriter : IDisposable
    {
        private sealed class IndiceQuadro
        {
            public int Offset { get; set; }
            public int Tamanho { get; set; }
        }

        private readonly FileStream _arquivo;
        private readonly BinaryWriter _writer;
        private readonly List<IndiceQuadro> _indices = new List<IndiceQuadro>();
        private readonly int _fps;
        private readonly long _posicaoTamanhoRiff;
        private readonly long _posicaoTotalQuadros;
        private readonly long _posicaoComprimentoStream;
        private readonly long _posicaoBufferAvih;
        private readonly long _posicaoBufferStrh;
        private readonly long _posicaoMaxBytesSegundo;
        private readonly long _posicaoTamanhoMovi;
        private readonly long _inicioDadosMovi;
        private int _maiorQuadro;
        private bool _finalizado;

        public MjpegAviWriter(string caminho, int largura, int altura, int fps)
        {
            if (largura <= 0 || altura <= 0 || fps <= 0)
                throw new ArgumentOutOfRangeException("As dimensões e a taxa de quadros devem ser positivas.");

            Directory.CreateDirectory(Path.GetDirectoryName(caminho));
            _fps = fps;
            _arquivo = new FileStream(caminho, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _writer = new BinaryWriter(_arquivo, Encoding.ASCII, true);

            EscreverFourCc("RIFF");
            _posicaoTamanhoRiff = _arquivo.Position;
            _writer.Write(0);
            EscreverFourCc("AVI ");

            EscreverFourCc("LIST");
            long posicaoTamanhoHdrl = _arquivo.Position;
            _writer.Write(0);
            EscreverFourCc("hdrl");

            EscreverFourCc("avih");
            _writer.Write(56);
            _writer.Write(1000000 / fps);
            _posicaoMaxBytesSegundo = _arquivo.Position;
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0x10);
            _posicaoTotalQuadros = _arquivo.Position;
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(1);
            _posicaoBufferAvih = _arquivo.Position;
            _writer.Write(0);
            _writer.Write(largura);
            _writer.Write(altura);
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0);

            EscreverFourCc("LIST");
            long posicaoTamanhoStrl = _arquivo.Position;
            _writer.Write(0);
            EscreverFourCc("strl");

            EscreverFourCc("strh");
            _writer.Write(56);
            EscreverFourCc("vids");
            EscreverFourCc("MJPG");
            _writer.Write(0);
            _writer.Write((short)0);
            _writer.Write((short)0);
            _writer.Write(0);
            _writer.Write(1);
            _writer.Write(fps);
            _writer.Write(0);
            _posicaoComprimentoStream = _arquivo.Position;
            _writer.Write(0);
            _posicaoBufferStrh = _arquivo.Position;
            _writer.Write(0);
            _writer.Write(-1);
            _writer.Write(0);
            _writer.Write((short)0);
            _writer.Write((short)0);
            _writer.Write((short)largura);
            _writer.Write((short)altura);

            EscreverFourCc("strf");
            _writer.Write(40);
            _writer.Write(40);
            _writer.Write(largura);
            _writer.Write(altura);
            _writer.Write((short)1);
            _writer.Write((short)24);
            EscreverFourCc("MJPG");
            _writer.Write(largura * altura * 3);
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0);
            _writer.Write(0);

            AtualizarInteiro(posicaoTamanhoStrl, checked((int)(_arquivo.Position - posicaoTamanhoStrl - 4)));
            AtualizarInteiro(posicaoTamanhoHdrl, checked((int)(_arquivo.Position - posicaoTamanhoHdrl - 4)));

            EscreverFourCc("LIST");
            _posicaoTamanhoMovi = _arquivo.Position;
            _writer.Write(0);
            EscreverFourCc("movi");
            _inicioDadosMovi = _arquivo.Position;
        }

        public void AdicionarQuadro(byte[] jpeg)
        {
            if (_finalizado)
                throw new ObjectDisposedException(nameof(MjpegAviWriter));
            if (jpeg == null || jpeg.Length == 0)
                throw new ArgumentException("O quadro JPEG não pode ser vazio.", nameof(jpeg));

            long inicioChunk = _arquivo.Position;
            EscreverFourCc("00dc");
            _writer.Write(jpeg.Length);
            _writer.Write(jpeg);
            if ((jpeg.Length & 1) != 0)
                _writer.Write((byte)0);

            _indices.Add(new IndiceQuadro
            {
                Offset = checked((int)(inicioChunk - _inicioDadosMovi)),
                Tamanho = jpeg.Length
            });
            if (jpeg.Length > _maiorQuadro)
                _maiorQuadro = jpeg.Length;
        }

        public void Dispose()
        {
            if (_finalizado)
                return;
            _finalizado = true;

            long fimMovi = _arquivo.Position;
            AtualizarInteiro(_posicaoTamanhoMovi, checked((int)(fimMovi - _posicaoTamanhoMovi - 4)));

            EscreverFourCc("idx1");
            _writer.Write(_indices.Count * 16);
            foreach (IndiceQuadro indice in _indices)
            {
                EscreverFourCc("00dc");
                _writer.Write(0x10);
                _writer.Write(indice.Offset);
                _writer.Write(indice.Tamanho);
            }

            AtualizarInteiro(_posicaoTotalQuadros, _indices.Count);
            AtualizarInteiro(_posicaoComprimentoStream, _indices.Count);
            AtualizarInteiro(_posicaoBufferAvih, _maiorQuadro);
            AtualizarInteiro(_posicaoBufferStrh, _maiorQuadro);
            AtualizarInteiro(_posicaoMaxBytesSegundo, _maiorQuadro * _fps);
            AtualizarInteiro(_posicaoTamanhoRiff, checked((int)(_arquivo.Length - 8)));

            _writer.Flush();
            _writer.Dispose();
            _arquivo.Dispose();
        }

        private void EscreverFourCc(string valor)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(valor);
            if (bytes.Length != 4)
                throw new ArgumentException("Um FOURCC deve ter quatro caracteres.", nameof(valor));
            _writer.Write(bytes);
        }

        private void AtualizarInteiro(long posicao, int valor)
        {
            long atual = _arquivo.Position;
            _arquivo.Position = posicao;
            _writer.Write(valor);
            _arquivo.Position = atual;
        }
    }
}
