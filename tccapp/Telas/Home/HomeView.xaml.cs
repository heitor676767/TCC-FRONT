
using tccapp.ViewModels;

namespace tccapp.Telas.Home;

public partial class HomeView : ContentPage
{
    HomeViewModel viewModel;
    public HomeView()
	{

		InitializeComponent();
        viewModel = new HomeViewModel();
        BindingContext = viewModel;
	}
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.IniciarRastreamentoAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.PararRastreamento();
    }
}