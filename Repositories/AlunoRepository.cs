using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using TCC_Assiduidade.Modelos;

namespace TCC_Assiduidade.Repositories
{
    public class AlunoRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public void Adicionar(Aluno aluno)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = "INSERT INTO Aluno (Matricula, Nome, Email, TurmaId) VALUES (@matricula, @nome, @email, @turmaId)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@matricula", aluno.Matricula);
                    cmd.Parameters.AddWithValue("@nome", aluno.Nome);
                    cmd.Parameters.AddWithValue("@email", aluno.Email ?? "");
                    cmd.Parameters.AddWithValue("@turmaId", aluno.TurmaId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Adicionar(List<Aluno> alunos)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        StringBuilder sql = new StringBuilder("INSERT INTO Aluno (Matricula, Nome, Email, TurmaId) VALUES ");
                        List<string> rows = new List<string>();

                        using (var cmd = new MySqlCommand())
                        {
                            cmd.Connection = conn;
                            cmd.Transaction = trans;

                            for (int i = 0; i < alunos.Count; i++)
                            {
                                string mRef = "@m" + i;
                                string nRef = "@n" + i;
                                string eRef = "@e" + i;
                                string tRef = "@t" + i;

                                rows.Add($"({mRef}, {nRef}, {eRef}, {tRef})");

                                cmd.Parameters.AddWithValue(mRef, alunos[i].Matricula);
                                cmd.Parameters.AddWithValue(nRef, alunos[i].Nome);
                                cmd.Parameters.AddWithValue(eRef, alunos[i].Email ?? "");
                                cmd.Parameters.AddWithValue(tRef, alunos[i].TurmaId);
                            }

                            sql.Append(string.Join(",", rows));

                            cmd.CommandText = sql.ToString();
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }

            }
        }

        public Aluno ObterPorMatricula(string matricula)
        {
            Aluno aluno = new Aluno();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                        SELECT Matricula, Nome, Email, TurmaId
                        FROM Aluno
                        WHERE Matricula = @matricula
                    ";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@matricula", matricula);

                    using (var reader = cmd.ExecuteReader())
                    {
                        aluno.Matricula = reader["Matricula"].ToString();
                        aluno.Nome = reader["Nome"].ToString();
                        aluno.Email = reader["Email"].ToString();
                        aluno.TurmaId = Convert.ToInt32(reader["TurmaId"]);
                    }
                }
            }

            return aluno;
        }

        public List<Aluno> ObterPorTurma(int turmaId)
        {
            var alunos = new List<Aluno>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                        SELECT Matricula, Nome, Email, TurmaId
                        FROM Aluno
                        WHERE TurmaId = @turmaId
                    ";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@turmaId", turmaId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alunos.Add(new Aluno
                            {
                                Matricula = reader["Matricula"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                TurmaId = Convert.ToInt32(reader["TurmaId"])
                            });
                        }
                    }
                }
            }

            return alunos;
        }

        public List<Aluno> ObterTodos()
        {
            var alunos = new List<Aluno>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                string query = "SELECT Matricula, Nome FROM Aluno";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        alunos.Add(new Aluno
                        {
                            Matricula = reader["Matricula"].ToString(),
                            Nome = reader["Nome"].ToString()
                        });
                    }
                }
            }
            return alunos;
        }
    }
}
