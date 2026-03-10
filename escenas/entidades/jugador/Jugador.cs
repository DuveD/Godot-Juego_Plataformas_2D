using System;
using Godot;

namespace PrimerjuegoPlataformas2D.escenas.entidades.jugador;

public partial class Jugador : CharacterBody2D
{
    public const string NOMBRE_ANIMACION_ATERRIZAR = "aterrizar";
    public const string NOMBRE_ANIMACION_CAER = "caer";
    public const string NOMBRE_ANIMACION_CORRER = "correr";
    public const string NOMBRE_ANIMACION_GOLPEADO = "golpeado";
    public const string NOMBRE_ANIMACION_IDLE = "idle";
    public const string NOMBRE_ANIMACION_MUERTE = "muerte";
    public const string NOMBRE_ANIMACION_RODAR = "rodar";
    public const string NOMBRE_ANIMACION_RODANDO = "rodando";
    public const string NOMBRE_ANIMACION_SALTAR = "saltar";

    /// <summary>
    /// Describe el estado físico vertical del jugador.
    /// </summary>
    public enum EstadoLocomocionJugador
    {
        EnSuelo,
        Saltando,
        Cayendo,
        Rodando
    }

    private int _framesEstadoTemporal = 0;


    [Export]
    public float VELOCIDAD = 130f;

    [Export]
    public float VELOCIDAD_SALTO = 320.0f;

    [Export]
    public float MAXIMA_VELOCIDAD_CAIDA = 350f;

    private const float MULTIPLICADOR_CAIDA = 1.8f;
    private const float MULTIPLICADOR_CAIDA_RAPIDA = 2.5f;

    private int _coyoteFrames = 0;
    private const int MAX_COYOTE_FRAMES = 10;

    private int _jumpBufferFrames = 0;
    private const int MAX_JUMP_BUFFER = 6;

    private const float ACELERACION_SUELO = 1000f;
    private const float ACELERACION_AIRE = 500f;

    private const float GRAVEDAD_EXTRA_CORTE_SALTO = 2f;

    private const float UMBRAL_APEX = 40f;
    private const float MULTIPLICADOR_CONTROL_APEX = 1.5f;
    private const float MULTIPLICADOR_GRAVEDAD_APEX = 0.5f;

    private int _framesCaidaRapida = 0;
    private const int FRAMES_CAIDA_RAPIDA_MIN = 2;

    private const int FRAMES_RODAR = 16;          // duración del rodar

    private bool _mantenerRodar = false;

    [Export]
    public float VELOCIDAD_RODAR = 200; // velocidad horizontal durante el rodar

    private float _velocidadInicialRodar = 0f;

    private AnimatedSprite2D _animatedSprite2D;
    public CollisionShape2D CollisionShape2D;
    public Area2D SensorSuelo;
    public float Gravedad = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    public SistemaPlataformas SistemaPlataformas;
    public EstadoLocomocionJugador EstadoLocomocionAnterior = EstadoLocomocionJugador.EnSuelo;
    public EstadoLocomocionJugador EstadoLocomocion = EstadoLocomocionJugador.EnSuelo;

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

    public override void _Ready()
    {
        this._animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        this.CollisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        this.SensorSuelo = GetNode<Area2D>("SensorSuelo");

        this.SistemaPlataformas = new SistemaPlataformas(this);
        AddChild(SistemaPlataformas);

        EvaluarEstadoLocomocion();
        this.ActualizarAnimacion();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocidad = Velocity;

        ActualizarInputs();

        velocidad = GestionarMovimientoHorizontal(delta, velocidad);
        velocidad = GestionarMovimientoVertical(delta, velocidad);

        Velocity = velocidad;

        MoveAndSlide();

        EvaluarEstadoLocomocion();
        ActualizarAnimacion();
    }

    private void ActualizarInputs()
    {
        ActualizarCoyoteTime();
        ActualizarBufferDeSalto();
    }

    private void ActualizarCoyoteTime()
    {
        if (IsOnFloor())
            _coyoteFrames = MAX_COYOTE_FRAMES;
        else if (_coyoteFrames > 0)
            _coyoteFrames--;
    }

    private void ActualizarBufferDeSalto()
    {
        if (Input.IsActionJustPressed("ui_accept"))
            _jumpBufferFrames = MAX_JUMP_BUFFER;
        else if (_jumpBufferFrames > 0)
            _jumpBufferFrames--;
    }

