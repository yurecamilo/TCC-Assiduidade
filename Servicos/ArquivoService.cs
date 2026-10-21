using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TCC_Assiduidade.Servicos
{
    public static class ArquivoService
    {
        public static string SalvarHtmlEAbrir(string html, string nomeArquivoPadrao)
        {
            if (string.IsNullOrWhiteSpace(html))
                throw new ArgumentException("O conteúdo HTML não pode ser vazio.", nameof(html));

            var dialog = new SaveFileDialog
            {
                Title = "Salvar relatório HTML",
                Filter = "Arquivos HTML (*.html)|*.html|Todos os arquivos (*.*)|*.*",
                FileName = SanitizarNomeArquivo(nomeArquivoPadrao)
            };

            if (dialog.ShowDialog() != true)
                return null;

            File.WriteAllText(dialog.FileName, html, Encoding.UTF8);
            Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            return dialog.FileName;
        }

        /// <summary>
        /// Lê um arquivo CSV de forma genérica e retorna os dados como uma lista de dicionários.
        /// Cada linha do CSV vira um Dictionary onde:
        /// - chave = nome da coluna (do cabeçalho)
        /// - valor = valor da célula
        /// </summary>
        /// <param name="caminho">Caminho completo do arquivo CSV</param>
        /// <param name="separador">Separador de colunas (padrão: vírgula)</param>
        /// <returns>Lista de registros do CSV</returns>
        public static List<Dictionary<string, string>> LerCsv(string caminho, char separador = ',')
        {
            if (string.IsNullOrWhiteSpace(caminho))
                throw new ArgumentException("O caminho do arquivo não pode ser nulo ou vazio.", nameof(caminho));

            if (!File.Exists(caminho))
                throw new FileNotFoundException($"O arquivo '{caminho}' não foi encontrado.", caminho);

            var dados = new List<Dictionary<string, string>>();
            string conteudo = File.ReadAllText(caminho);
            if (string.IsNullOrWhiteSpace(conteudo))
                return dados;

            separador = DetectarSeparador(conteudo, separador);
            List<List<string>> linhas = AnalisarCsv(conteudo, separador);
            if (linhas.Count == 0)
                return dados;

            string[] headers = linhas[0].ConvertAll(valor => valor.Trim().ToLowerInvariant()).ToArray();
            var colunasVerificadas = new HashSet<string>();
            for (int i = 0; i < headers.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(headers[i]))
                    throw new InvalidDataException($"O cabeçalho contém uma coluna vazia na posição {i + 1}.");

                if (!colunasVerificadas.Add(headers[i]))
                    throw new InvalidDataException($"O arquivo CSV contém colunas duplicadas no cabeçalho: '{headers[i]}'.");
            }

            for (int linhaIndice = 1; linhaIndice < linhas.Count; linhaIndice++)
            {
                List<string> valores = linhas[linhaIndice];
                if (valores.Count == 1 && string.IsNullOrWhiteSpace(valores[0]))
                    continue;
                if (valores.Count > headers.Length)
                    throw new InvalidDataException($"A linha {linhaIndice + 1} contém mais colunas que o cabeçalho.");

                var registro = new Dictionary<string, string>();
                for (int i = 0; i < headers.Length; i++)
                    registro[headers[i]] = i < valores.Count ? valores[i].Trim() : string.Empty;

                dados.Add(registro);
            }

            return dados;
        }

        private static char DetectarSeparador(string conteudo, char separadorPadrao)
        {
            int virgulas = ContarSeparadoresNoCabecalho(conteudo, ',');
            int pontosEVirgulas = ContarSeparadoresNoCabecalho(conteudo, ';');
            return pontosEVirgulas > virgulas ? ';' : separadorPadrao;
        }

        private static int ContarSeparadoresNoCabecalho(string conteudo, char separador)
        {
            int quantidade = 0;
            bool entreAspas = false;
            for (int i = 0; i < conteudo.Length; i++)
            {
                char atual = conteudo[i];
                if (atual == '"')
                {
                    if (entreAspas && i + 1 < conteudo.Length && conteudo[i + 1] == '"')
                        i++;
                    else
                        entreAspas = !entreAspas;
                }
                else if (!entreAspas && atual == separador)
                    quantidade++;
                else if (!entreAspas && (atual == '\r' || atual == '\n'))
                    break;
            }
            return quantidade;
        }

        private static List<List<string>> AnalisarCsv(string conteudo, char separador)
        {
            var linhas = new List<List<string>>();
            var linha = new List<string>();
            var campo = new StringBuilder();
            bool entreAspas = false;

            for (int i = 0; i < conteudo.Length; i++)
            {
                char atual = conteudo[i];
                if (atual == '"')
                {
                    if (entreAspas && i + 1 < conteudo.Length && conteudo[i + 1] == '"')
                    {
                        campo.Append('"');
                        i++;
                    }
                    else
                        entreAspas = !entreAspas;
                }
                else if (!entreAspas && atual == separador)
                {
                    linha.Add(campo.ToString());
                    campo.Clear();
                }
                else if (!entreAspas && (atual == '\r' || atual == '\n'))
                {
                    if (atual == '\r' && i + 1 < conteudo.Length && conteudo[i + 1] == '\n')
                        i++;
                    linha.Add(campo.ToString());
                    campo.Clear();
                    linhas.Add(linha);
                    linha = new List<string>();
                }
                else
                {
                    if (entreAspas && atual == '\r' && i + 1 < conteudo.Length && conteudo[i + 1] == '\n')
                    {
                        campo.Append('\n');
                        i++;
                    }
                    else
                        campo.Append(atual);
                }
            }

            if (entreAspas)
                throw new InvalidDataException("O arquivo CSV contém um campo com aspas não fechadas.");

            if (campo.Length > 0 || linha.Count > 0)
            {
                linha.Add(campo.ToString());
                linhas.Add(linha);
            }

            return linhas;
        }

        private static string SanitizarNomeArquivo(string nomeArquivo)
        {
            string nome = string.IsNullOrWhiteSpace(nomeArquivo) ? "Relatorio.html" : nomeArquivo;
            foreach (char caractere in Path.GetInvalidFileNameChars())
                nome = nome.Replace(caractere, '_');

            return nome.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                ? nome
                : nome + ".html";
        }
    }
}
