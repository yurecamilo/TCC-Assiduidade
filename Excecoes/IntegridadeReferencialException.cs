using System;

namespace TCC_Assiduidade.Excecoes
{
    /// <summary>
    /// Lançada quando uma operação violaria a integridade referencial do banco de
    /// dados — por exemplo, excluir uma turma que ainda possui alunos, aulas ou
    /// ausências vinculadas a ela.
    /// </summary>
    public class IntegridadeReferencialException : Exception
    {
        public IntegridadeReferencialException(string mensagem) : base(mensagem) { }

        public IntegridadeReferencialException(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna) { }
    }
}
