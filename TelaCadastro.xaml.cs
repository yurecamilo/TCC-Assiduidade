using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Windows;

namespace TCC_Assiduidade
{
    public partial class TelaCadastro : Window
    {
        string connectionString = ConfigurationManager.ConnectionStrings["MyDbConn"].ConnectionString;

        public TelaCadastro()
        {
            InitializeComponent();
            tbDados.Clear();
            tbTurmaNome.Clear();
        }

        private void Importacao(string turmaNome, List<Dictionary<string, string>> dados)
        {
            tbDados.Clear();

            if (string.IsNullOrWhiteSpace(turmaNome))
            {
                tbDados.AppendText("Erro: informe o nome da turma.\n");
                return;
            }

            Turma turma = BuscarTurmaPorNome(turmaNome);
            bool reescreverTurma = false;

            if (turma == null)
            {
                int turmaId = CriarTurma(turmaNome);

                if (turmaId == -1)
                {
                    tbDados.AppendText($"Erro ao salvar turma {turmaNome}. Verifique os dados da turma.\n");
                    return;
                }

                turma = new Turma { Id = turmaId, Nome = turmaNome };
                tbDados.AppendText($"Turma {turma.Nome} salva com sucesso.\n");
            }
            else
            {
                var resposta = MessageBox.Show(
                    $"A turma {turma.Nome} ja existe. Deseja substituir todos os alunos dessa turma pelos alunos do CSV?",
                    "Turma ja cadastrada",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (resposta != MessageBoxResult.Yes)
                {
                    tbDados.AppendText("Importacao cancelada. Nenhum aluno foi alterado.\n");
                    return;
                }

                reescreverTurma = true;
            }

            List<Aluno> alunosParaSalvar = new List<Aluno>();

            foreach (var linha in dados)
            {
                var aluno = new Aluno
                {
                    Matricula = linha["matricula"],
                    Nome = linha["nome"],
                    Email = linha.ContainsKey("email") ? linha["email"] : "",
                    TurmaId = turma.Id
                };

                alunosParaSalvar.Add(aluno);
            }

            bool salvou = reescreverTurma
                ? ReescreverAlunosDaTurma(turma.Id, alunosParaSalvar)
                : SalvarAlunos(alunosParaSalvar);

            tbDados.Clear();

            if (salvou)
            {
                tbDados.AppendText("Importacao realizada com sucesso!\n");
                tbDados.AppendText($"Turma: {turma.Nome}\n");

                if (reescreverTurma)
                {
                    tbDados.AppendText("Alunos anteriores da turma foram substituidos pelo CSV.\n");
                }

                foreach (var aluno in alunosParaSalvar)
                {
                    tbDados.AppendText($"- {aluno.Nome} (Matricula: {aluno.Matricula})\n");
                }
            }
            else
            {
                tbDados.AppendText("Erro ao salvar alunos. Verifique os dados do CSV e do banco.\n");
            }
        }

        private void buttonImportarCadastro_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbTurmaNome.Text))
            {
                MessageBox.Show("Informe o nome da turma.");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Selecionar arquivo CSV da turma"
            };

            if (dialog.ShowDialog() == true)
            {
                var dados = CsvUtils.LerCsv(dialog.FileName);
                Importacao(tbTurmaNome.Text.Trim(), dados);
            }
        }

        private bool SalvarAlunos(List<Aluno> listaAlunos)
        {
            if (listaAlunos == null || listaAlunos.Count == 0) return false;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            InserirAlunos(listaAlunos, conn, trans);
                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro na importacao em massa: " + ex.Message);
                return false;
            }
        }

        private bool ReescreverAlunosDaTurma(int turmaId, List<Aluno> alunos)
        {
            if (alunos == null || alunos.Count == 0) return false;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (var trans = conn.BeginTransaction())
                    {
                        try
                        {
                            string deleteQuery = "DELETE FROM Aluno WHERE TurmaId = @turmaId";

                            using (var cmd = new MySqlCommand(deleteQuery, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@turmaId", turmaId);
                                cmd.ExecuteNonQuery();
                            }

                            InserirAlunos(alunos, conn, trans);
                            trans.Commit();
                        }
                        catch
                        {
                            trans.Rollback();
                            throw;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao reescrever alunos da turma: " + ex.Message);
                return false;
            }
        }

        private void InserirAlunos(List<Aluno> listaAlunos, MySqlConnection conn, MySqlTransaction trans)
        {
            StringBuilder sql = new StringBuilder("INSERT INTO Aluno (Matricula, Nome, Email, TurmaId) VALUES ");
            List<string> rows = new List<string>();

            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = conn;
                cmd.Transaction = trans;

                for (int i = 0; i < listaAlunos.Count; i++)
                {
                    string mRef = "@m" + i;
                    string nRef = "@n" + i;
                    string eRef = "@e" + i;
                    string tRef = "@t" + i;

                    rows.Add($"({mRef}, {nRef}, {eRef}, {tRef})");

                    cmd.Parameters.AddWithValue(mRef, listaAlunos[i].Matricula);
                    cmd.Parameters.AddWithValue(nRef, listaAlunos[i].Nome);
                    cmd.Parameters.AddWithValue(eRef, listaAlunos[i].Email ?? "");
                    cmd.Parameters.AddWithValue(tRef, listaAlunos[i].TurmaId);
                }

                sql.Append(string.Join(",", rows));

                // Se o email do CSV vier vazio, mantem o email ja salvo no banco.
                sql.Append(" ON DUPLICATE KEY UPDATE Nome = VALUES(Nome), TurmaId = VALUES(TurmaId), Email = COALESCE(NULLIF(VALUES(Email), ''), Email)");

                cmd.CommandText = sql.ToString();
                cmd.ExecuteNonQuery();
            }
        }

        public bool SalvarAluno(Aluno aluno)
        {
            try
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

                        if (TurmaExiste(aluno.TurmaId))
                        {
                            cmd.Parameters.AddWithValue("@turmaId", aluno.TurmaId);
                        }
                        else
                        {
                            return false;
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public int CriarTurma(string turmaNome)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        INSERT INTO Turma (Nome) VALUES (@nome);
                        SELECT LAST_INSERT_ID();";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nome", turmaNome);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return -1;
            }
        }

        public Turma BuscarTurmaPorNome(string turmaNome)
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
                MessageBox.Show("Erro ao buscar turma: " + ex.Message);
            }

            return null;
        }

        public List<Aluno> CarregarAlunosDoBanco()
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

            tbDados.AppendText("\n--- Alunos no banco ---\n");
            foreach (var a in alunos)
            {
                tbDados.AppendText($"Matricula: {a.Matricula} | Nome: {a.Nome}\n");
            }

            return alunos;
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

        private void buttonAbrirTelaPresenca_Click(object sender, RoutedEventArgs e)
        {
            TelaPresenca tela = new TelaPresenca();
            tela.Show();
        }
    }
}
