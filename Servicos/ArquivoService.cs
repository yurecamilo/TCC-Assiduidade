using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TCC_Assiduidade.Servicos
{
    public static class ArquivoService
    {
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
            {
                throw new ArgumentException("O caminho do arquivo não pode ser nulo ou vazio.", nameof(caminho));
            }

            if (!File.Exists(caminho))
            {
                throw new FileNotFoundException($"O arquivo '{caminho}' não foi encontrado.", caminho);
            }

            var dados = new List<Dictionary<string, string>>();

            using (var reader = new StreamReader(caminho))
            {
                string headerLine = reader.ReadLine();

                if (string.IsNullOrWhiteSpace(headerLine))
                    return dados;

                if (headerLine.Contains(";"))
                    separador = ';';

                string[] headers = headerLine.Split(separador);

                var colunasVerificadas = new HashSet<string>();

                for (int i = 0; i < headers.Length; i++)
                {
                    headers[i] = headers[i].Trim().ToLower();

                    if (string.IsNullOrWhiteSpace(headers[i]))
                    {
                        throw new System.IO.InvalidDataException($"O cabeçalho contém uma coluna vazia na posição {i + 1}.");
                    }

                    if (!colunasVerificadas.Add(headers[i]))
                    {
                        throw new System.IO.InvalidDataException($"O arquivo CSV contém colunas duplicadas no cabeçalho: '{headers[i]}'.");
                    }
                }

                while (!reader.EndOfStream)
                {
                    string linha = reader.ReadLine();

                    if (string.IsNullOrWhiteSpace(linha))
                        continue;

                    string[] valores = linha.Split(separador);

                    var registro = new Dictionary<string, string>();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        string valor = i < valores.Length ? valores[i].Trim() : "";

                        registro[headers[i]] = valor;
                    }

                    dados.Add(registro);
                }
            }

            return dados;
        }
    }
}