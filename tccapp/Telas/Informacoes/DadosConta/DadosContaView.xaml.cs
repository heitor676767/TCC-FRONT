using tccapp.ViewModels;

namespace tccapp.Telas.Informacoes.DadosConta;

public partial class DadosContaView : ContentPage
{
	DadosContaViewModel viewModel;
	public DadosContaView()
	{
		InitializeComponent();
		viewModel = new DadosContaViewModel();
		BindingContext = viewModel;
	}
}