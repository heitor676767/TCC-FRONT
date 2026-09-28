using tccapp.ViewModels;

namespace tccapp.Telas.Cadastro;

public partial class CastroPet : ContentPage
{
	PetCadastroViewModel viewModel;
	public CastroPet()
	{

		InitializeComponent();

		viewModel = new PetCadastroViewModel();
		BindingContext = viewModel;
	}
}