using tccapp.ViewModels;

namespace tccapp.Telas.Home;

public partial class HomeView : ContentPage
{
	public HomeView()
	{
		InitializeComponent();
		BindingContext = new HomeViewModel();
	}
}