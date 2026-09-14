using System;

namespace TCC_Assiduidade.Excecoes
{
    /// <summary>
    /// Lançada quando não foi possível conectar ou se comunicar com o servidor de
    /// banco de dados (rede indisponível, servidor fora do ar, credenciais
    /// inválidas), diferenciando essa falha de um erro de regra de negócio.
    /// </summary>
    public class ConexaoBancoDadosException : Exception
    {
        public ConexaoBancoDadosException(string mensagem) : base(mensagem) { }

        public ConexaoBancoDadosException(string mensagem, Exception excecaoInterna) : base(mensagem, excecaoInterna) { }
    }
}
