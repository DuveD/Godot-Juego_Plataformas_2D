using System;
using System.Threading.Tasks;
using Godot;
using PrimerjuegoPlataformas2D.escenas.elementos.proyectiles.Flecha;
using PrimerjuegoPlataformas2D.escenas.entidades.enemigos.Slime;
using PrimerjuegoPlataformas2D.nucleo.utilidades;
using BloqueRompible = PrimerjuegoPlataformas2D.escenas.bloques.BloqueRompible;
using Escalera = PrimerjuegoPlataformas2D.escenas.elementos.Escalera.Escalera;
using PuntoControl = PrimerjuegoPlataformas2D.escenas.elementos.PuntoControl.PuntoControl;

namespace PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

public partial class Jugador : CharacterBody2D
{
    #region Nodos
    public AnimatedSprite2D SpriteJugador;
    public Area2D SensorSuelo;
    private Camera2D _camera2D;
    public SistemaPlataformas SistemaPlataformas;
    public Area2D HitBox;
    #endregion

    #region Arco
    public Node2D Carcaj;
    public Sprite2D SpriteCarcaj;
    public Node2D Arco;
    public AnimatedSprite2D SpriteArco;

    [Export]
    public PackedScene PackedSceneFlecha;
    public Flecha _flecha;
    private Vector2 _direccionApuntado = Vector2.Right;
    #endregion

    #region Spawn
    [Export]
    public PuntoControl PuntoControl;
    #endregion

    #region Físicas

    private static float _gravedad = UtilidadesFisicas.ObtenerGravedad();

    /// <summary>
    /// Dirección: 1 = derecha, -1 = izquierda.
    /// </summary>
    private int _direccion = 1;

    [Export]
    public float VELOCIDAD = 130f;
    [Export]
    public float VELOCIDAD_SALTO = 320.0f;
    [Export]
    public float MAXIMA_VELOCIDAD_CAIDA = 350f;

    private const float ACELERACION_SUELO = 1000f;
    private const float ACELERACION_AIRE = 500f;

    private const float VELOCIDAD_ESCALAR = 80;
    #endregion

    #region Estado de locomoción
    /// <summary>
    /// Describe el estado físico vertical del jugador.
    /// </summary>
    public enum EstadoLocomocionJugador
    {
        EnSuelo, Saltando, Cayendo, Escalando
    }

    public EstadoLocomocionJugador EstadoLocomocionAnterior = EstadoLocomocionJugador.EnSuelo;
    public EstadoLocomocionJugador EstadoLocomocion = EstadoLocomocionJugador.EnSuelo;

    private int _framesEstadoTemporal = 0;

    bool _enSuelo = false;
    bool _enPared = false;
    #endregion

    #region Estado Accion
    public enum EstadoAccionJugador { Ninguno, AterrizajeFuerte, Atacando, Disparando }
    public EstadoAccionJugador EstadoAccion = EstadoAccionJugador.Ninguno;
    #endregion

    #region Salto
    private int _coyoteFrames = 0;
    private const int MAX_COYOTE_FRAMES = 10;

    private int _jumpBufferFrames = 0;
    private const int MAX_JUMP_BUFFER = 6;

    private const float GRAVEDAD_EXTRA_CORTE_SALTO = 2f;

    private const float UMBRAL_APEX = 40f;
    private const float MULTIPLICADOR_CONTROL_APEX = 1.5f;
    private const float MULTIPLICADOR_GRAVEDAD_APEX = 0.5f;
    #endregion

    #region Caída
    private const float MULTIPLICADOR_CAIDA = 1.8f;
    private const float MULTIPLICADOR_CAIDA_RAPIDA = 2.5f;

    private int _framesCaidaRapida = 0;
    private const int FRAMES_CAIDA_RAPIDA_MIN = 2;

    public bool? CaidaRapida
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;

