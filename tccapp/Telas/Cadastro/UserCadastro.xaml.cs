using tccapp.ViewModels;

namespace tccapp.Telas.Cadastro;

public partial class UserCadastro : ContentPage
{
	public UserCadastro()
	{
		InitializeComponent();
		BindingContext = new UserCadastroViewModel();
	}
}