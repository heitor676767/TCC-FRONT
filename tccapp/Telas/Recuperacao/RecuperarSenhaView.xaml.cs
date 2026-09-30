using tccapp.ViewModels;

namespace tccapp.Telas.Recuperacao;

public partial class RecuperarSenhaView : ContentPage
{
	RecuperarSenhaViewModel viewModel;
	public RecuperarSenhaView()
	{

		InitializeComponent();
		viewModel = new RecuperarSenhaViewModel();
		BindingContext = viewModel;
    }
}