            if (value != null)
            {
                if (value.Value)
                    GD.Print("Caída rápida.");
                else
                    GD.Print("Caida normal.");
            }
        }
    }

    private float _posYAlCaer = 0f;
    private const float ALTURA_MINIMA_ATERRIZAJE_FUERTE = 200f;
    #endregion

    #region Rodar
    [Export]
    public float VELOCIDAD_RODAR = 200f;

    private int _framesRodando = 0;
    private const int FRAMES_RODAR = 16;

    private bool _rodandoIniciado = false;
    private float _velocidadInicialRodar = 0f;

    public bool Rodando
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            if (value)
                OnRodando();
            else
                OnDejarRodar();
        }
    }
    #endregion

    #region Escalar
    private Escalera _escaleraActual = null;
    private bool _agarradoEscalera = false;

    private int _frameEscalera = 0;
    private float _distanciaEscalada = 0f;
    private bool _moviendoseEscaleraAnterior = false;
    private const float DISTANCIA_CAMBIO_FRAME_ESCALAR = 20f;
    #endregion 

    #region Inputs
    private struct InputJugador
    {
        public bool RodarPresionado;

        public bool SaltoPresionado;
        public bool Salto;


        public bool ArribaPresionado;
        public bool Arriba;
        public bool AbajoPresionado;
        public bool Abajo;
        public bool Izquierda;
        public bool Derecha;

        public bool DispararPresionado;
        public bool Disparar;

        /// <summary>
        /// H -> 1 = derecha, -1 = izquierda.<br/>
        /// V -> 1 = abajo, -1 = arriba.
        /// </summary>
        public readonly (int H, int V) Direccion
        {
            get
            {
                int h = (Derecha ? 1 : 0) - (Izquierda ? 1 : 0);
                int v = (Abajo ? 1 : 0) - (Arriba ? 1 : 0);
                return (h, v);
            }
        }
    }
    #endregion

    #region Muerte

    public bool Invulnerable = false;
    public bool DesactivarFisicas = false;

    private const float DISTANCIA_SUPERIOR_ANIMACION_MUERTE = 80f;
    private const float DISTANCIA_FINAL_ANIMACION_MUERTE = 600f;
    private const float VELOCIDAD_SUBIDA_MUERTE = 200f;
    private const float VELOCIDAD_BAJADA_MUERTE = 600f;
    #endregion

    public override void _Ready()
    {
        this.SpriteJugador = GetNode<AnimatedSprite2D>("SpriteJugador");
        this.SensorSuelo = GetNode<Area2D>("SensorSuelo");
        this._camera2D = GetNode<Camera2D>("Camera2D");
        this.HitBox = GetNode<Area2D>("HitBox");
        this.Carcaj = GetNode<Node2D>("Carcaj");
        this.SpriteCarcaj = GetNode<Sprite2D>("Carcaj/SpriteCarcaj");
        this.Arco = GetNode<Node2D>("Arco");
        this.SpriteArco = GetNode<AnimatedSprite2D>("Arco/SpriteArco");

        HitBox.BodyEntered += OnBodyEntered;

        this.SistemaPlataformas = new SistemaPlataformas(this);
        AddChild(SistemaPlataformas);

        RespawnEnPuntoSpawn();
    }

    public override void _PhysicsProcess(double delta)
    {
        // Si están desactivadas las físicas del jugador, no procesamos nada.
        if (DesactivarFisicas)
            return;

        Vector2 velocidad = Velocity;

        InputJugador inputJugador = ActualizarInputs();

        ProcesarDisparar(delta, inputJugador);

        velocidad = CalcularMovimientoHorizontal(delta, velocidad, inputJugador);
        velocidad = CalcularMovimientoVertical(delta, velocidad, inputJugador);

        Velocity = velocidad;

        MoveAndSlide();

        ComprobarContactoBloquesRompibles();

        EvaluarEstadoLocomocion();
        ActualizarAnimacion(inputJugador);
    }
    private float _tiempoDisparar = 0f;
    private const float TIEMPO_DISPARO = 1f; // segundos
    private bool _disparoPreparado = false;

    private void ProcesarDisparar(double delta, InputJugador inputJugador)
    {
        if (EstadoAccion != EstadoAccionJugador.Ninguno && EstadoAccion != EstadoAccionJugador.Disparando)
        {
            InterrumpirDisparo();
            return;
        }

        if (EstadoLocomocion == EstadoLocomocionJugador.Escalando || Rodando)
            return;

        ActualizarDireccionDisparo(inputJugador);

        if (inputJugador.DispararPresionado)
        {
            PrepararDisparo();
        }
        else if (inputJugador.Disparar)
        {
            _tiempoDisparar += (float)delta;

            if (_tiempoDisparar < TIEMPO_DISPARO)
            {
                int Frame = Mathf.Min((int)(_tiempoDisparar / TIEMPO_DISPARO * 4), 3);
                SpriteArco.Frame = Frame;
                if (Frame == 1 && !_disparoPreparado)
                {
                    _disparoPreparado = true;
                    GD.Print("Disparo preparado.");
                    _flecha?.Position = new Vector2(_flecha.Position.X - 1, _flecha.Position.Y);
                }
            }
        }
        else
        {
            if (_disparoPreparado)
            {
                Disparar(inputJugador);
            }
            else
            {
                InterrumpirDisparo();
            }
        }
    }
    private void ActualizarDireccionDisparo(InputJugador inputJugador)
    {
        if (inputJugador.Direccion.H != 0)
            _direccion = (int)inputJugador.Direccion.H;

        SpriteJugador.FlipH = _direccion < 0;

        float rotacion = 0f;
        if (inputJugador.Direccion.V == -1)
            rotacion = -40f;
        else if (inputJugador.Direccion.V == 1)
            rotacion = 40f;

        Arco.RotationDegrees = rotacion * _direccion;
        Arco.Scale = new Vector2(_direccion, 1);

        Carcaj.Scale = new Vector2(_direccion, 1);
    }

    private void Disparar(InputJugador inputJugador)
    {
        _disparoPreparado = false;

        if (_flecha != null)
        {
            Vector2 posicionGlobal = _flecha.GlobalPosition; // capturar antes de reparentar

            _flecha.GetParent().RemoveChild(_flecha);
            GetParent().AddChild(_flecha);

            _flecha.GlobalPosition = posicionGlobal; // restaurar la posición global

            float fuerzaDeDisparo = Mathf.Clamp(_tiempoDisparar / TIEMPO_DISPARO, 0.5f, 1.5f);
            _flecha.Disparar((_direccion, inputJugador.Direccion.V), fuerzaDeDisparo);
            _flecha = null;
        }

        SpriteJugador.Play();
    }

    private void PrepararDisparo()
    {
        EstadoAccion = EstadoAccionJugador.Disparando;

        Arco.Visible = true;
        SpriteCarcaj.Visible = true;

        _tiempoDisparar = 0f;
        _disparoPreparado = false;

        SpriteArco.Play("tensando");
        SpriteArco.Frame = 0;

        // Instanciar flecha
        _flecha = PackedSceneFlecha.Instantiate<Flecha>();
        Vector2 puntoDisparoLocal = new Vector2(8, SpriteArco.Position.Y);
        Arco.AddChild(_flecha);
        _flecha.Position = puntoDisparoLocal;

        SpriteJugador.Pause();
    }

    private void InterrumpirDisparo()
    {
        if (EstadoAccion == EstadoAccionJugador.Disparando)
            EstadoAccion = EstadoAccionJugador.Ninguno;

        Arco.Visible = false;
        SpriteCarcaj.Visible = false;

        _tiempoDisparar = 0f;
        _disparoPreparado = false;

        if (_flecha != null && !_flecha.Disparada)
        {
            _flecha.QueueFree();
            _flecha = null;
        }

        SpriteJugador.Play();
    }

    private void ComprobarContactoBloquesRompibles()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);

            if (collision.GetCollider() is BloqueRompible bloque)
            {
                bloque.TryBreak(this, collision.GetNormal());
            }
        }
    }

    private InputJugador ActualizarInputs()
    {
        if (EstadoAccion == EstadoAccionJugador.AterrizajeFuerte)
            return new InputJugador();

        InputJugador inputJugador = LeerInput();

        ActualizarCoyoteTime();
        ActualizarBufferDeSalto(inputJugador);

        return inputJugador;
    }

    private InputJugador LeerInput()
    {
        InputJugador inputJugador = new InputJugador
        {
            RodarPresionado = Input.IsActionJustPressed("rodar"),

            SaltoPresionado = Input.IsActionJustPressed("ui_accept"),
            Salto = Input.IsActionPressed("ui_accept"),

            ArribaPresionado = Input.IsActionJustPressed("ui_up"),
            Arriba = Input.IsActionPressed("ui_up"),
            AbajoPresionado = Input.IsActionJustPressed("ui_down"),
            Abajo = Input.IsActionPressed("ui_down"),
            Izquierda = Input.IsActionPressed("ui_left"),
            Derecha = Input.IsActionPressed("ui_right"),

            DispararPresionado = Input.IsActionJustPressed("disparar"),
            Disparar = Input.IsActionPressed("disparar")
        };

        return inputJugador;
    }

    private void ActualizarCoyoteTime()
    {
        if (_enSuelo)
            _coyoteFrames = MAX_COYOTE_FRAMES;
        else if (_coyoteFrames > 0)
            _coyoteFrames--;
    }

    private void ActualizarBufferDeSalto(InputJugador inputJugador)
    {
        if (inputJugador.SaltoPresionado)
            _jumpBufferFrames = MAX_JUMP_BUFFER;
        else if (_jumpBufferFrames > 0)
            _jumpBufferFrames--;
    }

    private Vector2 CalcularMovimientoVertical(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        // Intentar agarrarse
        (bool movimientoEscalera, Vector2 velocidadEscalera) = ProcesarMovimientoEscalera(delta, ref velocidad, inputJugador);
        if (movimientoEscalera)
            return velocidadEscalera;

        // Física normal
        velocidad = ProcesarSalto(delta, velocidad, inputJugador);
        velocidad = AplicarGravedad(delta, velocidad, inputJugador);

        if (velocidad.Y > MAXIMA_VELOCIDAD_CAIDA)
            velocidad.Y = MAXIMA_VELOCIDAD_CAIDA;

        return velocidad;
    }

    private (bool, Vector2) ProcesarMovimientoEscalera(double delta, ref Vector2 velocidad, InputJugador inputJugador)
    {
        if (EstadoAccion == EstadoAccionJugador.Disparando)
            return (false, default);

        if (_agarradoEscalera && _enSuelo)
        {
            _agarradoEscalera = false;
            GD.Print("Escalera soltada al aterrizar.");
        }

        velocidad = ProcesarAgarrarEscalera(velocidad, inputJugador);

        if (_agarradoEscalera && _escaleraActual != null)
        {
            (bool saltoEnEscalera, Vector2 velocidadSaltoEnEscalera) = ProcesarSaltoEnEscalera(ref velocidad);
            if (saltoEnEscalera)
                return (true, velocidadSaltoEnEscalera);

            float dirY = inputJugador.Arriba ? -1f : inputJugador.Abajo ? 1f : 0f;
            velocidad.Y = dirY * VELOCIDAD_ESCALAR;

            // Snap suave al centro X
            velocidad.X = Mathf.Clamp(
                (_escaleraActual.CentroX - GlobalPosition.X) / (float)delta,
                -200f, 200f
            );

            float movimiento = Mathf.Abs(velocidad.Y * (float)delta);
            if (movimiento > 0)
                _distanciaEscalada += movimiento;

            return (true, velocidad); // sin gravedad
        }

        return (false, default);
    }

    private Vector2 ProcesarAgarrarEscalera(Vector2 velocidad, InputJugador inputJugador)
    {
        if (_escaleraActual != null && !_agarradoEscalera && !Rodando)
        {
            if (inputJugador.ArribaPresionado || (inputJugador.AbajoPresionado && !_enSuelo))
            {
                _agarradoEscalera = true;
                _coyoteFrames = 0;
                velocidad = Vector2.Zero;
                _distanciaEscalada = 0f;
                _moviendoseEscaleraAnterior = false;
            }
        }

        return velocidad;
    }

    private (bool, Vector2) ProcesarSaltoEnEscalera(ref Vector2 velocidad)
    {
        if (HayInputSalto())
        {
            _agarradoEscalera = false;
            velocidad.Y = -VELOCIDAD_SALTO;
            _coyoteFrames = 0;
            _jumpBufferFrames = 0;

            return (true, velocidad);
        }
        else
        {
            return (false, default);
        }
    }

    private Vector2 CancelarSalto(Vector2 velocidad)
    {
        GD.Print("Salto interrumpido.");

        velocidad.Y = Mathf.Max(velocidad.Y, 0);
        return velocidad;
    }

    private Vector2 FrenarSalto(double delta, Vector2 velocidad)
    {
        GD.Print("Frenando salto.");

        velocidad.Y += _gravedad * GRAVEDAD_EXTRA_CORTE_SALTO * (float)delta;
        return velocidad;
    }

    private Vector2 AplicarGravedad(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        bool subiendo = velocidad.Y < 0;
        bool cayendo = velocidad.Y > 0;

        float gravedadAplicada = _gravedad;

        if (subiendo && Mathf.Abs(velocidad.Y) < UMBRAL_APEX)
        {
            gravedadAplicada *= MULTIPLICADOR_GRAVEDAD_APEX;
        }
        else if (cayendo)
        {
            ActualizarCaidaRapida(inputJugador);
            gravedadAplicada *= CaidaRapida == true ? MULTIPLICADOR_CAIDA_RAPIDA : MULTIPLICADOR_CAIDA;
        }

        velocidad.Y += gravedadAplicada * (float)delta;

        return velocidad;
    }

    private void ActualizarCaidaRapida(InputJugador inputJugador)
    {
        // Si estamos presionando ↓ y no estamos atravesando plataformas, activamos la caída rápida mínima
        if (inputJugador.Abajo && !SistemaPlataformas.AtravesandoPlataformas())
            _framesCaidaRapida = FRAMES_CAIDA_RAPIDA_MIN;

        // Activar la caída rápida mientras queden frames
        if (_framesCaidaRapida > 0)
        {
            CaidaRapida = true;
            _framesCaidaRapida--;
        }
        else
        {
            CaidaRapida = false;
        }
    }

    private bool PuedeSaltar()
    {
        return _coyoteFrames > 0;
    }

    private bool HayInputSalto()
    {
        return _jumpBufferFrames > 0;
    }

    private Vector2 ProcesarSalto(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        if (EstadoLocomocion != EstadoLocomocionJugador.Saltando)
        {
            if (HayInputSalto() && PuedeSaltar())
            {
                if (inputJugador.Abajo)
                {
                    if (SistemaPlataformas.HayPlataformasDebajo())
                    {
                        velocidad = SistemaPlataformas.AtravesarPlataformasDebajo(delta, velocidad);
                    }
                }
                else
                {
                    velocidad.Y = Mathf.Min(velocidad.Y, 0);
                    velocidad.Y -= VELOCIDAD_SALTO;
                }

                _coyoteFrames = 0;
                _jumpBufferFrames = 0;

                return velocidad;
            }
        }

        bool subiendo = velocidad.Y < 0;
        if (subiendo)
        {
            // Cancelar salto con ↓
            if (inputJugador.Abajo)
            {
                velocidad = CancelarSalto(velocidad);
            }
            // Jump cut: soltar salto antes de tiempo
            else if (!inputJugador.Salto)
            {
                velocidad = FrenarSalto(delta, velocidad);
            }
        }

        return velocidad;
    }

    private Vector2 CalcularMovimientoHorizontal(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        if (EstadoAccion == EstadoAccionJugador.Disparando)
        {
            // Mientras disparamos, si hay velocidad horizontal y estamos en el suelo, la decrecemos rápidamente hasta detenernos, pero no aplicamos aceleración ni control horizontal.
            if (Mathf.Abs(velocidad.X) > 0 && _enSuelo)
            {
                float desaceleracion = 1000f; // píxeles/segundo²
                velocidad.X = Mathf.MoveToward(velocidad.X, 0, desaceleracion * (float)delta);
            }

            return velocidad;
        }

        (bool rodando, Vector2 velocidadRodar) = ProcesarRodar(delta, velocidad, inputJugador);
        if (rodando)
            return velocidadRodar;

        velocidad = AplicarAceleracionHorizontal(delta, velocidad, inputJugador);

        ActualizarDireccion(velocidad);

        return velocidad;
    }

    private void ActualizarDireccion(Vector2 velocidad)
    {
        if (velocidad.X == 0)
            return;

        _direccion = Mathf.Sign(velocidad.X);
        SpriteJugador.FlipH = velocidad.X < 0;
    }


    private (bool, Vector2) ProcesarRodar(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        ActualizarRodar();

        if (_enSuelo)
        {
            // Si estamos en el suelo, queremos rodar y no estamos ya rodando, iniciamos el rodar.
            if (inputJugador.RodarPresionado && !Rodando)
                IniciarRodar(inputJugador);
        }

        if (Rodando)
            return (true, MantenerVelocidadRodar(delta, velocidad));
        else
            return (false, default);
    }

    private Vector2 MantenerVelocidadRodar(double delta, Vector2 velocidad)
    {
        if (_enSuelo)
        {
            // Si estamos en el suelo, mantenemos la velocidad de rodar todo el rato.
            velocidad.X = _velocidadInicialRodar;
        }
        else
        {
            // Si estamos en el aire, mantenemos la velocidad los frames de la animación restante y a partir de cero decrementamos.
            if (_framesRodando > -(FRAMES_RODAR * 2))
            {
                velocidad.X = _velocidadInicialRodar;
            }
            else
            {
                float deceleracion = 150f; // píxeles/segundo²
                velocidad.X = Mathf.MoveToward(velocidad.X, 0, deceleracion * (float)delta);
            }
        }

        return velocidad;
    }

    private void ActualizarRodar()
    {
        if (!Rodando)
            return;

        _framesRodando--;

        // Después del primer frame cambiamos a la animación loop
        if (_framesRodando < FRAMES_RODAR - 1)
            _rodandoIniciado = true;

        // El jugador es invulnerable mientras estemos en la primera mitad de los frames de Rodar.
        Invulnerable = _framesRodando >= (FRAMES_RODAR / 2);

        if (_framesRodando <= 0 && _enSuelo)
        {
            DejarDeRodar();
        }
    }

    private void IniciarRodar(InputJugador inputJugador)
    {
        Rodando = true;
        _framesRodando = FRAMES_RODAR;
        _rodandoIniciado = false;
        int direccion = inputJugador.Direccion.H != 0 ? inputJugador.Direccion.H : _direccion;
        _velocidadInicialRodar = direccion * VELOCIDAD_RODAR;
    }

    private void DejarDeRodar()
    {
        Rodando = false;
        _framesRodando = 0;
        _rodandoIniciado = false;
    }

    private void OnRodando()
    {
        GD.Print("Jugador rodando.");
    }

    private void OnDejarRodar()
    {
        GD.Print("Jugador dejar de rodar.");
    }

    private Vector2 AplicarAceleracionHorizontal(double delta, Vector2 velocidad, InputJugador inputJugador)
    {
        float aceleracion = _enSuelo ? ACELERACION_SUELO : ACELERACION_AIRE;

        if (EstadoLocomocion == EstadoLocomocionJugador.Saltando && Mathf.Abs(Velocity.Y) < UMBRAL_APEX)
            aceleracion *= MULTIPLICADOR_CONTROL_APEX;

        float objetivoX = inputJugador.Direccion.H * VELOCIDAD;

        if (!_enSuelo && Mathf.Abs(velocidad.X) > Mathf.Abs(objetivoX) &&
            Mathf.Sign(velocidad.X) == Mathf.Sign(objetivoX))
            return velocidad;

        velocidad.X = Mathf.MoveToward(velocidad.X, objetivoX, aceleracion * (float)delta);
        velocidad.X = Mathf.Clamp(velocidad.X, -VELOCIDAD, VELOCIDAD);

        return velocidad;
    }

    private void EvaluarEstadoLocomocion()
    {
        _enSuelo = IsOnFloor();
        _enPared = IsOnWall();

        // Reducimos contador si estamos en un estado temporal
        if (_framesEstadoTemporal > 0)
        {
            _framesEstadoTemporal--;

            return; // Mientras dure el estado temporal, no evaluamos otro cambio
        }

        var nuevoEstado = CalcularEstadoLocomocion();
        CambiarEstadoLocomocion(nuevoEstado);
    }

    public EstadoLocomocionJugador CalcularEstadoLocomocion()
    {
        if (_agarradoEscalera)
        {
            return EstadoLocomocionJugador.Escalando;
        }

        if (_enSuelo)
        {
            return EstadoLocomocionJugador.EnSuelo;
        }

        if (Velocity.Y < 0)
        {
            return EstadoLocomocionJugador.Saltando;
        }
        else
        {
            return EstadoLocomocionJugador.Cayendo;
        }
    }

    private void CambiarEstadoLocomocion(EstadoLocomocionJugador nuevoEstado)
    {
        if (EstadoLocomocion == nuevoEstado)
            return;

        var anterior = EstadoLocomocion;
        EstadoLocomocionAnterior = anterior;
        EstadoLocomocion = nuevoEstado;

        OnEstadoLocomocionChanged(anterior, nuevoEstado);
    }

    private void OnEstadoLocomocionChanged(EstadoLocomocionJugador anterior, EstadoLocomocionJugador actual)
    {
        // Cambios de estados compuestos.
        if (anterior == EstadoLocomocionJugador.Cayendo &&
            actual == EstadoLocomocionJugador.EnSuelo)
        {
            OnAterrizar();
        }
        else if (anterior == EstadoLocomocionJugador.EnSuelo &&
            actual == EstadoLocomocionJugador.Saltando)
        {
            OnDespegar();
        }
        // Cambios de estados simples.
        else
        {
            switch (actual)
            {
                case EstadoLocomocionJugador.EnSuelo:
                    OnEnSuelo();
                    break;

                case EstadoLocomocionJugador.Saltando:
                    OnSaltando();
                    break;

                case EstadoLocomocionJugador.Cayendo:
                    OnCayendo();
                    break;

                case EstadoLocomocionJugador.Escalando:
                    OnEscalando();
                    break;
            }
        }
    }

    private void OnAterrizar()
    {
        float alturaCaida = GlobalPosition.Y - _posYAlCaer;
        if (alturaCaida >= ALTURA_MINIMA_ATERRIZAJE_FUERTE)
            OnAterrizajeFuerte();
        else
            GD.Print("Aterrizaje.");

        OnEnSuelo();
    }

    private async void OnAterrizajeFuerte()
    {
        GD.Print("Aterrizaje fuerte.");

        EstadoAccion = EstadoAccionJugador.AterrizajeFuerte;

        ReproducirAnimacion(AnimacionJugador.AterrizajeFuerte);
        await ToSignal(SpriteJugador, AnimatedSprite2D.SignalName.AnimationFinished);

        EstadoAccion = EstadoAccionJugador.Ninguno;
    }

    private void OnDespegar()
    {
        GD.Print("Despegando.");
        OnSaltando();
    }

    private void OnEnSuelo()
    {
        CaidaRapida = null;
        _framesCaidaRapida = 0;

        GD.Print("Jugador en el suelo.");
    }

    private void OnSaltando()
    {
        GD.Print("Jugador saltando.");
    }

    private void OnCayendo()
    {
        GD.Print("Jugador cayendo.");

        _posYAlCaer = GlobalPosition.Y;
    }

    private void OnEscalando()
    {
        GD.Print("Jugador escalando.");
    }

    private void ActualizarAnimacion(InputJugador inputJugador)
    {
        // Mientras dure el estado temporal de AterrizajeFuerte, no permitimos que se interrumpa por cambios de locomoción normales, así que mientras dure no actualizamos la animación.
        if (EstadoAccion == EstadoAccionJugador.AterrizajeFuerte || EstadoAccion == EstadoAccionJugador.Disparando)
            return;

        if (Rodando)
        {
            ActualizarAnimacionRodando();
            return;
        }

        switch (EstadoLocomocion)
        {
            case EstadoLocomocionJugador.EnSuelo:
                ActualizarAnimacionEnSuelo(inputJugador);
                break;

            case EstadoLocomocionJugador.Saltando:
                ActualizarAnimacionSaltando();
                break;

            case EstadoLocomocionJugador.Cayendo:
                ActualizarAnimacionCayendo();
                break;

            case EstadoLocomocionJugador.Escalando:
                ActualizarAnimacionEscalando(inputJugador);
                break;
        }
    }

    private void ActualizarAnimacionEnSuelo(InputJugador inputJugador)
    {
        if (inputJugador.Direccion.H != 0 && !IsOnWall())
            ReproducirAnimacion(AnimacionJugador.Correr);
        else
            ReproducirAnimacion(AnimacionJugador.Idle);
    }

    private void ActualizarAnimacionSaltando()
    {
        ReproducirAnimacion(AnimacionJugador.Saltar);
    }

    private void ActualizarAnimacionCayendo()
    {
        ReproducirAnimacion(AnimacionJugador.Caer);
    }

    private void ActualizarAnimacionEscalando(InputJugador inputJugador)
    {
        bool moviendose = inputJugador.Arriba || inputJugador.Abajo;

        ReproducirAnimacion(AnimacionJugador.Escalando);

        if (moviendose)
        {
            // Al empezar a moverse, avanzar un frame inmediatamente
            if (!_moviendoseEscaleraAnterior)
            {
                _frameEscalera = 1 - SpriteJugador.Frame;
                _distanciaEscalada = 0f; // resetear para que el siguiente cambio sea limpio
            }
            else
            {
                _distanciaEscalada += Mathf.Abs(Velocity.Y * (float)GetPhysicsProcessDeltaTime());

                if (_distanciaEscalada >= DISTANCIA_CAMBIO_FRAME_ESCALAR)
                {
                    _distanciaEscalada -= DISTANCIA_CAMBIO_FRAME_ESCALAR;
                    _frameEscalera = 1 - _frameEscalera;
                }
            }

            SpriteJugador.Frame = _frameEscalera;
        }

        _moviendoseEscaleraAnterior = moviendose;
    }

    private void ActualizarAnimacionRodando()
    {
        if (_rodandoIniciado)
            ReproducirAnimacion(AnimacionJugador.Rodando, true);
        else
            ReproducirAnimacion(AnimacionJugador.Rodar, true);
    }

    private void ReproducirAnimacion(AnimacionJugador animacion, bool forzarReproducir = false)
    {
        if (SpriteJugador.Animation == animacion.Nombre && !forzarReproducir)
            return;

        SpriteJugador.Play(animacion.Nombre);
    }

    public async void Muerte()
    {
        if (DesactivarFisicas)
            return;

        MarcarComoMuerto();
        await EjecutarAnimacionMuerte();
        Revivir();
    }

    private void MarcarComoMuerto()
    {
        DesactivarFisicas = true;
        Velocity = Vector2.Zero;
        DejarDeRodar();
    }

    private async Task EjecutarAnimacionMuerte()
    {
        // Reparentamos la cámara al escenario para mantener su posición durante la animación.
        Node padreJugador = GetParent();
        Vector2 posicionGlobalCamara = _camera2D.GlobalPosition;
        _camera2D.Reparent(padreJugador);
        _camera2D.GlobalPosition = posicionGlobalCamara;

        ReproducirAnimacion(AnimacionJugador.MuerteEnCaida);

        await ToSignal(GetTree().CreateTimer(0.5f), Timer.SignalName.Timeout);

        Tween tween = AnimacionMuerte();
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private Tween AnimacionMuerte()
    {
        Vector2 posicionInicial = Position;
        Vector2 posicionApex = posicionInicial - new Vector2(0, DISTANCIA_SUPERIOR_ANIMACION_MUERTE);
        Vector2 posicionFinal = posicionInicial + new Vector2(0, DISTANCIA_FINAL_ANIMACION_MUERTE);

        float duracionSubida = DISTANCIA_SUPERIOR_ANIMACION_MUERTE / VELOCIDAD_SUBIDA_MUERTE;
        float duracionBajada = DISTANCIA_FINAL_ANIMACION_MUERTE / VELOCIDAD_BAJADA_MUERTE;

        Tween tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Idle);

        tween.TweenProperty(this, "position", posicionApex, duracionSubida)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Quad);

        tween.TweenProperty(this, "position", posicionFinal, duracionBajada)
             .SetEase(Tween.EaseType.In)
             .SetTrans(Tween.TransitionType.Quad);

        return tween;
    }

    private void Revivir()
    {
        RespawnEnPuntoSpawn();

        // Devolvemos la cámara al jugador, ahora ya en el punto de spawn.
        _camera2D.Reparent(this);
        _camera2D.Position = Vector2.Zero;

        // Devolvemos el estado de muerto a false.
        DesactivarFisicas = false;
    }

    private void RespawnEnPuntoSpawn()
    {
        if (PuntoControl == null)
            return;

        // Movemos el jugador al últimpo punto de Spawn.
        this.Position = PuntoControl.GlobalPosition;
        this._direccion = PuntoControl.Direccion;
        SpriteJugador.FlipH = _direccion < 0;
        _posYAlCaer = GlobalPosition.Y;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is Slime)
        {
            OnBodyEnteredSlime();
        }
    }

    private void OnBodyEnteredSlime()
    {
        if (!Invulnerable)
            this.Muerte();
    }

    public void EntrarZonaEscalera(Escalera escalera)
    {
        if (escalera == null)
            return;

        _escaleraActual = escalera;
    }

    public void SalirZonaEscalera(Escalera escalera)
    {
        if (escalera == null)
            return;

        if (_escaleraActual != escalera)
            return;

        _escaleraActual = null;
        _agarradoEscalera = false;
    }
}