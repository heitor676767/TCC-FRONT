using tccapp.ViewModels;

namespace tccapp.Telas.Passeios;

public partial class PasseioAndamentoView : ContentPage
{
    PasseioAndamentoViewModel viewModel;
    public PasseioAndamentoView()
    {
        InitializeComponent();
        viewModel = new PasseioAndamentoViewModel();
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.Dispose();
    }
}