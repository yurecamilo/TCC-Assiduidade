using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using TCC_Assiduidade.Modelos.Banco;
using TCC_Assiduidade.Modelos.DTO;

namespace TCC_Assiduidade.Repositories
{
    public class TurmaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public void Adicionar(string turmaNome)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                    INSERT INTO Turma (Nome) VALUES (@nome);";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", turmaNome);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Nao foi possivel adicionar a turma.", ex);
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Erro ao acessar o banco de dados ao buscar turma.", ex);
            }

            return null;
        }

        public Turma ObterTurmaPorId(int turmaId)
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
            return null;
        }

        public List<Turma> ObterTurmas()
        {
            var turmas = new List<Turma>();

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

            using (var conn = new MySqlConnection(connectionString))
            {
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

            return lista;
        }

        public void Atualizar (){ }

        public void Excluir() { }
    }
}
