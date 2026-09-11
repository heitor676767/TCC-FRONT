namespace tccapp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute("Cadastro", typeof(Telas.Cadastro.UserCadastro));
            Routing.RegisterRoute("Verificar", typeof(Telas.Cadastro.TelaVerificacaoEmail));
        }
    }
}
