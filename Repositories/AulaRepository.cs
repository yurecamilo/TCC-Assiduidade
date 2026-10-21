using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCC_Assiduidade.Excecoes;
using TCC_Assiduidade.Modelos.DTO;
using TCC_Assiduidade.Modelos.Banco;

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
                try
                {
                    conn.Open();
                }
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "a aula");
                }

                string query = @"
                        INSERT INTO Aula (Data, TurmaId)
                        VALUES (@data, @turmaId);
                        SELECT LAST_INSERT_ID();
                    ";

                try
                {
                    using (var trans = conn.BeginTransaction())
                    {
                        TurmaRepository.VerificarTurmaAtiva(conn, trans, turmaId);

                        // Proteção contra duplicação: a mesma turma não pode ter duas
                        // aulas gravadas com a mesma data.
                        using (var cmdVerificarDuplicada = new MySqlCommand(
                            "SELECT Id FROM Aula WHERE TurmaId = @turmaId AND Data = @data LIMIT 1 FOR UPDATE", conn, trans))
                        {
                            cmdVerificarDuplicada.Parameters.AddWithValue("@turmaId", turmaId);
                            cmdVerificarDuplicada.Parameters.AddWithValue("@data", data.Date);
                            if (cmdVerificarDuplicada.ExecuteScalar() != null)
                                throw new RegistroDuplicadoException(
                                    "Já existe uma aula registrada para esta turma em " + data.ToString("dd/MM/yyyy") + ".");
                        }

                        using (var cmd = new MySqlCommand(query, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@data", data.Date);
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            aulaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        trans.Commit();
                    }
                }
                catch (MySqlException ex)
                {
                    // FK inválida (turma inexistente) também cai aqui, traduzida
                    // para uma mensagem de integridade referencial.
                    throw MySqlExceptionTranslator.Traduzir(ex, "a aula");
                }
            }
            return aulaId > 0 ? aulaId : -1;
        }

        public int AdicionarComAusencias(DateTime data, int turmaId, IReadOnlyList<string> alunosAusentes)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (MySqlException ex)
                {
                    throw MySqlExceptionTranslator.Traduzir(ex, "a aula e suas ausências");
                }

                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        TurmaRepository.VerificarTurmaAtiva(conn, trans, turmaId);
                        int aulaId;
                        const string inserirAula = @"
                            INSERT INTO Aula (Data, TurmaId)
                            VALUES (@data, @turmaId);
                            SELECT LAST_INSERT_ID();";
                        using (var cmd = new MySqlCommand(inserirAula, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@data", data.Date);
                            cmd.Parameters.AddWithValue("@turmaId", turmaId);
                            aulaId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (alunosAusentes.Count > 0)
                        {
                            var sql = new StringBuilder("INSERT INTO Ausencia (AulaId, AlunoMatricula) VALUES ");
                            var valores = new List<string>();
                            using (var cmd = new MySqlCommand { Connection = conn, Transaction = trans })
                            {
                                for (int i = 0; i < alunosAusentes.Count; i++)
                                {
                                    string matriculaParametro = "@matricula" + i;
                                    valores.Add("(@aulaId, " + matriculaParametro + ")");
                                    cmd.Parameters.AddWithValue(matriculaParametro, alunosAusentes[i]);
                                }

                                cmd.Parameters.AddWithValue("@aulaId", aulaId);
                                sql.Append(string.Join(",", valores));
                                cmd.CommandText = sql.ToString();
                                cmd.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                        return aulaId;
                    }
                    catch (MySqlException ex)
                    {
                        trans.Rollback();
                        throw MySqlExceptionTranslator.Traduzir(ex, "a aula e suas ausências");
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<AulaExibicaoDTO> ObterResumoAulas()
        {
            var aulas = new List<AulaExibicaoDTO>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string colunaStatus = TurmaRepository.ObterColunaStatus(conn, "Turma");
                    string query = $@"
                    SELECT
                        Aula.Id AS AulaId,
                        Aula.Data,
                        Aula.TurmaId,
                        Turma.Nome AS Turma,
                        {colunaStatus} AS StatusTurma,
                        COUNT(Ausencia.AlunoMatricula) AS NumeroAusentes
                    FROM Aula
                    INNER JOIN Turma ON Turma.Id = Aula.TurmaId
                    LEFT JOIN Ausencia ON Ausencia.AulaId = Aula.Id
                        AND EXISTS (
                            SELECT 1 FROM VinculoTurmaAluno v
                            WHERE v.AlunoMatricula = Ausencia.AlunoMatricula
                              AND v.TurmaId = Aula.TurmaId
                              AND v.DataEntrada <= Aula.Data
                              AND v.DataEntrada = (
                                  SELECT MAX(v2.DataEntrada) FROM VinculoTurmaAluno v2
                                  WHERE v2.AlunoMatricula = v.AlunoMatricula
                                    AND v2.DataEntrada <= Aula.Data
                              )
                        )
                    WHERE {colunaStatus} <> 3
                    GROUP BY Aula.Id, Aula.Data, Aula.TurmaId, Turma.Nome, StatusTurma
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
                                TurmaId = Convert.ToInt32(reader["TurmaId"]),
                                StatusTurma = (EStatusTurma)Convert.ToInt32(reader["StatusTurma"]),
                                Data = Convert.ToDateTime(reader["Data"]),
                                Turma = Turma.FormatarNome(reader["Turma"].ToString(), (EStatusTurma)Convert.ToInt32(reader["StatusTurma"])),
                                NumeroAusentes = Convert.ToInt32(reader["NumeroAusentes"])
                            });
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                throw MySqlExceptionTranslator.Traduzir(ex, "os dados das aulas");
            }

            return aulas;
        }

    }
}
