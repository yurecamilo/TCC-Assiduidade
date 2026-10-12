namespace TCC_Assiduidade.Modelos.Banco
{
    public class Turma
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public EStatusTurma Status { get; set; } = EStatusTurma.Ativa;
        public System.DateTime? DataAlteracaoStatus { get; set; }
        public string NomeExibicao => FormatarNome(Nome, Status);

        internal static string FormatarNome(string nome, EStatusTurma status)
        {
            if (status == EStatusTurma.Arquivada) return $"{nome} (arquivada)";
            if (status == EStatusTurma.Excluida) return $"{nome} (excluída)";
            return nome;
        }
    }
}
