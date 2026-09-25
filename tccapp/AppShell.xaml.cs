namespace tccapp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Cadastro", typeof(Telas.Cadastro.UserCadastro));
            Routing.RegisterRoute("Verificar", typeof(Telas.Cadastro.TelaVerificacaoEmail));
            Routing.RegisterRoute("Telefone", typeof(Telas.Cadastro.TelaVerificacaoTelefone));
            Routing.RegisterRoute("Home", typeof(Telas.Home.HomeView));
            Routing.RegisterRoute("CadastroPet", typeof(Telas.Cadastro.CastroPet));
        }
    }
}
