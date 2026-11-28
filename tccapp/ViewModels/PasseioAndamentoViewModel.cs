using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Microsoft.Maui.Devices;
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
        private string _codigoInicioEsperado = string.Empty;

        // Controle do trajeto percorrido durante o passeio
        private readonly List<(double Lon, double Lat)> _historicoCaminhoPasseio = new();
        private double _distanciaAcumuladaKm;

        // Controle do aviso de 5 minutos e rota de retorno ao dono
        private bool _avisoRetornoEnviado;
        private bool _modoRetornoDono;
        private bool _rotaRetornoCalculada;

        // Distância máxima em km (0.08 km = 80 metros) para liberar o início do passeio
        private const double DistanciaMaximaInicioKm = 0.08;

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
        [ObservableProperty] private bool podeIniciarPasseio;
        [ObservableProperty] private string textoBotaoIniciar = "Aproxime-se do local para iniciar";
        [ObservableProperty] private string codigoConfirmacaoTexto = string.Empty;
        [ObservableProperty] private bool mostrarBotaoFinalizar;
        [ObservableProperty] private bool mostrarMenuOpcoes = true;
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

            // Contorno escuro da rota / trajeto
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

            // Linha principal azul da rota / trajeto
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
                _dataInicio = NormalizarDataInicio(passeio.DataInicio);
                StatusAtual = passeio.StatusPass;
                _codigoInicioEsperado = ExtrairCodigoRga(passeio.PetRga);

                NomeExibicao = _ehPetwalker
                    ? $"{passeio.DonoNome} ({passeio.PetNome})"
                    : passeio.PetwalkerNome;

                string enderecoBase = passeio.Localizacao != null && !string.IsNullOrWhiteSpace(passeio.Localizacao.Numero)
                    ? passeio.Localizacao.Numero
                    : "Ponto de encontro marcado no mapa";

                if (_ehPetwalker)
                {
                    CodigoConfirmacaoTexto = string.Empty;
                    EnderecoDestinoTexto = enderecoBase;
                }
                else
                {
                    CodigoConfirmacaoTexto = $"Código de início: {_codigoInicioEsperado}";
                    EnderecoDestinoTexto = $"Código para iniciar: {_codigoInicioEsperado} • {enderecoBase}";
                }

                if (passeio.Localizacao != null)
                {
                    _latDono = (double)passeio.Localizacao.Latitude;
                    _lngDono = (double)passeio.Localizacao.Longitude;
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

        private static DateTime? NormalizarDataInicio(DateTime? dataApi)
        {
            if (dataApi == null)
                return null;

            DateTime utc = dataApi.Value.Kind == DateTimeKind.Local
                ? dataApi.Value.ToUniversalTime()
                : DateTime.SpecifyKind(dataApi.Value, DateTimeKind.Utc);

            return utc.ToLocalTime();
        }

        private static string ExtrairCodigoRga(string? rga)
        {
            if (string.IsNullOrWhiteSpace(rga))
                return "0000";

            string apenasDigitos = new string(rga.Where(char.IsDigit).ToArray());
            string fonte = apenasDigitos.Length > 0 ? apenasDigitos : rga.Trim();

            return fonte.Length <= 4
                ? fonte
                : fonte[^4..];
        }

        private void AtualizarEstadoVisual()
        {
            IsIndoAteDono = StatusAtual == "Aceito";
            IsEmAndamento = StatusAtual == "EmAndamento";

            MostrarBotaoIniciar = _ehPetwalker && IsIndoAteDono;
            MostrarBotaoFinalizar = _ehPetwalker && IsEmAndamento;
            MostrarMenuOpcoes = !(_ehPetwalker && IsEmAndamento);

            if (IsIndoAteDono)
            {
                SubtituloTempoTexto = "Chegada estimada";
                TituloStatusDeslocamento = _ehPetwalker
                    ? "Siga o trajeto até o dono"
                    : "Petwalker indo até você";

                if (_latDono.HasValue && _lngDono.HasValue)
                    AtualizarPinoDono(_latDono.Value, _lngDono.Value);
            }
            else if (IsEmAndamento)
            {
                PodeIniciarPasseio = false;
                SubtituloTempoTexto = "Restantes";
                LimparRotaDoMapa();

                // Depois de pegar o pet, o Petwalker não vê mais o pino do Dono no mapa (até faltar 5 min)
                if (_ehPetwalker && !_modoRetornoDono)
                {
                    LimparPinoDonoDoMapa();
                }

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
                    _dataInicio = NormalizarDataInicio(passeio.DataInicio);
                    _historicoCaminhoPasseio.Clear();
                    _distanciaAcumuladaKm = 0;
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
                double distanciaAteDonoKm = DistanciaEmKm(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value);

                // Só libera o botão de iniciar quando o Petwalker estiver dentro do raio de 80m do Dono
                PodeIniciarPasseio = distanciaAteDonoKm <= DistanciaMaximaInicioKm;
                if (PodeIniciarPasseio)
                {
                    TextoBotaoIniciar = "Iniciar passeio";
                    TituloStatusDeslocamento = _ehPetwalker
                        ? "Você chegou! Peça o código ao dono"
                        : "Petwalker chegou ao local";
                }
                else
                {
                    int metrosRestantes = (int)Math.Round(distanciaAteDonoKm * 1000);
                    TextoBotaoIniciar = $"Chegue ao local ({metrosRestantes}m)";
                }

                bool saiuDaRota = _pontosRotaRestante.Count >= 2 &&
                                  DistanciaEmKm(latWalker.Value, lngWalker.Value, _pontosRotaRestante[0].Lat, _pontosRotaRestante[0].Lon) > 0.08;

                if (!_rotaInicialCalculada || saiuDaRota)
                {
                    await CalcularRotaEEstimativaAsync(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value, atualizarTempo: true);
                    _rotaInicialCalculada = true;
                }
                else if (_pontosRotaRestante.Count >= 2)
                {
                    ApararTrechoPercorrido(latWalker.Value, lngWalker.Value);
                    TempoPrincipalTexto = Math.Max(1, (int)Math.Ceiling((distanciaAteDonoKm / 4.5) * 60.0)).ToString();
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
            else if (IsEmAndamento)
            {
                // Registra continuamente o trajeto percorrido pelo Petwalker
                RegistrarPontoTrajeto(latWalker.Value, lngWalker.Value);
                DistanciaPercorridaTexto = _distanciaAcumuladaKm.ToString("0.0");

                if (_ehPetwalker)
                {
                    // Se faltam <= 5 min, exibe o pino do dono novamente e traça a rota de volta
                    if (_modoRetornoDono && _latDono.HasValue && _lngDono.HasValue)
                    {
                        AtualizarPinoDono(_latDono.Value, _lngDono.Value);

                        bool saiuDaRotaRetorno = _pontosRotaRestante.Count >= 2 &&
                                                 DistanciaEmKm(latWalker.Value, lngWalker.Value, _pontosRotaRestante[0].Lat, _pontosRotaRestante[0].Lon) > 0.08;

                        if (!_rotaRetornoCalculada || saiuDaRotaRetorno)
                        {
                            await CalcularRotaEEstimativaAsync(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value, atualizarTempo: false);
                            _rotaRetornoCalculada = true;
                            EnquadrarOrigemEDestino(latWalker.Value, lngWalker.Value, _latDono.Value, _lngDono.Value);
                        }
                        else if (_pontosRotaRestante.Count >= 2)
                        {
                            ApararTrechoPercorrido(latWalker.Value, lngWalker.Value);
                            Map.Navigator.CenterOn(pontoWalker);
                        }
                    }
                    else
                    {
                        LimparPinoDonoDoMapa();
                        Map.Navigator.CenterOn(pontoWalker);
                    }
                }
                else
                {
                    // Visão do Dono: mostra o caminho que o Petwalker está fazendo em tempo real
                    if (_historicoCaminhoPasseio.Count >= 2)
                    {
                        DesenharPolilinhaNoMapa(_historicoCaminhoPasseio);
                    }

                    Map.Navigator.CenterOn(pontoWalker);
                }
            }
        }

        private void RegistrarPontoTrajeto(double lat, double lng)
        {
            if (_historicoCaminhoPasseio.Count == 0)
            {
                _historicoCaminhoPasseio.Add((lng, lat));
                return;
            }

            var ultimo = _historicoCaminhoPasseio[^1];
            double trechoKm = DistanciaEmKm(ultimo.Lat, ultimo.Lon, lat, lng);

            // Ignora micro-ruídos menores que 4 metros, mas grava qualquer deslocamento real
            if (trechoKm >= 0.004)
            {
                _distanciaAcumuladaKm += trechoKm;
                _historicoCaminhoPasseio.Add((lng, lat));
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

        private async Task CalcularRotaEEstimativaAsync(double latOrigem, double lngOrigem, double latDestino, double lngDestino, bool atualizarTempo)
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
                        if (atualizarTempo)
                        {
                            double duracaoSegundos = rota.GetProperty("duration").GetDouble();
                            int minutosEstimados = Math.Max(1, (int)Math.Ceiling(duracaoSegundos / 60.0));
                            TempoPrincipalTexto = minutosEstimados.ToString();
                        }

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

            if (atualizarTempo)
            {
                double km = DistanciaEmKm(latOrigem, lngOrigem, latDestino, lngDestino);
                TempoPrincipalTexto = Math.Max(1, (int)Math.Ceiling((km / 4.5) * 60.0)).ToString();
            }

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

        private void LimparPinoDonoDoMapa()
        {
            _destinoDonoLayer.Features = new List<IFeature>();
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

            int minutos = restante.TotalSeconds <= 0
                ? 0
                : (int)Math.Ceiling(restante.TotalMinutes);

            int minutosClamp = Math.Clamp(minutos, 0, _duracaoMinutos);
            TempoPrincipalTexto = minutosClamp.ToString("0");

            // Aviso ao Petwalker quando faltarem 5 minutos para encerrar o passeio
            if (_ehPetwalker && !_avisoRetornoEnviado && minutosClamp <= 5 && restante.TotalSeconds > 0)
            {
                _avisoRetornoEnviado = true;
                _modoRetornoDono = true;
                _rotaRetornoCalculada = false;

                Mensagem = "Faltam 5 min! Retorne ao local do dono.";

                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(800));
                }
                catch { }

                _ = NotificarRetornoPetwalkerAsync();
            }
        }

        private async Task NotificarRetornoPetwalkerAsync()
        {
            try
            {
                await Shell.Current.DisplayAlertAsync(
                    "⏰ Faltam 5 minutos!",
                    "Faltam 5 minutos para acabar o passeio. Volte ao local do dono seguindo a rota traçada no mapa.",
                    "Entendi");

                await AtualizarPosicoesERotaAsync();
            }
            catch { }
        }

        [RelayCommand]
        private async Task AbrirMenuPasseio()
        {
            if (_ehPetwalker && IsEmAndamento)
                return;

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
            if (!PodeIniciarPasseio)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Longe do local",
                    "Você precisa chegar até o local do dono antes de iniciar o passeio.",
                    "OK");
                return;
            }

            string? codigoDigitado = await Shell.Current.DisplayPromptAsync(
                "Código de confirmação",
                "Solicite ao dono os 4 últimos dígitos do RGA do pet:",
                accept: "Iniciar",
                cancel: "Cancelar",
                placeholder: "4 dígitos",
                maxLength: 4,
                keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(codigoDigitado))
                return;

            if (!codigoDigitado.Trim().Equals(_codigoInicioEsperado, StringComparison.OrdinalIgnoreCase))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Código incorreto",
                    "O código digitado não confere com o RGA do pet. Confirme o número com o dono e tente novamente.",
                    "OK");
                return;
            }

            try
            {
                Carregando = true;
                string token = Preferences.Get("UsuarioToken", string.Empty);
                var passeioService = new PasseioService(token);

                await passeioService.IniciarAsync(IdPasseio);
                StatusAtual = "EmAndamento";
                _dataInicio = DateTime.Now;
                _historicoCaminhoPasseio.Clear();
                _distanciaAcumuladaKm = 0;
                _avisoRetornoEnviado = false;
                _modoRetornoDono = false;
                _rotaRetornoCalculada = false;

                AtualizarEstadoVisual();
                await AtualizarPosicoesERotaAsync();
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