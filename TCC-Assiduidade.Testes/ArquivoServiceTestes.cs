using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCC_Assiduidade.Servicos;

namespace TCC_Assiduidade.Testes
{
    /// <summary>
    /// Testes de integridade dos arquivos CSV importados pelo sistema: cabeçalho
    /// duplicado ou vazio, arquivo inexistente e leitura correta com vírgula ou
    /// ponto e vírgula. Cada teste grava um arquivo temporário próprio e o remove
    /// ao final, sem depender de nenhum arquivo externo.
    /// </summary>
    [TestClass]
    public class ArquivoServiceTestes
    {
        private string _arquivoTemporario;

        [TestInitialize]
        public void Preparar()
        {
            _arquivoTemporario = Path.Combine(Path.GetTempPath(), $"tcc_teste_{Guid.NewGuid():N}.csv");
        }

        [TestCleanup]
        public void Limpar()
        {
            if (File.Exists(_arquivoTemporario))
                File.Delete(_arquivoTemporario);
        }

        [TestMethod]
        public void LerCsv_ComCaminhoVazio_DeveLancarArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => ArquivoService.LerCsv(""));
        }

        [TestMethod]
        public void LerCsv_ComArquivoInexistente_DeveLancarFileNotFoundException()
        {
            string caminhoInexistente = Path.Combine(Path.GetTempPath(), $"nao_existe_{Guid.NewGuid():N}.csv");

            Assert.ThrowsException<FileNotFoundException>(() => ArquivoService.LerCsv(caminhoInexistente));
        }

        [TestMethod]
        public void LerCsv_ComCabecalhoDuplicado_DeveLancarInvalidDataException()
        {
            File.WriteAllText(_arquivoTemporario, "matricula,nome,matricula\n123,Fulano,123\n");

            Assert.ThrowsException<InvalidDataException>(() => ArquivoService.LerCsv(_arquivoTemporario));
        }

        [TestMethod]
        public void LerCsv_ComColunaDeCabecalhoVazia_DeveLancarInvalidDataException()
        {
            File.WriteAllText(_arquivoTemporario, "matricula,,email\n123,Fulano,fulano@escola.com\n");

            Assert.ThrowsException<InvalidDataException>(() => ArquivoService.LerCsv(_arquivoTemporario));
        }

        [TestMethod]
        public void LerCsv_ComDadosValidos_RetornaLinhasComOsCamposCorretos()
        {
            File.WriteAllText(_arquivoTemporario, "matricula,nome,email\n123,Fulano de Tal,fulano@escola.com\n456,Beltrana,beltrana@escola.com\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual(2, resultado.Count);
            Assert.AreEqual("123", resultado[0]["matricula"]);
            Assert.AreEqual("Fulano de Tal", resultado[0]["nome"]);
            Assert.AreEqual("beltrana@escola.com", resultado[1]["email"]);
        }

        [TestMethod]
        public void LerCsv_ComPontoEVirgula_DetectaSeparadorAutomaticamente()
        {
            File.WriteAllText(_arquivoTemporario, "matricula;nome;email\n123;Fulano;fulano@escola.com\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Fulano", resultado[0]["nome"]);
        }

        [TestMethod]
        public void LerCsv_ComLinhaComMenosColunasQueOCabecalho_PreencheComVazio()
        {
            // Consistência: uma linha "quebrada" (menos campos que o cabeçalho) não
            // deve estourar um IndexOutOfRangeException — os campos faltantes viram
            // string vazia, para a validação de negócio (não a leitura do arquivo)
            // decidir se rejeita a linha.
            File.WriteAllText(_arquivoTemporario, "matricula,nome,email\n123,Fulano\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("", resultado[0]["email"]);
        }

        [TestMethod]
        public void LerCsv_ComSeparadorDentroDeCampoEntreAspas_PreservaCampoCompleto()
        {
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\n123,\"Silva, João\",joao@escola.com\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual("Silva, João", resultado[0]["nome"]);
        }

        [TestMethod]
        public void LerCsv_ComAspasEscapadas_ConverteAspasDuplicadas()
        {
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\n123,\"João \"\"Joca\"\" Silva\",joao@escola.com\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual("João \"Joca\" Silva", resultado[0]["nome"]);
        }

        [TestMethod]
        public void LerCsv_ComCampoEntreAspasEmMaisDeUmaLinha_PreservaQuebraDeLinha()
        {
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\n123,\"João\nSilva\",joao@escola.com\n");

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual("João\nSilva", resultado[0]["nome"]);
        }

        [TestMethod]
        public void LerCsv_ComMaisValoresQueCabecalhos_DeveLancarInvalidDataException()
        {
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\n123,João,joao@escola.com,valor-excedente\n");

            Assert.ThrowsException<InvalidDataException>(() => ArquivoService.LerCsv(_arquivoTemporario));
        }

        [TestMethod]
        public void LerCsv_ComAspasNaoFechadas_DeveLancarInvalidDataException()
        {
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\n123,\"João Silva,joao@escola.com\n");

            Assert.ThrowsException<InvalidDataException>(() => ArquivoService.LerCsv(_arquivoTemporario));
        }

        [TestMethod]
        public void LerCsv_ComEncodingAnsiDoExcel_LeAcentosCorretamente()
        {
            // O Excel em português exporta CSV em ANSI (Windows-1252) e sem BOM.
            // Os acentos ficam como um único byte (ex.: 'é' = 0xE9), que não é
            // UTF-8 válido — o leitor deve recorrer ao fallback ANSI em vez de
            // corromper o nome gravado no banco.
            byte[] bytesAnsi = Encoding.GetEncoding(1252).GetBytes(
                "matricula,nome,email\r\n2026901,José Aparecido,jose.aparecido@escola.com\r\n");
            File.WriteAllBytes(_arquivoTemporario, bytesAnsi);

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("José Aparecido", resultado[0]["nome"]);
        }

        [TestMethod]
        public void LerCsv_ComUtf8SemBom_LeAcentosCorretamente()
        {
            // Arquivos UTF-8 sem BOM continuam sendo lidos normalmente.
            File.WriteAllText(_arquivoTemporario,
                "matricula,nome,email\r\n2026902,Éder Müller,eder.muller@escola.com\r\n",
                new UTF8Encoding(false));

            var resultado = ArquivoService.LerCsv(_arquivoTemporario);

            Assert.AreEqual(1, resultado.Count);
            Assert.AreEqual("Éder Müller", resultado[0]["nome"]);
        }
    }
}
