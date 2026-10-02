namespace tccapp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Login", typeof(Telas.Inicio.TelaInicial));
            Routing.RegisterRoute("Cadastro", typeof(Telas.Cadastro.UserCadastro));
            Routing.RegisterRoute("Home", typeof(Telas.Home.HomeView));
            Routing.RegisterRoute("CadastroPet", typeof(Telas.Cadastro.CastroPet));
            Routing.RegisterRoute("InfoConta", typeof(Telas.Informacoes.InfoContaView));
            Routing.RegisterRoute("RecuperarSenha", typeof(Telas.Recuperacao.RecuperarSenhaView));
            Routing.RegisterRoute("VerificarEmail", typeof(Telas.Recuperacao.VerificarEmailView));
        }
    }
}
