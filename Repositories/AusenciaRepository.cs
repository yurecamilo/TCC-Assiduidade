using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using TCC_Assiduidade.Modelos;

namespace TCC_Assiduidade.Repositories
{
    public class AusenciaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;
        public void Adicionar(int aulaId, List<string> alunosMatricula)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                StringBuilder sql = new StringBuilder("INSERT INTO Ausencia (AulaId, AlunoMatricula) VALUES ");
                List<string> rows = new List<string>();

                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;

                    for (int i = 0; i < alunosMatricula.Count; i++)
                    {
                        string mRef = "@m" + i;
                        string nRef = "@n" + i;

                        rows.Add($"({mRef}, {nRef})");

                        cmd.Parameters.AddWithValue(mRef, aulaId);
                        cmd.Parameters.AddWithValue(nRef, alunosMatricula[i]);
                    }

                    sql.Append(string.Join(",", rows));

                    cmd.CommandText = sql.ToString();
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public List<RelatorioAusente> ObterAusentesPorAula(int aulaId)
        {
            var lista = new List<RelatorioAusente>();
            // Mudamos o alias de 'Turma.Nome AS Turma' para 'Turma.Nome AS NomeTurma' para evitar conflito com o nome da tabela
            string query = @"
        SELECT 
            Aula.Id AS AulaId,
            Aula.Data,
            Turma.Nome AS NomeTurma,
            Aluno.Matricula,
            Aluno.Nome AS Aluno,
            Aluno.Email
        FROM Ausencia
        INNER JOIN Aula ON Ausencia.AulaId = Aula.Id
        INNER JOIN Aluno ON Ausencia.AlunoMatricula = Aluno.Matricula
        INNER JOIN Turma ON Aula.TurmaId = Turma.Id
        WHERE Aula.Id = @aulaId";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@aulaId", aulaId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new RelatorioAusente
                            {
                                AulaId = Convert.ToInt32(reader["AulaId"]),
                                Data = Convert.ToDateTime(reader["Data"]),
                                Turma = reader["NomeTurma"].ToString(), // Lendo o alias correto agora!
                                Matricula = reader["Matricula"].ToString(),
                                Aluno = reader["Aluno"].ToString(),
                                Email = reader["Email"].ToString()
                            });
                        }
                    }
                }
            }
            return lista; // Adicionado o return que faltava no final do método
        }

    }
}
