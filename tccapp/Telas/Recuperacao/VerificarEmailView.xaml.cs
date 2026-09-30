using tccapp.ViewModels;

namespace tccapp.Telas.Recuperacao;

public partial class VerificarEmailView : ContentPage
{
	VerificarEmailViewModel viewModel;
	public VerificarEmailView()
	{
		InitializeComponent();
		viewModel = new VerificarEmailViewModel();
        BindingContext = viewModel;
    }
}