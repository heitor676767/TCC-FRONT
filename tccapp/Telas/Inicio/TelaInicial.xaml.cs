using tccapp.ViewModels;

namespace tccapp.Telas.Inicio;

public partial class TelaInicial : ContentPage
{
	public TelaInicial()
	{
		InitializeComponent();
        BindingContext = new TelaInicialViewModel();
    }
}