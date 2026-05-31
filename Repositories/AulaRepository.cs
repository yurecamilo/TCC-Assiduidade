using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Collections.Generic;
using TCC_Assiduidade.Modelos;

namespace TCC_Assiduidade.Repositories
{
    public class AulaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;
        public int Adicionar(DateTime data, int turmaId)
        {
            int aulaId;
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                        INSERT INTO Aula (Data, TurmaId)
                        VALUES (@data, @turmaId);
                        SELECT LAST_INSERT_ID();
                    ";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@data", data.Date);
                    cmd.Parameters.AddWithValue("@turmaId", turmaId);

                    aulaId = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            return aulaId > 0 ? aulaId : -1;
        }

        public List<AulaExibicaoDTO> ObterResumoAulas()
        {
            var aulas = new List<AulaExibicaoDTO>();

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT
                        Aula.Id AS AulaId,
                        Aula.Data,
                        Turma.Nome AS Turma,
                        COUNT(Ausencia.AlunoMatricula) AS NumeroAusentes
                    FROM Aula
                    INNER JOIN Turma ON Turma.Id = Aula.TurmaId
                    LEFT JOIN Ausencia ON Ausencia.AulaId = Aula.Id
                    GROUP BY Aula.Id, Aula.Data, Turma.Nome
                    ORDER BY Aula.Data DESC, Aula.Id DESC;
                ";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        aulas.Add(new AulaExibicaoDTO
                        {
                            AulaId = Convert.ToInt32(reader["AulaId"]),
                            Data = Convert.ToDateTime(reader["Data"]),
                            Turma = reader["Turma"].ToString(),
                            NumeroAusentes = Convert.ToInt32(reader["NumeroAusentes"])
                        });
                    }
                }
            }

            return aulas;
        }
    }
}