    public EstadoLocomocionJugador CalcularEstadoLocomocion()
    {
        if (IsOnFloor())
        {
            return EstadoLocomocionJugador.EnSuelo;
        }

        if (Velocity.Y < 0)
        {
            return EstadoLocomocionJugador.Saltando;
        }

        return EstadoLocomocionJugador.Cayendo;
    }

    private Vector2 GestionarMovimientoVertical(double delta, Vector2 velocidad)
    {
        // Procesamos el salto.
        velocidad = ProcesarSalto(delta, velocidad);

        // Al procesar el salto, la velocidad puede quedar en positivo por la frenada.
        bool cayendo = velocidad.Y > 0;
        if (cayendo && EstadoLocomocion != EstadoLocomocionJugador.Cayendo)
            CambiarEstadoLocomocion(EstadoLocomocionJugador.Cayendo);

        // Aplicamos la gravedad.
        velocidad = AplicarGravedad(delta, velocidad);

        // Limitamos la velocidad máxima de caída.
        if (velocidad.Y > MAXIMA_VELOCIDAD_CAIDA)
            velocidad.Y = MAXIMA_VELOCIDAD_CAIDA;

        return velocidad;
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
        velocidad.Y += Gravedad * GRAVEDAD_EXTRA_CORTE_SALTO * (float)delta;
        return velocidad;
    }

    private Vector2 AplicarGravedad(double delta, Vector2 velocidad)
    {
        bool subiendo = velocidad.Y < 0;
        bool cayendo = velocidad.Y > 0;

        float gravedadAplicada = Gravedad;

        if (subiendo && Mathf.Abs(velocidad.Y) < UMBRAL_APEX)
        {
            gravedadAplicada *= MULTIPLICADOR_GRAVEDAD_APEX;
        }
        else if (cayendo)
        {
            ActualizarCaidaRapida();
            gravedadAplicada *= CaidaRapida == true ? MULTIPLICADOR_CAIDA_RAPIDA : MULTIPLICADOR_CAIDA;
        }

        velocidad.Y += gravedadAplicada * (float)delta;

        return velocidad;
    }

