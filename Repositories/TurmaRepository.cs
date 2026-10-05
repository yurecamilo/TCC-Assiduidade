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

                    string query = "SELECT Id, Nome FROM Turma WHERE Nome = @nome LIMIT 1";

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
                                    Nome = reader["Nome"].ToString()
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
                    string query = "SELECT Id, Nome FROM Turma WHERE Id = @id";

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
                                    Nome = reader["Nome"].ToString()
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

                    string query = "SELECT Id, Nome FROM Turma ORDER BY Nome";

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

        public List<TurmaExibicaoDTO> ObterTurmasComContagem()
        {
            var lista = new List<TurmaExibicaoDTO>();

            // QUERY CORRIGIDA: Agora vincula com a tabela Matricula filtrando pela turma ativa (MAX DataEntrada)
            string sql = @"
                SELECT t.Id, t.Nome, COUNT(v.AlunoMatricula) AS QuantidadeAlunos
                FROM Turma t
                LEFT JOIN VinculoTurmaAluno v ON v.TurmaId = t.Id AND v.DataEntrada = (
                    SELECT MAX(subM.DataEntrada)
                    FROM VinculoTurmaAluno subM
                    WHERE subM.AlunoMatricula = v.AlunoMatricula
                )
                GROUP BY t.Id, t.Nome
                ORDER BY t.Nome;";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new TurmaExibicaoDTO
                            {
                                TurmaOriginal = new Turma
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Nome = reader["Nome"].ToString()
                                },
                                Nome = reader["Nome"].ToString(),
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

                string query = @"
                    UPDATE Turma SET Nome = @nome WHERE Id = @id;";

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

        public void Excluir(int turmaId, bool excluirAlunos)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                AbrirConexao(conn, "a turma");

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Guarda os alunos vinculados antes de remover os vínculos. A
                        // tabela temporária permite escolher entre manter os cadastros
                        // sem turma ou excluí-los por completo.
                        string queryCriarTabelaTemporaria = @"
                            CREATE TEMPORARY TABLE AlunosDaTurmaExcluida (
                                AlunoMatricula VARCHAR(50) NOT NULL PRIMARY KEY
                            );";
                        using (var cmd = new MySqlCommand(queryCriarTabelaTemporaria, conn, transaction))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        string queryAlunosDaTurma = @"
                            INSERT INTO AlunosDaTurmaExcluida (AlunoMatricula)
                            SELECT DISTINCT AlunoMatricula
                            FROM VinculoTurmaAluno
                            WHERE TurmaId = @turmaId;";
                        using (var cmd = new MySqlCommand(queryAlunosDaTurma, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            cmd.ExecuteNonQuery();
                        }

                        string queryAusencias = @"
                            DELETE Ausencia FROM Ausencia
                            INNER JOIN Aula ON Ausencia.AulaId = Aula.Id
                            WHERE Aula.TurmaId = @turmaId;";
                        using (var cmd = new MySqlCommand(queryAusencias, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            cmd.ExecuteNonQuery();
                        }

                        string queryAulas = "DELETE FROM Aula WHERE TurmaId = @turmaId;";
                        using (var cmd = new MySqlCommand(queryAulas, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            cmd.ExecuteNonQuery();
                        }

                        if (excluirAlunos)
                        {
                            string queryOutrasAusencias = @"
                                DELETE FROM Ausencia
                                WHERE AlunoMatricula IN
                                    (SELECT AlunoMatricula FROM AlunosDaTurmaExcluida);";
                            using (var cmd = new MySqlCommand(queryOutrasAusencias, conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            string queryTodosVinculos = @"
                                DELETE FROM VinculoTurmaAluno
                                WHERE AlunoMatricula IN
                                    (SELECT AlunoMatricula FROM AlunosDaTurmaExcluida);";
                            using (var cmd = new MySqlCommand(queryTodosVinculos, conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }

                            string queryAlunos = @"
                                DELETE FROM Aluno
                                WHERE Matricula IN
                                    (SELECT AlunoMatricula FROM AlunosDaTurmaExcluida);";
                            using (var cmd = new MySqlCommand(queryAlunos, conn, transaction))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }

                        string queryVinculos = "DELETE FROM VinculoTurmaAluno WHERE TurmaId = @turmaId;";
                        using (var cmd = new MySqlCommand(queryVinculos, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            cmd.ExecuteNonQuery();
                        }

                        string queryTurma = "DELETE FROM Turma WHERE Id = @id;";
                        using (var cmd = new MySqlCommand(queryTurma, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", turmaId);
                            int linhasAfetadas = cmd.ExecuteNonQuery();

                            if (linhasAfetadas == 0)
                                throw new RegistroNaoEncontradoException($"A turma de código {turmaId} não foi encontrada. Ela pode já ter sido excluída por outro usuário.");
                        }

                        transaction.Commit();
                    }
                    catch (RegistroNaoEncontradoException)
                    {
                        transaction.Rollback();
                        throw;
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
    }
}
