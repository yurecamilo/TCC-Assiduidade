using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TCC_Assiduidade.ViewModel.Base
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        // Este evento nativo é o que o WPF fica escutando para atualizar a tela
    public event PropertyChangedEventHandler PropertyChanged;

        // Este método avisa o WPF: "Ei, a variável X mudou de valor! Atualiza o layout!"
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
