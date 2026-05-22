using MySql.Data.MySqlClient;
using System;
using System.Configuration;

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
    }
}
