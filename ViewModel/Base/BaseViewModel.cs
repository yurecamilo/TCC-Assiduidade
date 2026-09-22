using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using TCC_Assiduidade.Excecoes;

namespace TCC_Assiduidade.ViewModel.Base
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Exibe uma mensagem de erro ao usuário. Quando a exceção é de um dos tipos
        /// de domínio conhecidos (registro duplicado, integridade referencial,
        /// registro não encontrado, falha de conexão ou dados inválidos), mostra um
        /// título e uma mensagem específicos para aquele tipo de erro — em vez de um
        /// texto genérico igual para qualquer falha. Para qualquer outra exceção não
        /// classificada, usa a mensagem padrão informada pelo chamador.
        /// </summary>
        protected void MostrarErro(string mensagemPadrao, Exception excecao = null)
        {
            if (excecao != null)
            {
                Debug.WriteLine(excecao);
            }

            string titulo;
            string mensagem;
            ClassificarErro(excecao, mensagemPadrao, out titulo, out mensagem);

            MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Sobrecarga que classifica e exibe o erro sem exigir uma mensagem padrão do
        /// chamador — útil quando qualquer exceção não classificada deve cair em um
        /// texto genérico definido aqui mesmo.
        /// </summary>
        protected void MostrarErro(Exception excecao)
        {
            MostrarErro("Ocorreu um erro inesperado. Tente novamente.", excecao);
        }

        /// <summary>
        /// Define o título e a mensagem exibidos para cada tipo de exceção. Mantido
        /// como método estático e independente do MessageBox para poder ser coberto
        /// por testes automatizados.
        /// </summary>
        internal static void ClassificarErro(Exception excecao, string mensagemPadrao, out string titulo, out string mensagem)
        {
            string fallback = string.IsNullOrWhiteSpace(mensagemPadrao)
                ? "Ocorreu um erro inesperado. Tente novamente."
                : mensagemPadrao;

            if (excecao is RegistroDuplicadoException)
            {
                titulo = "Registro duplicado";
                mensagem = excecao.Message;
            }
            else if (excecao is IntegridadeReferencialException)
            {
                titulo = "Ação bloqueada";
                mensagem = excecao.Message;
            }
            else if (excecao is RegistroNaoEncontradoException)
            {
                titulo = "Registro não encontrado";
                mensagem = excecao.Message;
            }
            else if (excecao is ConexaoBancoDadosException)
            {
                titulo = "Falha de conexão";
                mensagem = excecao.Message;
            }
            else if (excecao is ArgumentException)
            {
                titulo = "Dados inválidos";
                mensagem = excecao.Message;
            }
            else
            {
                titulo = "Erro";
                mensagem = fallback;
            }
        }
    }
}
