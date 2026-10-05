using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;

namespace TCC_Assiduidade.Repositories
{
    public class TurmaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        // Leituras permanecem compatíveis até a migração; a exclusão fica bloqueada.
        internal static bool PossuiDataExclusao(MySqlConnection conn, MySqlTransaction trans = null)
        {
            const string sql = @"SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Turma'
                  AND COLUMN_NAME = 'DataExclusao'";
            using (var cmd = new MySqlCommand(sql, conn, trans))
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        internal static string ObterColunaDataExclusao(MySqlConnection conn, string alias, MySqlTransaction trans = null)
        {
            return PossuiDataExclusao(conn, trans) ? alias + ".DataExclusao" : "NULL";
        }

        internal static void VerificarTurmaAtiva(MySqlConnection conn, MySqlTransaction trans, int turmaId)
        {
            string coluna = ObterColunaDataExclusao(conn, "t", trans);
            string sql = $"SELECT t.Id FROM Turma t WHERE t.Id = @id AND {coluna} IS NULL FOR UPDATE";
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                cmd.Parameters.AddWithValue("@id", turmaId);
                if (cmd.ExecuteScalar() == null)
                    throw new RegistroNaoEncontradoException("A turma não existe ou foi excluída. Selecione uma turma ativa.");
            }
        }

        /// <summary>
        /// Abre a conexão traduzindo qualquer falha (servidor fora do ar, sem rede,
        /// credenciais inválidas) em <see cref="ConexaoBancoDadosException"/>, com
        /// uma mensagem específica em vez do erro técnico bruto do driver MySQL.
        /// </summary>
        private static void AbrirConexao(MySqlConnection conn, string contexto)
        {
            try
            {
                conn.Open();
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, contexto);
            }
        }

        public void Adicionar(string turmaNome)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a turma");

                string query = @"
                    INSERT INTO Turma (Nome) VALUES (@nome);";

                try
                {
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", turmaNome);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "a turma");
                }
            }
        }

        public Turma ObterTurmaPorNome(string turmaNome)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string coluna = ObterColunaDataExclusao(conn, "t");
                    string query = $"SELECT t.Id, t.Nome, {coluna} AS DataExclusao FROM Turma t WHERE t.Nome = @nome LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", turmaNome);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Turma
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nome = reader["Nome"].ToString(),
                                    DataExclusao = reader["DataExclusao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataExclusao"]) : null
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "a turma");
            }

            return null;
        }

        public Turma ObterTurmaPorId(int turmaId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string coluna = ObterColunaDataExclusao(conn, "t");
                    string query = $"SELECT t.Id, t.Nome, {coluna} AS DataExclusao FROM Turma t WHERE t.Id = @id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", turmaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Turma
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nome = reader["Nome"].ToString(),
                                    DataExclusao = reader["DataExclusao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataExclusao"]) : null
                                };
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "a turma");
            }

            return null;
        }

        public List<Turma> ObterTurmas()
        {
            var turmas = new List<Turma>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string coluna = ObterColunaDataExclusao(conn, "t");
                    string query = $"SELECT t.Id, t.Nome FROM Turma t WHERE {coluna} IS NULL ORDER BY t.Nome";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            turmas.Add(new Turma
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "as turmas");
            }

            return turmas;
        }

        public List<TurmaExibicaoDTO> ObterTurmasComContagem(bool incluirExcluidas = false)
        {
            var lista = new List<TurmaExibicaoDTO>();

            // QUERY CORRIGIDA: Agora vincula com a tabela Matricula filtrando pela turma ativa (MAX DataEntrada)
            const string consulta = @"
                SELECT t.Id, t.Nome, {0} AS DataExclusao, COUNT(v.AlunoMatricula) AS QuantidadeAlunos
                FROM Turma t
                LEFT JOIN VinculoTurmaAluno v ON v.TurmaId = t.Id AND v.DataEntrada = (
                    SELECT MAX(subM.DataEntrada)
                    FROM VinculoTurmaAluno subM
                    WHERE subM.AlunoMatricula = v.AlunoMatricula
                )
                WHERE {1}
                GROUP BY t.Id, t.Nome, DataExclusao
                ORDER BY t.Nome;";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string coluna = ObterColunaDataExclusao(conn, "t");
                    string sql = string.Format(consulta, coluna, incluirExcluidas ? "1 = 1" : coluna + " IS NULL");
                    using (var cmd = new MySqlCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new TurmaExibicaoDTO
                            {
                                TurmaOriginal = new Turma
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nome = reader["Nome"].ToString(),
                                    DataExclusao = reader["DataExclusao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataExclusao"]) : null
                                },
                                Nome = Turma.FormatarNome(reader["Nome"].ToString(), reader["DataExclusao"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataExclusao"]) : null),
                                QuantidadeAlunos = Convert.ToInt32(reader["QuantidadeAlunos"])
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os dados das turmas");
            }

            return lista;
        }

        public void Atualizar (int turmaId, string novoNome)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a turma");

                string coluna = ObterColunaDataExclusao(conn, "Turma");
                string query = $"UPDATE Turma SET Nome = @nome WHERE Id = @id AND {coluna} IS NULL";

                try
                {
                    int linhasAfetadas;
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", novoNome);
                        cmd.Parameters.AddWithValue("@id", turmaId);
                        linhasAfetadas = cmd.ExecuteNonQuery();
                    }

                    if (linhasAfetadas == 0)
                        throw new RegistroNaoEncontradoException($"A turma de código {turmaId} não foi encontrada. Ela pode já ter sido excluída por outro usuário.");
                }
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "a turma");
                }
            }
        }

        public void Excluir(int turmaId)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a turma");
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        ValidarEsquemaExclusao(PossuiDataExclusao(conn, transaction));
                        const string sql = "UPDATE Turma SET DataExclusao = UTC_TIMESTAMP() WHERE Id = @id AND DataExclusao IS NULL";
                        using (var cmd = new MySqlCommand(sql, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", turmaId);
                            if (cmd.ExecuteNonQuery() == 0)
                                throw new RegistroNaoEncontradoException("A turma não existe ou já foi excluída.");
                        }
                        transaction.Commit();
                    }
                    catch (MySqlException ex)
                    {
                        transaction.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "a turma");
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        internal static void ValidarEsquemaExclusao(bool possuiDataExclusao)
        {
            if (!possuiDataExclusao)
                throw new ArgumentException("A exclusão segura de turmas requer a migração 001_exclusao_logica_turma.sql. Solicite sua aplicação ao responsável pelo banco. Nenhum dado foi excluído.");
        }
    }
}
