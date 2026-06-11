using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using TCC_Assiduidade.Modelos;
using TCC_Assiduidade.Modelos.Relatorios;

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

        public int ContarMatriculasExistentes(List<string> matriculas)
        {
            if (matriculas == null || matriculas.Count == 0) return 0;

            List<string> parametros = new List<string>();
            for (int i = 0; i < matriculas.Count; i++)
            {
                parametros.Add($"@p{i}");
            }

            string sql = $"SELECT COUNT(1) FROM Aluno WHERE Matricula IN ({string.Join(", ", parametros)})";

            using (var conexao = new MySqlConnection(connectionString))
            {
                using (var comando = new MySqlCommand(sql, conexao))
                {
                    for (int i = 0; i < matriculas.Count; i++)
                    {
                        comando.Parameters.AddWithValue($"@p{i}", matriculas[i]);
                    }

                    conexao.Open();

                    int totalExistente = Convert.ToInt32(comando.ExecuteScalar());

                    return totalExistente;
                }
            }
        }

        public int ContarAlunosDeOutraTurma(int turmaId, List<string> matriculas)
        {
            List<string> parametros = new List<string>();
            for (int i = 0; i < matriculas.Count; i++)
            {
                parametros.Add($"@p{i}");
            }

            string sql = $@"SELECT COUNT(1) FROM Aluno 
                            WHERE TurmaId <> @turmaId 
                            AND Matricula IN ({string.Join(", ", parametros)})";

            using (var conexao = new MySqlConnection(connectionString))
            {
                using (var comando = new MySqlCommand(sql, conexao))
                {
                    comando.Parameters.AddWithValue("@turmaId", turmaId);

                    for (int i = 0; i < matriculas.Count; i++)
                    {
                        comando.Parameters.AddWithValue($"@p{i}", matriculas[i]);
                    }

                    conexao.Open();
                    return Convert.ToInt32(comando.ExecuteScalar());
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
                        ORDER BY Nome ASC
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

                string query = "SELECT Matricula, Nome, Email, TurmaId FROM Aluno ORDER BY Nome ASC";

                using (var cmd = new MySqlCommand(query, conn))
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
            return alunos;
        }

        public List<AlunoExibicaoDTO> ObterPerfilAluno()
        {
            var lista = new List<AlunoExibicaoDTO>();

            string sql = @"
                SELECT 
                    a.Matricula, 
                    a.Nome, 
                    a.Email, 
                    COALESCE(t.Nome, 'Sem turma') AS NomeTurma,
                    -- Subqueries ou Joins para calcular a frequência (Exemplo genérico):
                    COALESCE((SELECT COUNT(*) FROM Ausencia aus WHERE aus.AlunoMatricula = a.Matricula), 0) AS TotalFaltas,
                    COALESCE((SELECT COUNT(*) FROM Aula WHERE Aula.TurmaId = a.TurmaId), 0) AS TotalAulas
                FROM Aluno a
                LEFT JOIN Turma t ON a.TurmaId = t.Id
                ORDER BY a.Nome ASC;";

            using (var conn = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int faltas = Convert.ToInt32(reader["TotalFaltas"]);
                            int totalAulas = Convert.ToInt32(reader["TotalAulas"]);

                            double percentualAssiduidade = 100.0; 
                            if (totalAulas > 0)
                            {
                                percentualAssiduidade = ((double)(totalAulas - faltas) / totalAulas) * 100;
                                percentualAssiduidade = Math.Round(percentualAssiduidade, 1);
                            }

                            var alunoDTO = new AlunoExibicaoDTO
                            {
                                Matricula = reader["matricula"].ToString(),
                                Nome = reader["nome"].ToString(),
                                Email = reader["email"].ToString(),
                                Turma = reader["nometurma"].ToString(),

                                DadosFrequencia = new RelatorioFrequencia
                                {
                                    TotalFaltas = faltas,
                                    TotalAulas = totalAulas,
                                    Assiduidade = percentualAssiduidade
                                }
                            };

                            lista.Add(alunoDTO);
                        }
                    }
                }
            }

            return lista;
        }
    }
}
