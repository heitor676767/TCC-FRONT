using tccapp.ViewModels;

namespace tccapp.Telas.Home;

public partial class HomeViewPetWalker : ContentPage
{
	HomeViewPetwalkerModel viewModel;
	public HomeViewPetWalker()
	{

	
		InitializeComponent();
        viewModel = new HomeViewPetwalkerModel();
		BindingContext = viewModel;
    }
    private async void SwitchDisponibilidade_Toggled(object sender, ToggledEventArgs e)
    {
        await viewModel.AlterarDisponibilidadeAsync(e.Value);
    }
}