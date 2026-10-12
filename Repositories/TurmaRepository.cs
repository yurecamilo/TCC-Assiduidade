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

        internal static bool PossuiColunaStatus(MySqlConnection conn, MySqlTransaction trans = null)
        {
            const string sql = @"SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Turma'
                  AND COLUMN_NAME = 'Status'";
            using (var cmd = new MySqlCommand(sql, conn, trans))
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        internal static string ObterColunaStatus(MySqlConnection conn, string alias, MySqlTransaction trans = null)
        {
            ValidarEsquemaStatus(PossuiColunaStatus(conn, trans));
            return alias + ".Status";
        }

        internal static string ObterColunaDataStatus(MySqlConnection conn, string alias, MySqlTransaction trans = null)
        {
            const string sql = @"SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Turma'
                  AND COLUMN_NAME = 'DataAlteracaoStatus'";
            using (var cmd = new MySqlCommand(sql, conn, trans))
            {
                if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                    throw new ArgumentException("O banco precisa da migração 003_status_turmas.sql para consultar a data do status. Nenhum dado foi alterado.");
            }
            return alias + ".DataAlteracaoStatus";
        }

        internal static void VerificarTurmaAtiva(MySqlConnection conn, MySqlTransaction trans, int turmaId)
        {
            string coluna = ObterColunaStatus(conn, "t", trans);
            string sql = $"SELECT t.Id FROM Turma t WHERE t.Id = @id AND {coluna} = 1 FOR UPDATE";
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

                    string status = ObterColunaStatus(conn, "t");
                    string data = ObterColunaDataStatus(conn, "t");
                    string query = $"SELECT t.Id, t.Nome, {status} AS Status, {data} AS DataAlteracaoStatus FROM Turma t WHERE t.Nome = @nome LIMIT 1";

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
                                    Status = (EStatusTurma)Convert.ToInt32(reader["Status"]),
                                    DataAlteracaoStatus = reader["DataAlteracaoStatus"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataAlteracaoStatus"]) : null
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
                    string status = ObterColunaStatus(conn, "t");
                    string data = ObterColunaDataStatus(conn, "t");
                    string query = $"SELECT t.Id, t.Nome, {status} AS Status, {data} AS DataAlteracaoStatus FROM Turma t WHERE t.Id = @id";

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
                                    Status = (EStatusTurma)Convert.ToInt32(reader["Status"]),
                                    DataAlteracaoStatus = reader["DataAlteracaoStatus"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataAlteracaoStatus"]) : null
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

                    string coluna = ObterColunaStatus(conn, "t");
                    string data = ObterColunaDataStatus(conn, "t");
                    string query = $"SELECT t.Id, t.Nome, {coluna} AS Status, {data} AS DataAlteracaoStatus FROM Turma t WHERE {coluna} = 1 ORDER BY t.Nome";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            turmas.Add(new Turma
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString(),
                                Status = (EStatusTurma)Convert.ToInt32(reader["Status"]),
                                DataAlteracaoStatus = reader["DataAlteracaoStatus"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataAlteracaoStatus"]) : null
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

        public List<TurmaExibicaoDTO> ObterTurmasComContagem(bool incluirArquivadas = false)
        {
            var lista = new List<TurmaExibicaoDTO>();

            // QUERY CORRIGIDA: Agora vincula com a tabela Matricula filtrando pela turma ativa (MAX DataEntrada)
            const string consulta = @"
                SELECT t.Id, t.Nome, {0} AS Status, {1} AS DataAlteracaoStatus, COUNT(v.AlunoMatricula) AS QuantidadeAlunos
                FROM Turma t
                LEFT JOIN VinculoTurmaAluno v ON v.TurmaId = t.Id
                WHERE {2}
                GROUP BY t.Id, t.Nome, Status, DataAlteracaoStatus
                ORDER BY t.Nome;";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string status = ObterColunaStatus(conn, "t");
                    string data = ObterColunaDataStatus(conn, "t");
                    string filtro = incluirArquivadas ? status + " <> 3" : status + " = 1";
                    string sql = string.Format(consulta, status, data, filtro);
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
                                    Status = (EStatusTurma)Convert.ToInt32(reader["Status"]),
                                    DataAlteracaoStatus = reader["DataAlteracaoStatus"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DataAlteracaoStatus"]) : null
                                },
                                Nome = Turma.FormatarNome(reader["Nome"].ToString(), (EStatusTurma)Convert.ToInt32(reader["Status"])),
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

                string coluna = ObterColunaStatus(conn, "Turma");
                string query = $"UPDATE Turma SET Nome = @nome WHERE Id = @id AND {coluna} = 1";

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
                        ValidarEsquemaStatus(PossuiColunaStatus(conn, transaction));
                        AlterarStatusDentroDaTransacao(conn, transaction, turmaId, EStatusTurma.Ativa, EStatusTurma.Excluida);
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

        public void AlterarStatus(int turmaId, EStatusTurma statusEsperado, EStatusTurma novoStatus)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a turma");
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        ValidarEsquemaStatus(PossuiColunaStatus(conn, tx));
                        AlterarStatusDentroDaTransacao(conn, tx, turmaId, statusEsperado, novoStatus);
                        tx.Commit();
                    }
                    catch (MySqlException ex) { tx.Rollback(); throw MySqlExceptionTranslator.Traduzir(ex, "a turma"); }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        public void AlterarStatusEmLote(List<Tuple<int, EStatusTurma, EStatusTurma>> alteracoes)
        {
            if (alteracoes == null || alteracoes.Count == 0) return;
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "as turmas");
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        ValidarEsquemaStatus(PossuiColunaStatus(conn, tx));
                        foreach (var alteracao in alteracoes)
                            AlterarStatusDentroDaTransacao(conn, tx, alteracao.Item1, alteracao.Item2, alteracao.Item3);
                        tx.Commit();
                    }
                    catch (MySqlException ex) { tx.Rollback(); throw MySqlExceptionTranslator.Traduzir(ex, "as turmas"); }
                    catch { tx.Rollback(); throw; }
                }
            }
        }

        private static void AlterarStatusDentroDaTransacao(MySqlConnection conn, MySqlTransaction tx, int turmaId, EStatusTurma esperado, EStatusTurma novo)
        {
            if (esperado == EStatusTurma.Arquivada && novo == EStatusTurma.Ativa)
            {
                const string sqlConflito = @"SELECT vinculo.AlunoMatricula FROM VinculoTurmaAluno vinculo
                    INNER JOIN VinculoTurmaAluno outro ON outro.AlunoMatricula = vinculo.AlunoMatricula
                    INNER JOIN Turma ativa ON ativa.Id = outro.TurmaId AND ativa.Status = 1
                    WHERE vinculo.TurmaId = @id AND outro.TurmaId <> @id
                    LIMIT 1 FOR UPDATE";
                using (var conflito = new MySqlCommand(sqlConflito, conn, tx))
                {
                    conflito.Parameters.AddWithValue("@id", turmaId);
                    object matricula = conflito.ExecuteScalar();
                    if (matricula != null)
                        throw new ArgumentException("A turma não pode ser desarquivada porque há aluno(s) associado(s) a outra turma ativa. Arquive a outra turma antes de continuar.");
                }
            }
            const string sql = "UPDATE Turma SET Status = @novo, DataAlteracaoStatus = UTC_TIMESTAMP() WHERE Id = @id AND Status = @esperado";
            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@novo", (int)novo);
                cmd.Parameters.AddWithValue("@id", turmaId);
                cmd.Parameters.AddWithValue("@esperado", (int)esperado);
                if (cmd.ExecuteNonQuery() != 1)
                    throw new RegistroNaoEncontradoException("A turma não existe ou seu status foi alterado por outro usuário. Atualize a lista e tente novamente.");
            }
        }

        internal static void ValidarEsquemaStatus(bool possuiStatus)
        {
            if (!possuiStatus)
                throw new ArgumentException("A operação requer a migração 003_status_turmas.sql. Solicite sua aplicação ao responsável pelo banco. Nenhum dado foi alterado.");
        }
    }
}
