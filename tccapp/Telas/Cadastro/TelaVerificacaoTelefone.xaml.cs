using tccapp.ViewModels;

namespace tccapp.Telas.Cadastro;

public partial class TelaVerificacaoTelefone : ContentPage
{
	public TelaVerificacaoTelefone()
	{
		InitializeComponent();
        BindingContext = new TelaVerificacaoEmailViewModel();
    }
}