using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace TCC_Assiduidade.ViewModel.Base
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void MostrarErro(string mensagemUsuario, Exception excecao = null)
        {
            if (excecao != null)
            {
                Debug.WriteLine(excecao);
            }

            MessageBox.Show(mensagemUsuario, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
