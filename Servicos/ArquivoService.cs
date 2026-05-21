using System.Collections.Generic;
using System.IO;

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
            // Lista final que será retornada
            var dados = new List<Dictionary<string, string>>();

            // Abre o arquivo para leitura
            using (var reader = new StreamReader(caminho))
            {
                // =========================
                // 1. LEITURA DO CABEÇALHO
                // =========================

                // Primeira linha do arquivo (nomes das colunas)
                string headerLine = reader.ReadLine();

                // Se o arquivo estiver vazio, retorna lista vazia
                if (string.IsNullOrWhiteSpace(headerLine))
                    return dados;

                // Detecta automaticamente o separador se necessário
                if (headerLine.Contains(";"))
                    separador = ';';

                // Divide o cabeçalho em colunas
                string[] headers = headerLine.Split(separador);

                // Normaliza os nomes das colunas (remove espaços e deixa minúsculo)
                for (int i = 0; i < headers.Length; i++)
                {
                    headers[i] = headers[i].Trim().ToLower();
                }

                // =========================
                // 2. LEITURA DAS LINHAS
                // =========================

                // Continua lendo até o final do arquivo
                while (!reader.EndOfStream)
                {
                    string linha = reader.ReadLine();

                    // Ignora linhas vazias
                    if (string.IsNullOrWhiteSpace(linha))
                        continue;

                    // Divide a linha em valores
                    string[] valores = linha.Split(separador);

                    // Cria um dicionário para representar a linha
                    var registro = new Dictionary<string, string>();

                    // Percorre todas as colunas do cabeçalho
                    for (int i = 0; i < headers.Length; i++)
                    {
                        // Se não existir valor naquela posição, usa vazio
                        string valor = i < valores.Length ? valores[i].Trim() : "";

                        // Associa coluna → valor
                        registro[headers[i]] = valor;
                    }

                    // Adiciona o registro à lista final
                    dados.Add(registro);
                }
            }

            // Retorna todos os dados lidos
            return dados;
        }
    }
}