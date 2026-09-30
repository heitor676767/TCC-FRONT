using tccapp.ViewModels;

namespace tccapp.Telas.Informacoes;

public partial class InfoContaView : ContentPage
{
	InfoContaViewModel viewModel;
	public InfoContaView()
	{

		InitializeComponent();
		viewModel = new InfoContaViewModel();
        BindingContext = viewModel;
    }
}