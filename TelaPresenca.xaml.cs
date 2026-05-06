using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TCC_Assiduidade
{
    /// <summary>
    /// Lógica interna para TelaPresenca.xaml
    /// </summary>
    public partial class TelaPresenca : Window
    {
        string connectionString = "server=switchyard.proxy.rlwy.net;database=railway;user=root;password=ACaRpVAAgmoyXtiEvdYlHtLTISAzUSZS;port=26278";
        public TelaPresenca()
        {
            InitializeComponent();
        }
        private void buttonImportarPresenca_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV de presença"
            };

            if (string.IsNullOrWhiteSpace(tbTurmaId.Text))
            {
                MessageBox.Show("Informe o ID da turma.");
                return;
            }

            if (!int.TryParse(tbTurmaId.Text, out int turmaId))
            {
                MessageBox.Show("O ID da turma deve ser um número inteiro.");
                return;
            }

            DateTime dataAula = DateTime.Now;

            if (dialog.ShowDialog() == true)
            {
                var dadosPresenca = CsvUtils.LerCsv(dialog.FileName);

                bool importou = ImportarPresenca(turmaId, dataAula, dadosPresenca);

                if (importou)
                {
                    MessageBox.Show("Presença importada e ausências registradas com sucesso!");
                }
                else
                {
                    MessageBox.Show("Não foi possível importar a presença.");
                }
            }
        }
        public int CriarAula(DateTime data, int turmaId)
        {
            try
            {
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

                        int aulaId = Convert.ToInt32(cmd.ExecuteScalar());
                        return aulaId;
                    }
                }
            }
            catch (Exception ex)
            {
                tbDados.AppendText($"Erro ao criar aula: {ex.Message}\n");
                return -1;
            }
        }
        public List<Aluno> CarregarAlunosDaTurma(int turmaId)
        {
            var alunos = new List<Aluno>();

            try
            {
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
            }
            catch (Exception ex)
            {
                tbDados.AppendText($"Erro ao carregar alunos da turma: {ex.Message}\n");
            }

            return alunos;
        }

        public bool SalvarAusencia(int aulaId, string alunoMatricula)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                            INSERT INTO Ausencia (AulaId, AlunoMatricula)
                            VALUES (@aulaId, @alunoMatricula)
                        ";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@aulaId", aulaId);
                        cmd.Parameters.AddWithValue("@alunoMatricula", alunoMatricula);

                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                tbDados.AppendText($"Erro ao salvar ausência: {ex.Message}\n");
                return false;
            }
        }

        private bool ImportarPresenca(int turmaId, DateTime dataAula, List<Dictionary<string, string>> dadosPresenca)
        {
            tbDados.Clear();

            if (!TurmaExiste(turmaId))
            {
                tbDados.AppendText("Erro: turma não encontrada.\n");
                return false;
            }

            int aulaId = CriarAula(dataAula, turmaId);

            if (aulaId == -1)
            {
                tbDados.AppendText("Não foi possível criar a aula.\n");
                return false;
            }

            List<Aluno> alunosDaTurma = CarregarAlunosDaTurma(turmaId);

            if (alunosDaTurma.Count == 0)
            {
                tbDados.AppendText("Nenhum aluno cadastrado para essa turma.\n");
                return false;
            }

            HashSet<string> matriculasPresentes = new HashSet<string>();

            foreach (var linha in dadosPresenca)
            {
                if (linha.ContainsKey("matricula_aluno"))
                {
                    string matricula = linha["matricula_aluno"].Trim();
                    matriculasPresentes.Add(matricula);
                }
            }

            int totalAusentes = 0;

            foreach (var aluno in alunosDaTurma)
            {
                if (!matriculasPresentes.Contains(aluno.Matricula))
                {
                    bool salvou = SalvarAusencia(aulaId, aluno.Matricula);

                    if (salvou)
                    {
                        totalAusentes++;
                    }
                }
            }

            tbDados.AppendText("--- IMPORTAÇÃO CONCLUÍDA ---\n\n");
            tbDados.AppendText($"Aula criada: {aulaId}\n");
            tbDados.AppendText($"Data: {dataAula:dd/MM/yyyy}\n");
            tbDados.AppendText($"Total de alunos da turma: {alunosDaTurma.Count}\n");
            tbDados.AppendText($"Total de presentes no CSV: {matriculasPresentes.Count}\n");
            tbDados.AppendText($"Total de ausentes salvos: {totalAusentes}\n\n");

            GerarRelatorioAusentes(aulaId);

            return true;
        }

        public bool TurmaExiste(int turmaId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM Turma WHERE Id = @id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", turmaId);
                        var result = Convert.ToInt32(cmd.ExecuteScalar());
                        return result > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public void GerarRelatorioAusentes(int aulaId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT 
                    Aula.Id AS AulaId,
                    Aula.Data,
                    Turma.Nome AS Turma,
                    Aluno.Matricula,
                    Aluno.Nome AS Aluno,
                    Aluno.Email
                FROM Ausencia
                INNER JOIN Aula ON Ausencia.AulaId = Aula.Id
                INNER JOIN Aluno ON Ausencia.AlunoMatricula = Aluno.Matricula
                INNER JOIN Turma ON Aula.TurmaId = Turma.Id
                WHERE Aula.Id = @aulaId
            ";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@aulaId", aulaId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            tbDados.AppendText("\n--- RELATÓRIO SALVO NO BANCO ---\n\n");

                            while (reader.Read())
                            {
                                tbDados.AppendText(
                                    $"Aula: {reader["AulaId"]} | " +
                                    $"Data: {Convert.ToDateTime(reader["Data"]):dd/MM/yyyy} | " +
                                    $"Turma: {reader["Turma"]} | " +
                                    $"Aluno: {reader["Aluno"]} | " +
                                    $"Matrícula: {reader["Matricula"]} | " +
                                    $"Email: {reader["Email"]}\n"
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tbDados.AppendText($"Erro ao gerar relatório: {ex.Message}\n");
            }
        }
    }

}