    private void ActualizarCaidaRapida()
    {
        // Si estamos presionando ↓ y no estamos atravesando plataformas, activamos la caída rápida mínima
        if (Input.IsActionPressed("ui_down") && !SistemaPlataformas.AtravesandoPlataformas())
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

    private Vector2 ProcesarSalto(double delta, Vector2 velocidad)
    {
        if (EstadoLocomocion != EstadoLocomocionJugador.Saltando)
        {
            if (HayInputSalto() && PuedeSaltar())
            {
                if (Input.IsActionPressed("ui_down"))
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
            if (Input.IsActionJustPressed("ui_down"))
            {
                velocidad = CancelarSalto(velocidad);
            }
            // Jump cut: soltar salto antes de tiempo
            else if (!Input.IsActionPressed("ui_accept"))
            {
                velocidad = FrenarSalto(delta, velocidad);
            }
        }

        return velocidad;
    }

    private Vector2 GestionarMovimientoHorizontal(double delta, Vector2 velocidad)
    {
        ProcesarRodar();

        if (_mantenerRodar)
            return MantenerVelocidadRodar(velocidad);

        float direccion = Input.GetAxis("ui_left", "ui_right");
        velocidad = AplicarAceleracionHorizontal(velocidad, direccion, delta);

        if (direccion != 0)
            _animatedSprite2D.FlipH = direccion < 0;

        return velocidad;
    }

    private void ProcesarRodar()
    {
        if (!IsOnFloor() || !Input.IsActionJustPressed("rodar"))
            return;

        if (EstadoLocomocion != EstadoLocomocionJugador.Rodando)
            IniciarRodar();
        else
            CambiarDireccionRodar();
    }

    private void IniciarRodar()
    {
        CambiarEstadoLocomocion(EstadoLocomocionJugador.Rodando);
        _velocidadInicialRodar = ObtenerDireccionActual() * VELOCIDAD_RODAR;
        _framesEstadoTemporal = FRAMES_RODAR;
    }

    private void CambiarDireccionRodar()
    {
        float direccionInput = Input.GetAxis("ui_left", "ui_right");
        float direccionActual = Mathf.Sign(_velocidadInicialRodar);

        if (Mathf.Sign(direccionInput) != 0 && Mathf.Sign(direccionInput) != direccionActual)
        {
            _animatedSprite2D.FlipH = direccionInput < 0;
            _velocidadInicialRodar = Mathf.Sign(direccionInput) * VELOCIDAD_RODAR;
        }

        _framesEstadoTemporal = FRAMES_RODAR;
    }

    private float ObtenerDireccionActual()
    {
        return _animatedSprite2D.FlipH ? -1f : 1f;
    }

    private Vector2 MantenerVelocidadRodar(Vector2 velocidad)
    {
        velocidad.X = _velocidadInicialRodar;
        return velocidad;
    }

    private Vector2 AplicarAceleracionHorizontal(Vector2 velocidad, float direccion, double delta)
    {
        float aceleracion = IsOnFloor() ? ACELERACION_SUELO : ACELERACION_AIRE;

        if (EstadoLocomocion == EstadoLocomocionJugador.Saltando && Mathf.Abs(Velocity.Y) < UMBRAL_APEX)
            aceleracion *= MULTIPLICADOR_CONTROL_APEX;

        float objetivoX = direccion * VELOCIDAD;

        if (!IsOnFloor() && Mathf.Abs(velocidad.X) > Mathf.Abs(objetivoX) &&
            Mathf.Sign(velocidad.X) == Mathf.Sign(objetivoX))
            return velocidad;

        velocidad.X = Mathf.MoveToward(velocidad.X, objetivoX, aceleracion * (float)delta);
        velocidad.X = Mathf.Clamp(velocidad.X, -VELOCIDAD, VELOCIDAD);

        return velocidad;
    }

    private void CambiarEstadoLocomocion(EstadoLocomocionJugador nuevoEstado)
    {
        EstadoLocomocionAnterior = EstadoLocomocion;

        if (EstadoLocomocion == nuevoEstado)
            return;

        EstadoLocomocion = nuevoEstado;

        OnEstadoLocomocionChanged(EstadoLocomocionAnterior, nuevoEstado);
    }

    private void EvaluarEstadoLocomocion()
    {
        // Reducimos contador si estamos en un estado temporal
        if (_framesEstadoTemporal > 0)
        {
            _framesEstadoTemporal--;

            return; // Mientras dure el estado temporal, no evaluamos otro cambio
        }

        var nuevoEstado = CalcularEstadoLocomocion();
        CambiarEstadoLocomocion(nuevoEstado);
    }

    private void OnEstadoLocomocionChanged(EstadoLocomocionJugador anterior, EstadoLocomocionJugador actual)
    {
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

                case EstadoLocomocionJugador.Rodando:
                    OnRodando();
                    break;
            }
        }
    }

    private void OnAterrizar()
    {
        GD.Print("Aterrizando.");
        OnEnSuelo();
    }

    private void OnDespegar()
    {
        GD.Print("Despegando.");
        OnSaltando();
    }

    private void OnEnSuelo()
    {
        _mantenerRodar = false;

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
    }

    private void OnRodando()
    {
        GD.Print("Jugador rodando.");
        _mantenerRodar = true;
    }

    private void ActualizarAnimacion()
    {
        switch (EstadoLocomocion)
        {
            case EstadoLocomocionJugador.EnSuelo:
                ReproducirAnimacion(Mathf.Abs(Velocity.X) > 5f ? NOMBRE_ANIMACION_CORRER : NOMBRE_ANIMACION_IDLE);
                break;

            case EstadoLocomocionJugador.Saltando:
                if (_mantenerRodar)
                    ReproducirAnimacion(NOMBRE_ANIMACION_RODANDO, true);
                else
                    ReproducirAnimacion(NOMBRE_ANIMACION_SALTAR);
                break;

            case EstadoLocomocionJugador.Cayendo:
                if (_mantenerRodar)
                    ReproducirAnimacion(NOMBRE_ANIMACION_RODANDO, true);
                else
                    ReproducirAnimacion(NOMBRE_ANIMACION_CAER);
                break;

            case EstadoLocomocionJugador.Rodando:
                if (_mantenerRodar)
                    ReproducirAnimacion(NOMBRE_ANIMACION_RODANDO, true);
                else
                    ReproducirAnimacion(NOMBRE_ANIMACION_RODAR, true);
                break;
        }
    }

    private void ReproducirAnimacion(string nombreAnimacion, bool forzarReproducir = false)
    {
        if (_animatedSprite2D.Animation == nombreAnimacion && !forzarReproducir)
            return;

        _animatedSprite2D.Play(nombreAnimacion);
    }
}