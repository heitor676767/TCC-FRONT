
using tccapp.ViewModels;

namespace tccapp.Telas.Informacoes.CartoesConta;

public partial class CartoesView : ContentPage
{
	CartoesViewModel viewModel;
	public CartoesView()
	{
		InitializeComponent();
		viewModel = new CartoesViewModel();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.CarregarCartoesCommand.ExecuteAsync(null);
    }
}