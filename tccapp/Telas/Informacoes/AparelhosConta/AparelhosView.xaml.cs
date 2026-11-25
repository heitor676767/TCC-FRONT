
using tccapp.ViewModels;

namespace tccapp.Telas.Informacoes.AparelhosConta;

public partial class AparelhosView : ContentPage
{
    private readonly AparelhosViewModel viewModel;

    public AparelhosView()
    {
        InitializeComponent();
        viewModel = new AparelhosViewModel();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await viewModel.CarregarSessoesCommand.ExecuteAsync(null);
    }
}