using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using System.Timers;
using Timer = System.Timers.Timer;
using tccapp.Models;
using tccapp.Services.Passeios;
using tccapp.Services.Petwalkers;

namespace tccapp.ViewModels
{
    [QueryProperty(nameof(IdPasseio), "idPasseio")]
    public partial class PasseioAndamentoViewModel : ObservableObject, IDisposable
    {
        public Mapsui.Map Map { get; }
        private readonly MemoryLayer _petwalkerLayer;

        // Dados do início do passeio: guardados uma vez só, pra calcular o resto em cima deles.
        private DateTime? _dataInicio;
        private int _duracaoMinutos;
        private decimal? _latitudeInicial;
        private decimal? _longitudeInicial;

        // Timer de 1 em 1 segundo só pro contador. A busca de posição é mais espaçada
        // (a cada 10s) pra não martelar a API/GPS à toa.
        private readonly Timer _timerContador;
        private int _ticksDesdeUltimaBusca;
        private const int IntervaloBuscaPosicaoSegundos = 10;

        [ObservableProperty]
        private int idPasseio;

        [ObservableProperty]
        private string petwalkerCpf = string.Empty;

        [ObservableProperty]
        private string nomePetwalker = string.Empty;

        [ObservableProperty]
        private string tempoRestanteTexto = "--";

        [ObservableProperty]
        private string distanciaPercorridaTexto = "0.0";

        [ObservableProperty]
        private bool carregando = true;

        [ObservableProperty]
        private string mensagem = string.Empty;

        public PasseioAndamentoViewModel()
        {
            Map = new Mapsui.Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer("TCCApp"));

            _petwalkerLayer = new MemoryLayer
            {
                Name = "PosicaoPetwalker",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.9,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 211, 84, 0)),
                    Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 2)
                }
            };
            Map.Layers.Add(_petwalkerLayer);

            var (x, y) = SphericalMercator.FromLonLat(-46.5961203, -23.5189015);
            Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 10);

            // Dispara a cada 1s: atualiza o cronômetro sempre, e a cada N ticks busca a posição.
            _timerContador = new Timer(1000);
            _timerContador.Elapsed += OnTick;
        }

        partial void OnIdPasseioChanged(int value)
        {
            _ = CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            try
            {
                Carregando = true;
                Mensagem = string.Empty;

                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                PasseioDto passeio = await passeioService.GetPorIdAsync(IdPasseio);

                NomePetwalker = passeio.PetwalkerNome;
                PetwalkerCpf = passeio.PetwalkerCpf;
                _duracaoMinutos = passeio.Duracao;
                _dataInicio = passeio.DataInicio;
                _latitudeInicial = passeio.Localizacao?.Latitude;
                _longitudeInicial = passeio.Localizacao?.Longitude;

                if (passeio.StatusPass != "EmAndamento")
                    Mensagem = $"Esse passeio não está em andamento (status: {passeio.StatusPass}).";

                await BuscarPosicaoPetwalkerAsync();
                AtualizarTempoRestante();

                _ticksDesdeUltimaBusca = 0;
                _timerContador.Start();
            }
            catch (Exception ex)
            {
                Mensagem = ex.Message;
            }
            finally
            {
                Carregando = false;
            }
        }

        private void OnTick(object? sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                AtualizarTempoRestante();

                _ticksDesdeUltimaBusca++;
                if (_ticksDesdeUltimaBusca >= IntervaloBuscaPosicaoSegundos)
                {
                    _ticksDesdeUltimaBusca = 0;
                    await BuscarPosicaoPetwalkerAsync();
                }
            });
        }

        private void AtualizarTempoRestante()
        {
            if (_dataInicio == null)
            {
                TempoRestanteTexto = $"{_duracaoMinutos}";
                return;
            }

            var fim = _dataInicio.Value.AddMinutes(_duracaoMinutos);
            var restante = fim - DateTime.Now;

            if (restante.TotalSeconds <= 0)
            {
                TempoRestanteTexto = "0";
                _timerContador.Stop();
                return;
            }

            TempoRestanteTexto = Math.Ceiling(restante.TotalMinutes).ToString("0");
        }

        private async Task BuscarPosicaoPetwalkerAsync()
        {
            if (string.IsNullOrEmpty(PetwalkerCpf))
                return;

            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var petwalkerService = new PetwalkerService(token);

                PetwalkerPerfil perfil = await petwalkerService.GetPorCpfAsync(PetwalkerCpf);

                if (perfil?.Latitude == null || perfil.Longitude == null)
                    return; // petwalker ainda não tem posição atual disponível

                var (x, y) = SphericalMercator.FromLonLat((double)perfil.Longitude.Value, (double)perfil.Latitude.Value);

                _petwalkerLayer.Features = new List<IFeature>
                {
                    new PointFeature(new MPoint(x, y))
                };
                _petwalkerLayer.DataHasChanged();
                Map.Navigator.CenterOn(new MPoint(x, y));

                AtualizarDistanciaPercorrida((double)perfil.Latitude.Value, (double)perfil.Longitude.Value);
            }
            catch
            {
                // Silencioso: uma falha pontual de rede não pode travar o cronômetro na tela.
            }
        }

        // Simplificação: calcula a distância em linha reta do ponto de encontro combinado
        // até a posição atual do petwalker (Haversine), não o caminho real percorrido
        // (isso exigiria guardar um histórico de pontos, fora do escopo por enquanto).
        private void AtualizarDistanciaPercorrida(double latAtual, double lngAtual)
        {
            if (_latitudeInicial == null || _longitudeInicial == null)
                return;

            double distanciaKm = DistanciaEmKm(
                (double)_latitudeInicial.Value, (double)_longitudeInicial.Value,
                latAtual, lngAtual);

            DistanciaPercorridaTexto = distanciaKm.ToString("0.0");
        }

        private static double DistanciaEmKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double raioTerraKm = 6371.0;
            double dLat = GrausParaRadianos(lat2 - lat1);
            double dLng = GrausParaRadianos(lng2 - lng1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(GrausParaRadianos(lat1)) * Math.Cos(GrausParaRadianos(lat2)) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            return raioTerraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double GrausParaRadianos(double graus) => graus * Math.PI / 180.0;

        [RelayCommand]
        private async Task Voltar()
        {
            await Shell.Current.GoToAsync("..");
        }

        public void Dispose()
        {
            _timerContador?.Stop();
            _timerContador?.Dispose();
        }
    }
}