using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using TCC_Assiduidade.Modelos.Dashboard;

namespace TCC_Assiduidade.Repositories
{
    public class DashboardRepository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public List<RegistroFrequenciaDashboard> ObterRegistrosFrequencia()
        {
            var registros = new List<RegistroFrequenciaDashboard>();

            const string sql = @"
                SELECT DISTINCT
                    au.Id AS AulaId,
                    au.Data,
                    t.Id AS TurmaId,
                    t.Nome AS NomeTurma,
                    al.Matricula,
                    al.Nome AS NomeAluno,
                    CASE WHEN aus.AlunoMatricula IS NULL THEN 1 ELSE 0 END AS Presente
                FROM Aula au
                INNER JOIN Turma t ON t.Id = au.TurmaId
                INNER JOIN VinculoTurmaAluno v
                    ON v.TurmaId = au.TurmaId
                   AND v.DataEntrada <= au.Data
                   AND v.DataEntrada = (
                        SELECT MAX(v2.DataEntrada)
                        FROM VinculoTurmaAluno v2
                        WHERE v2.AlunoMatricula = v.AlunoMatricula
                          AND v2.DataEntrada <= au.Data
                   )
                INNER JOIN Aluno al ON al.Matricula = v.AlunoMatricula
                LEFT JOIN Ausencia aus
                    ON aus.AulaId = au.Id
                   AND aus.AlunoMatricula = al.Matricula
                ORDER BY au.Data, au.Id, al.Nome;";

            try
            {
                using (var conexao = new MySqlConnection(connectionString))
                using (var comando = new MySqlCommand(sql, conexao))
                {
                    conexao.Open();
                    using (var leitor = comando.ExecuteReader())
                    {
                        while (leitor.Read())
                        {
                            registros.Add(new RegistroFrequenciaDashboard
                            {
                                AulaId = Convert.ToInt32(leitor["AulaId"]),
                                Data = Convert.ToDateTime(leitor["Data"]),
                                TurmaId = Convert.ToInt32(leitor["TurmaId"]),
                                Turma = leitor["NomeTurma"].ToString(),
                                Matricula = leitor["Matricula"].ToString(),
                                Aluno = leitor["NomeAluno"].ToString(),
                                Presente = Convert.ToInt32(leitor["Presente"]) == 1
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os dados do dashboard");
            }

            return registros;
        }
    }
}
