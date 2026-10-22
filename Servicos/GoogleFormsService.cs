using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Configuration;

namespace TCC_Assiduidade.Servicos
{
    /// <summary>
    /// Baixa o CSV público da planilha de respostas do Google Forms (recurso
    /// "Publicar na web" da planilha vinculada ao formulário). É a fonte de
    /// dados do botão "Sincronizar Forms" da importação de presença.
    ///
    /// A escolha por download é intencional: o app só faz HTTPS de saída,
    /// não precisa de credenciais do Google (a URL de publicação é anônima)
    /// e não exige nada recebendo conexões na rede da escola.
    /// </summary>
    public static class GoogleFormsService
    {
        private const string ChaveUrlAppConfig = "FormsCsvUrl";
        private const string UrlPublicacaoEsperada = "/spreadsheets/d/e/";

        /// <summary>
        /// URL do CSV publicado salva no App.config. Nula quando não configurada.
        /// </summary>
        public static string ObterUrlPadrao()
        {
            string valor = ConfigurationManager.AppSettings[ChaveUrlAppConfig];
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }

        /// <summary>
        /// Baixa a planilha publicada e devolve as respostas no mesmo formato
        /// usado pelos CSVs locais de presença (lista de dicionários por linha).
        /// A coluna extra "Carimbo de data/hora" do Google é mantida, mas é
        /// ignorada pela validação e gravação da presença.
        /// </summary>
        public static List<Dictionary<string, string>> BaixarRespostas(string url = null)
        {
            url = string.IsNullOrWhiteSpace(url) ? ObterUrlPadrao() : url.Trim();
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("URL da planilha de respostas não configurada. Verifique o item FormsCsvUrl no App.config.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidOperationException("A URL da planilha precisa ser um endereço HTTPS completo.");

            if (!uri.PathAndQuery.Contains(UrlPublicacaoEsperada) || !uri.PathAndQuery.Contains("output=csv"))
                throw new InvalidOperationException("A URL não parece ser um CSV publicado do Google Sheets (deve conter /spreadsheets/d/e/ e output=csv).");

            string conteudo = BaixarTexto(url);
            return ArquivoService.AnalisarConteudoCsv(conteudo);
        }

        private static string BaixarTexto(string url)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            HttpWebRequest requisicao = (HttpWebRequest)WebRequest.Create(url);
            requisicao.Method = "GET";
            requisicao.Timeout = 30000;
            requisicao.ReadWriteTimeout = 30000;
            requisicao.UserAgent = "Mozilla/5.0 (compatible; TCC-Assiduidade)";
            requisicao.AllowAutoRedirect = true;

            try
            {
                using (HttpWebResponse resposta = (HttpWebResponse)requisicao.GetResponse())
                using (MemoryStream memoria = new MemoryStream())
                {
                    using (Stream fluxo = resposta.GetResponseStream())
                        fluxo.CopyTo(memoria);

                    string conteudo = DecodificarResposta(memoria.ToArray());
                    if (conteudo.TrimStart().StartsWith("<", StringComparison.Ordinal))
                        throw new InvalidOperationException("O endereço devolveu uma página web em vez do CSV. Confirme que a planilha continua publicada ('Arquivo > Publicar na web') e refaça a publicação se necessário.");

                    return conteudo;
                }
            }
            catch (WebException ex)
            {
                throw new InvalidOperationException("Não foi possível baixar a planilha de respostas (falha de rede ou link inválido). Verifique a internet e a URL publicada.", ex);
            }
        }

        /// <summary>
        /// Mesma regra de encoding do leitor de arquivos locais: BOM decide
        /// UTF-8/UTF-16; sem BOM, valida UTF-8 estrito e cai para ANSI
        /// (Windows-1252) quando há acentos fora do padrão — evita que nomes
        /// com acento cheguem corrompidos do download público.
        /// </summary>
        private static string DecodificarResposta(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            try
            {
                var utf8Estrito = new UTF8Encoding(false, true);
                return utf8Estrito.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
        }
    }
}
