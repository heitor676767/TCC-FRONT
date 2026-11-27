using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Maui.Devices.Sensors;
using NetTopologySuite.Geometries;
using System.Globalization;
using System.Text.Json;
using System.Timers;
using tccapp.Models;
using tccapp.Services.Passeios;
using tccapp.Services.Petwalkers;
using Timer = System.Timers.Timer;

namespace tccapp.ViewModels
{
    [QueryProperty(nameof(IdPasseio), "idPasseio")]
    public partial class PasseioAndamentoViewModel : ObservableObject, IDisposable
    {
        public Mapsui.Map Map { get; }
        private readonly MemoryLayer _petwalkerLayer;
        private readonly MemoryLayer _destinoDonoLayer;
        private readonly MemoryLayer _rotaBordaLayer;
        private readonly MemoryLayer _rotaLayer;
        private readonly HttpClient _httpClient = new();

        private DateTime? _dataInicio;
        private int _duracaoMinutos;
        private double? _latDono;
        private double? _lngDono;
        private bool _ehPetwalker;
        private bool _enquadrouMapaInicial;
        private bool _rotaInicialCalculada;

        // Lista de pontos (Lon, Lat) da rota atual para ir cortando conforme o Petwalker anda
        private List<(double Lon, double Lat)> _pontosRotaRestante = new();

        private readonly Timer _timerContador;
        private int _ticksDesdeUltimaBusca;
        private const int IntervaloBuscaSegundos = 4;

        [ObservableProperty] private int idPasseio;
        [ObservableProperty] private string statusAtual = "Aceito";
        [ObservableProperty] private string petwalkerCpf = string.Empty;
        [ObservableProperty] private string nomeExibicao = string.Empty;

        [ObservableProperty] private string tempoPrincipalTexto = "--";
        [ObservableProperty] private string subtituloTempoTexto = "Chegada estimada";

        [ObservableProperty] private bool isIndoAteDono = true;
        [ObservableProperty] private bool isEmAndamento;
        [ObservableProperty] private bool mostrarBotaoIniciar;
        [ObservableProperty] private bool mostrarBotaoFinalizar;
        [ObservableProperty] private string tituloStatusDeslocamento = "Petwalker a caminho";
        [ObservableProperty] private string enderecoDestinoTexto = "Local de encontro";
        [ObservableProperty] private string distanciaPercorridaTexto = "0.0";

        [ObservableProperty] private bool carregando = true;
        [ObservableProperty] private string mensagem = string.Empty;

        public PasseioAndamentoViewModel()
        {
            _ehPetwalker = Preferences.Get("UsuarioTipo", string.Empty) == "Petwalker";

            // Obrigatório: servidores OSRM/OSM bloqueiam requisições sem User-Agent (403)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TCCApp-Petwalker/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(8);

            Map = new Mapsui.Map();
            Map.Layers.Add(OpenStreetMap.CreateTileLayer("TCCApp"));
            Map.Widgets.Clear();

            // Contorno escuro da rota (efeito Google Maps)
            _rotaBordaLayer = new MemoryLayer
            {
                Name = "RotaBorda",
                Features = new List<IFeature>(),
                Style = new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromArgb(255, 24, 58, 122), 9)
                }
            };
            Map.Layers.Add(_rotaBordaLayer);

            // Linha principal azul da rota
            _rotaLayer = new MemoryLayer
            {
                Name = "RotaDeslocamento",
                Features = new List<IFeature>(),
                Style = new VectorStyle
                {
                    Line = new Pen(Mapsui.Styles.Color.FromArgb(255, 66, 133, 244), 6)
                }
            };
            Map.Layers.Add(_rotaLayer);

