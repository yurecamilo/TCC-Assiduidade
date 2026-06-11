using System.ComponentModel;
using System.Runtime.CompilerServices;

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
