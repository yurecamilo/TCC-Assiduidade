using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.Modelos.Relatorios;
using TCC_Assiduidade.Modelos.Banco;

namespace TCC_Assiduidade.Repositories
{
    public class AusenciaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;
        public void Adicionar(int aulaId, List<string> alunosMatricula)
        {
            // Defesa extra: sem linhas, o INSERT montado abaixo ficaria sem valores e
            // geraria um erro de sintaxe SQL em vez de simplesmente não fazer nada.
            if (alunosMatricula == null || alunosMatricula.Count == 0) return;

            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "as ausências");
                }

                StringBuilder sql = new StringBuilder("INSERT INTO Ausencia (AulaId, AlunoMatricula) VALUES ");
                List<string> rows = new List<string>();

                try
                {
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
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "as ausências");
                }
            }
        }


        public List<RelatorioAusente> ObterAusentesPorAula(int aulaId)
        {
            var lista = new List<RelatorioAusente>();
            // Mudamos o alias de 'Turma.Nome AS Turma' para 'Turma.Nome AS NomeTurma' para evitar conflito com o nome da tabela
            const string consulta = @"
        SELECT 
            Aula.Id AS AulaId,
            Aula.Data,
            Turma.Nome AS NomeTurma,
            {0} AS StatusTurma,
            Aluno.Matricula,
            Aluno.Nome AS Aluno,
            Aluno.Email
        FROM Ausencia
        INNER JOIN Aula ON Ausencia.AulaId = Aula.Id
        INNER JOIN Aluno ON Ausencia.AlunoMatricula = Aluno.Matricula
        INNER JOIN Turma ON Aula.TurmaId = Turma.Id
        WHERE Aula.Id = @aulaId
          AND EXISTS (
              SELECT 1 FROM VinculoTurmaAluno v
              WHERE v.AlunoMatricula = Aluno.Matricula
                AND v.TurmaId = Aula.TurmaId
                AND v.DataEntrada <= Aula.Data
                AND v.DataEntrada = (
                    SELECT MAX(v2.DataEntrada) FROM VinculoTurmaAluno v2
                    WHERE v2.AlunoMatricula = v.AlunoMatricula
                      AND v2.DataEntrada <= Aula.Data
                )
          )";

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = string.Format(consulta, TurmaRepository.ObterColunaStatus(conn, "Turma"));
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
                                    Turma = Turma.FormatarNome(reader["NomeTurma"].ToString(), (EStatusTurma)Convert.ToInt32(reader["StatusTurma"])),
                                    Matricula = reader["Matricula"].ToString(),
                                    Aluno = reader["Aluno"].ToString(),
                                    Email = reader["Email"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "as ausências da aula");
            }

            return lista; // Adicionado o return que faltava no final do método
        }

    }
}
