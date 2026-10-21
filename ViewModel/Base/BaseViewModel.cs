using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
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
        /// Limite em percentual que a barra alcança "andando" durante uma etapa
        /// longa. Evita que o progresso pareça travado quando a etapa mais custosa
        /// (validação e gravação no banco) é uma única chamada síncrona.
        /// </summary>
        private const double LimiteProgressoRastejante = 88;

        private bool _estaOcupado;
        private double _progressoImportacao;
        private string _etapaImportacao;
        private DispatcherTimer _timerProgresso;

        /// <summary>
        /// Indica que uma operação longa está em andamento e o overlay de
        /// carregamento deve proteger a janela, bloqueando interações.
        /// </summary>
        public bool EstaOcupado
        {
            get => _estaOcupado;
            private set { _estaOcupado = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Percentual (0 a 100) exibido pela barra de progresso do overlay.
        /// </summary>
        public double ProgressoImportacao
        {
            get => _progressoImportacao;
            private set { _progressoImportacao = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Texto da etapa atual exibido abaixo da barra de progresso do overlay.
        /// </summary>
        public string EtapaImportacao
        {
            get => _etapaImportacao;
            private set { _etapaImportacao = value; OnPropertyChanged(); }
        }

        /// <summary>Abre o overlay exibindo a primeira etapa da operação.</summary>
        protected void IniciarProgresso(string etapaInicial)
        {
            AtualizarProgresso(10, etapaInicial);
        }

        /// <summary>Move a barra para um ponto fixo e passa a exibir a etapa informada.</summary>
        protected void AtualizarProgresso(double progresso, string etapa)
        {
            ProgressoImportacao = Math.Min(progresso, 100);
            EtapaImportacao = etapa;
            IniciarRastejoDoProgresso();
        }

        /// <summary>Leva a barra a 100% e encerra o rastelo de avanço.</summary>
        protected void ConcluirProgresso(string etapaFinal)
        {
            PararRastejoDoProgresso();
            ProgressoImportacao = 100;
            EtapaImportacao = etapaFinal;
        }

        /// <summary>Zera o indicador e esconde o overlay, devolvendo a janela ao usuário.</summary>
        protected void FinalizarProgresso()
        {
            PararRastejoDoProgresso();
            ProgressoImportacao = 0;
            EtapaImportacao = null;
            EstaOcupado = false;
        }

        /// <summary>
        /// Faz a barra avançar lentamente enquanto a operação atual é um bloco único
        /// sem pontos intermediários de atualização, até o limite de segurança.
        /// </summary>
        private void IniciarRastejoDoProgresso()
        {
            PararRastejoDoProgresso();

            _timerProgresso = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timerProgresso.Tick += (s, e) =>
            {
                if (ProgressoImportacao < LimiteProgressoRastejante)
                    ProgressoImportacao = Math.Min(ProgressoImportacao + 1, LimiteProgressoRastejante);
            };
            _timerProgresso.Start();
            EstaOcupado = true;
        }

        private void PararRastejoDoProgresso()
        {
            if (_timerProgresso == null)
                return;

            _timerProgresso.Stop();
            _timerProgresso = null;
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
            string titulo;
            string mensagem;
            ClassificarErro(excecao, mensagemPadrao, out titulo, out mensagem);

            PopupMensagem.Show(mensagem, titulo, MessageBoxButton.OK, MessageBoxImage.Error);
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