            // Pino do Dono / Destino
            _destinoDonoLayer = new MemoryLayer
            {
                Name = "DestinoDono",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.9,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 219, 68, 55)),
                    Outline = new Pen(Mapsui.Styles.Color.White, 2.5)
                }
            };
            Map.Layers.Add(_destinoDonoLayer);

            // Pino do Petwalker
            _petwalkerLayer = new MemoryLayer
            {
                Name = "PosicaoPetwalker",
                Features = new List<IFeature>(),
                Style = new SymbolStyle
                {
                    SymbolScale = 0.95,
                    Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 47, 107, 142)),
                    Outline = new Pen(Mapsui.Styles.Color.White, 2.5)
                }
            };
            Map.Layers.Add(_petwalkerLayer);

            var (x, y) = SphericalMercator.FromLonLat(-46.5961203, -23.5189015);
            Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), 3);

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

                PetwalkerCpf = passeio.PetwalkerCpf;
                _duracaoMinutos = passeio.Duracao;
                _dataInicio = passeio.DataInicio;
                StatusAtual = passeio.StatusPass;

                NomeExibicao = _ehPetwalker
                    ? $"{passeio.DonoNome} ({passeio.PetNome})"
                    : passeio.PetwalkerNome;

                if (passeio.Localizacao != null)
                {
                    _latDono = (double)passeio.Localizacao.Latitude;
                    _lngDono = (double)passeio.Localizacao.Longitude;

                    EnderecoDestinoTexto = !string.IsNullOrWhiteSpace(passeio.Localizacao.Numero)
                        ? passeio.Localizacao.Numero
                        : "Ponto de encontro marcado no mapa";

                    AtualizarPinoDono(_latDono.Value, _lngDono.Value);
                }

                AtualizarEstadoVisual();
                await AtualizarPosicoesERotaAsync();

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

        private void AtualizarEstadoVisual()
        {
            IsIndoAteDono = StatusAtual == "Aceito";
            IsEmAndamento = StatusAtual == "EmAndamento";

            MostrarBotaoIniciar = _ehPetwalker && IsIndoAteDono;
            MostrarBotaoFinalizar = _ehPetwalker && IsEmAndamento;

            if (IsIndoAteDono)
            {
                SubtituloTempoTexto = "Chegada estimada";
                TituloStatusDeslocamento = _ehPetwalker
                    ? "Siga o trajeto até o dono"
                    : "Petwalker indo até você";
            }
            else if (IsEmAndamento)
            {
                SubtituloTempoTexto = "Restantes";
                LimparRotaDoMapa();
                AtualizarTempoRestantePasseio();
            }
        }

        private void OnTick(object? sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (IsEmAndamento)
                    AtualizarTempoRestantePasseio();

                _ticksDesdeUltimaBusca++;
                if (_ticksDesdeUltimaBusca >= IntervaloBuscaSegundos)
                {
                    _ticksDesdeUltimaBusca = 0;
                    await SincronizarStatusEPosicaoAsync();
                }
            });
        }

        private async Task SincronizarStatusEPosicaoAsync()
        {
            try
            {
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);
                var passeio = await passeioService.GetPorIdAsync(IdPasseio);

                if (passeio.StatusPass == "Finalizado" || passeio.StatusPass == "Cancelado")
                {
                    _timerContador.Stop();
                    string titulo = passeio.StatusPass == "Cancelado" ? "Corrida cancelada" : "Passeio encerrado";
                    await Shell.Current.DisplayAlertAsync(titulo, $"O passeio foi {passeio.StatusPass.ToLower()}.", "OK");
                    await VoltarParaHomeAsync();
                    return;
                }

                if (passeio.StatusPass != StatusAtual)
                {
                    StatusAtual = passeio.StatusPass;
                    _dataInicio = passeio.DataInicio;
                    AtualizarEstadoVisual();
                }

                await AtualizarPosicoesERotaAsync();
            }
            catch { }
        }

        private async Task AtualizarPosicoesERotaAsync()
        {
            string token = Preferences.Get("UsuarioToken", string.Empty);
            double? latWalker = null;
            double? lngWalker = null;

            if (_ehPetwalker)
            {
                try
                {
                    var loc = await Geolocation.Default.GetLocationAsync(
                                  new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(4)))
                              ?? await Geolocation.Default.GetLastKnownLocationAsync();

                    if (loc != null)
                    {
                        latWalker = loc.Latitude;
                        lngWalker = loc.Longitude;
                        var walkerService = new PetwalkerService(token);
                        await walkerService.AtualizarLocalizacaoAsync((decimal)latWalker, (decimal)lngWalker);
                    }
                }
                catch { }
            }

            if (latWalker == null || lngWalker == null)
            {
                try
                {
                    var walkerService = new PetwalkerService(token);
                    var perfil = await walkerService.GetPorCpfAsync(PetwalkerCpf);
                    if (perfil?.Latitude != null && perfil.Longitude != null)
                    {
                        latWalker = (double)perfil.Latitude.Value;
                        lngWalker = (double)perfil.Longitude.Value;
                    }
                }
                catch { }
            }

            if (latWalker == null || lngWalker == null)
                return;

            var (xw, yw) = SphericalMercator.FromLonLat(lngWalker.Value, latWalker.Value);
            var pontoWalker = new MPoint(xw, yw);

            _petwalkerLayer.Features = new List<IFeature> { new PointFeature(pontoWalker) };
            _petwalkerLayer.DataHasChanged();

            if (IsIndoAteDono && _latDono.HasValue && _lngDono.HasValue)
            {
                // Calcula a rota pelas ruas na primeira vez (ou se o petwalker saiu mais de 80m da rota)
                bool saiuDaRota = _pontosRotaRestante.Count >= 2 &&
                                  DistanciaEmKm(latWalker.Value, lngWalker.Value, _pontosRotaRestante[0].Lat, _pontosRotaRestante[0].Lon) > 0.08;

                if (!_rotaInicialCalculada || saiuDaRota)
                {
                    await CalcularRotaEEstimativaAsync(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value);
                    _rotaInicialCalculada = true;
                }
                else if (_pontosRotaRestante.Count >= 2)
                {
                    // Vai apagando os trechos já percorridos para a linha azul encurtar atrás do petwalker
                    ApararTrechoPercorrido(latWalker.Value, lngWalker.Value);

                    double kmRestante = DistanciaEmKm(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value);
                    TempoPrincipalTexto = Math.Max(1, (int)Math.Ceiling((kmRestante / 4.5) * 60.0)).ToString();
                }

                if (!_enquadrouMapaInicial)
                {
                    EnquadrarOrigemEDestino(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value);
                    _enquadrouMapaInicial = true;
                }
                else
                {
                    Map.Navigator.CenterOn(pontoWalker);
                }
            }
            else if (IsEmAndamento && _latDono.HasValue && _lngDono.HasValue)
            {
                Map.Navigator.CenterOn(pontoWalker);
                double km = DistanciaEmKm(_latDono.Value, _lngDono.Value, latWalker.Value, lngWalker.Value);
                DistanciaPercorridaTexto = km.ToString("0.0");
            }
        }

        private void ApararTrechoPercorrido(double latAtual, double lngAtual)
        {
            int indiceMaisProximo = 0;
            double menorDistancia = double.MaxValue;

            for (int i = 0; i < _pontosRotaRestante.Count; i++)
            {
                double d = DistanciaEmKm(latAtual, lngAtual, _pontosRotaRestante[i].Lat, _pontosRotaRestante[i].Lon);
                if (d < menorDistancia)
                {
                    menorDistancia = d;
                    indiceMaisProximo = i;
                }
            }

            if (indiceMaisProximo > 0)
            {
                _pontosRotaRestante.RemoveRange(0, indiceMaisProximo);
            }

            if (_pontosRotaRestante.Count > 0)
            {
                _pontosRotaRestante[0] = (lngAtual, latAtual);
                DesenharPolilinhaNoMapa(_pontosRotaRestante);
            }
        }

        private async Task CalcularRotaEEstimativaAsync(double latOrigem, double lngOrigem, double latDestino, double lngDestino)
        {
            string[] servidoresOsrm =
            {
                string.Format(CultureInfo.InvariantCulture,
                    "https://router.project-osrm.org/route/v1/foot/{0},{1};{2},{3}?overview=full&geometries=geojson",
                    lngOrigem, latOrigem, lngDestino, latDestino),
                string.Format(CultureInfo.InvariantCulture,
                    "https://routing.openstreetmap.de/routed-foot/route/v1/foot/{0},{1};{2},{3}?overview=full&geometries=geojson",
                    lngOrigem, latOrigem, lngDestino, latDestino)
            };

            foreach (string url in servidoresOsrm)
            {
                try
                {
                    string json = await _httpClient.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(json);

                    var routes = doc.RootElement.GetProperty("routes");
                    if (routes.GetArrayLength() > 0)
                    {
                        var rota = routes[0];
                        double duracaoSegundos = rota.GetProperty("duration").GetDouble();
                        int minutosEstimados = Math.Max(1, (int)Math.Ceiling(duracaoSegundos / 60.0));
                        TempoPrincipalTexto = minutosEstimados.ToString();

                        var coords = rota.GetProperty("geometry").GetProperty("coordinates");
                        var novaLista = new List<(double Lon, double Lat)> { (lngOrigem, latOrigem) };

                        foreach (var par in coords.EnumerateArray())
                        {
                            novaLista.Add((par[0].GetDouble(), par[1].GetDouble()));
                        }

                        _pontosRotaRestante = novaLista;
                        DesenharPolilinhaNoMapa(_pontosRotaRestante);
                        return;
                    }
                }
                catch { }
            }

            // Fallback apenas se estiver totalmente sem acesso aos servidores de rota
            double km = DistanciaEmKm(latOrigem, lngOrigem, latDestino, lngDestino);
            TempoPrincipalTexto = Math.Max(1, (int)Math.Ceiling((km / 4.5) * 60.0)).ToString();
            _pontosRotaRestante = new List<(double Lon, double Lat)>
            {
                (lngOrigem, latOrigem),
                (lngDestino, latDestino)
            };
            DesenharPolilinhaNoMapa(_pontosRotaRestante);
        }

        private void DesenharPolilinhaNoMapa(List<(double Lon, double Lat)> pontos)
        {
            if (pontos.Count < 2)
            {
                LimparRotaDoMapa();
                return;
            }

            var coordsMercator = pontos
                .Select(p =>
                {
                    var (mx, my) = SphericalMercator.FromLonLat(p.Lon, p.Lat);
                    return new Coordinate(mx, my);
                })
                .ToArray();

            var lineString = new LineString(coordsMercator);

            _rotaBordaLayer.Features = new List<IFeature> { new GeometryFeature(lineString) };
            _rotaBordaLayer.DataHasChanged();

            _rotaLayer.Features = new List<IFeature> { new GeometryFeature(lineString) };
            _rotaLayer.DataHasChanged();
        }

        private void LimparRotaDoMapa()
        {
            _pontosRotaRestante.Clear();
            _rotaBordaLayer.Features = new List<IFeature>();
            _rotaBordaLayer.DataHasChanged();
            _rotaLayer.Features = new List<IFeature>();
            _rotaLayer.DataHasChanged();
        }

        private void EnquadrarOrigemEDestino(double lat1, double lng1, double lat2, double lng2)
        {
            var (x1, y1) = SphericalMercator.FromLonLat(lng1, lat1);
            var (x2, y2) = SphericalMercator.FromLonLat(lng2, lat2);

            double margem = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)) * 0.35 + 150;
            var box = new MRect(
                Math.Min(x1, x2) - margem,
                Math.Min(y1, y2) - margem,
                Math.Max(x1, x2) + margem,
                Math.Max(y1, y2) + margem);

            Map.Navigator.ZoomToBox(box, MBoxFit.Fit);
        }

        private void AtualizarPinoDono(double lat, double lng)
        {
            var (x, y) = SphericalMercator.FromLonLat(lng, lat);
            _destinoDonoLayer.Features = new List<IFeature> { new PointFeature(new MPoint(x, y)) };
            _destinoDonoLayer.DataHasChanged();
        }

        private void AtualizarTempoRestantePasseio()
        {
            if (_dataInicio == null)
            {
                TempoPrincipalTexto = _duracaoMinutos.ToString();
                return;
            }

            var fim = _dataInicio.Value.AddMinutes(_duracaoMinutos);
            var restante = fim - DateTime.Now;

            TempoPrincipalTexto = restante.TotalSeconds <= 0
                ? "0"
                : Math.Ceiling(restante.TotalMinutes).ToString("0");
        }

        [RelayCommand]
        private async Task AbrirMenuPasseio()
        {
            string escolha = await Shell.Current.DisplayActionSheetAsync(
                "Opções do passeio",
                "Fechar",
                null,
                "Cancelar corrida");

            if (escolha == "Cancelar corrida")
            {
                bool confirmar = await Shell.Current.DisplayAlertAsync(
                    "Cancelar corrida",
                    "Tem certeza que deseja cancelar este passeio?",
                    "Sim, cancelar",
                    "Não");

                if (!confirmar)
                    return;

                try
                {
                    Carregando = true;
                    string token = Preferences.Get("UsuarioToken", string.Empty);
                    var passeioService = new PasseioService(token);

                    await passeioService.CancelarAsync(IdPasseio);
                    _timerContador.Stop();

                    await Shell.Current.DisplayAlertAsync("Cancelado", "A corrida foi cancelada.", "OK");
                    await VoltarParaHomeAsync();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlertAsync("Erro ao cancelar", ex.Message, "OK");
                }
                finally
                {
                    Carregando = false;
                }
            }
        }

        [RelayCommand]
        private async Task IniciarPasseio()
        {
            try
            {
                Carregando = true;
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                await passeioService.IniciarAsync(IdPasseio);
                StatusAtual = "EmAndamento";
                _dataInicio = DateTime.Now;
                AtualizarEstadoVisual();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }

        [RelayCommand]
        private async Task FinalizarPasseio()
        {
            try
            {
                Carregando = true;
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                decimal valor = _duracaoMinutos * 1.0m;
                await passeioService.FinalizarAsync(IdPasseio, valor);

                _timerContador.Stop();
                await Shell.Current.DisplayAlertAsync("Finalizado!", "Passeio encerrado com sucesso.", "OK");
                await VoltarParaHomeAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Erro", ex.Message, "OK");
            }
            finally
            {
                Carregando = false;
            }
        }

        [RelayCommand]
        private async Task VoltarParaHomeAsync()
        {
            _timerContador.Stop();
            await Shell.Current.GoToAsync("..");
        }

        private static double DistanciaEmKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double raioTerraKm = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLng = (lng2 - lng1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            return raioTerraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        public void Dispose()
        {
            _timerContador?.Stop();
            _timerContador?.Dispose();
        }
    }
}