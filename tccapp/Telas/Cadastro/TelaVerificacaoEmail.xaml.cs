using tccapp.ViewModels;

namespace tccapp.Telas.Cadastro;

public partial class TelaVerificacaoEmail : ContentPage
{
	public TelaVerificacaoEmail()
	{
		InitializeComponent();
		BindingContext = new TelaVerificacaoEmailViewModel();
	}
}