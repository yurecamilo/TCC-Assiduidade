namespace TCC_Assiduidade.Modelos.Dashboard
{
    public class OpcaoTurmaDashboard
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public bool Arquivada { get; set; }
    }

    public class OpcaoAlunoDashboard
    {
        public string Matricula { get; set; }
        public string Nome { get; set; }
        public int TurmaId { get; set; }
        public string Turma { get; set; }

        public string Descricao
        {
            get
            {
                string aluno = string.IsNullOrWhiteSpace(Matricula)
                    ? Nome
                    : Nome + " — " + Matricula;

                return string.IsNullOrWhiteSpace(Turma)
                    ? aluno
                    : aluno + " — " + Turma;
            }
        }
    }
}
