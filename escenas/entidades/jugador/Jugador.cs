using Godot;
using PrimerjuegoPlataformas2D.escenas.pantalla1;

namespace PrimerjuegoPlataformas2D.escenas.entidades.jugador;

public partial class Jugador : CharacterBody2D
{
    /// <summary>
    /// Describe el estado físico vertical del jugador.
    /// </summary>
    public enum EstadoLocomocionJugador
    {
        EnSuelo,
        Saltando,
        Cayendo
    }

    public float VELOCIDAD = 130.0f;

    public float VELOCIDAD_SALTO = 300.0f;

    private int _coyoteFrames = 0;

    private const int MAX_COYOTE_FRAMES = 6; // ~0.1s si physics = 60Hz

    private AnimatedSprite2D _animatedSprite2D;

    public CollisionShape2D CollisionShape2D;

    public float Gravedad = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private SistemaPlataformas _sistemaPlataformas;

    public EstadoLocomocionJugador? EstadoLocomocionAnterior;
    private EstadoLocomocionJugador? _estadoLocomocion;

    public EstadoLocomocionJugador EstadoLocomocion
    {
        get => _estadoLocomocion ??= CalcularEstadoLocomocion();
        private set
        {
            if (_estadoLocomocion.HasValue && _estadoLocomocion.Value == value)
                return;

            EstadoLocomocionAnterior = _estadoLocomocion;
            _estadoLocomocion = value;

            OnEstadoLocomocionChanged(EstadoLocomocionAnterior, value);
        }
    }

    private void OnEstadoLocomocionChanged(EstadoLocomocionJugador? anterior, EstadoLocomocionJugador actual)
    {
        switch (actual)
        {
            case EstadoLocomocionJugador.EnSuelo:
                GD.Print("Jugador en el suelo.");
                break;

            case EstadoLocomocionJugador.Saltando:
                GD.Print("Jugador saltando.");
                break;

            case EstadoLocomocionJugador.Cayendo:
                GD.Print("Jugador cayendo.");
                break;
        }

        if (anterior == EstadoLocomocionJugador.Cayendo &&
            actual == EstadoLocomocionJugador.EnSuelo)
        {
            OnAterrizar();
        }
    }

    private void OnAterrizar()
    {
        GD.Print("Aterrizaje.");
    }


    private Vector2 _velocidadPlataformaAlSaltar = Vector2.Zero;

    public Jugador()
    {
        _sistemaPlataformas = new SistemaPlataformas(this);
        AddChild(_sistemaPlataformas);
    }

    public override void _Ready()
    {
        this._animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        this.CollisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        ActualizarCoyoteTime();

        EstadoLocomocion = CalcularEstadoLocomocion();

        Vector2 velocidad = Velocity;

        velocidad = AplicarGravedad(delta, velocidad);

        velocidad = GestionarSalto(delta, velocidad);

        velocidad = GestionarMovimiento(velocidad);

        this.Velocity = velocidad;

        MoveAndSlide();
    }

    private void ActualizarCoyoteTime()
    {
        if (IsOnFloor())
            _coyoteFrames = MAX_COYOTE_FRAMES;
        else if (_coyoteFrames > 0)
            _coyoteFrames--;
    }

    public EstadoLocomocionJugador CalcularEstadoLocomocion()
    {
        if (IsOnFloor())
        {
            return EstadoLocomocionJugador.EnSuelo;
        }
        else if (Velocity.Y < 0)
        {
            return EstadoLocomocionJugador.Saltando;
        }
        else
        {
            return EstadoLocomocionJugador.Cayendo;
        }
    }

    private Vector2 AplicarGravedad(double delta, Vector2 velocidad)
    {
        // Aplicamos gravedad al jugador si no está en el suelo.

        if (!IsOnFloor())
        {
            velocidad.Y += Gravedad * (float)delta;
        }

        return velocidad;
    }

    private Vector2 GestionarSalto(double delta, Vector2 velocidad)
    {
        if (Input.IsActionJustPressed("ui_accept") && _coyoteFrames > 0)
        {
            if (Input.IsActionPressed("ui_down"))
                velocidad = _sistemaPlataformas.AtravesarPlataformasDebajo(delta, velocidad);
            else
                velocidad.Y = -VELOCIDAD_SALTO;

            _coyoteFrames = 0;
        }

        return velocidad;
    }

    private Vector2 GestionarMovimiento(Vector2 velocidad)
    {
        float direccion = Input.GetAxis("ui_left", "ui_right");

        Vector2 velocidadJugador = new Vector2(velocidad.X, velocidad.Y);

        if (direccion != 0)
        {
            velocidadJugador.X = direccion * VELOCIDAD;
            _animatedSprite2D.FlipH = !(direccion > 0);
            velocidad = velocidadJugador;
        }
        else
        {
            velocidadJugador.X = Mathf.MoveToward(velocidadJugador.X, 0f, VELOCIDAD * 10);

            // Obtener plataforma predominante debajo
            PhysicsBody2D plataformaPredominante = _sistemaPlataformas.ObtenerPlataformaDebajoJugadorPredominante();
            Vector2 velocidadPlataforma = Vector2.Zero;

            if (IsOnFloor())
            {
                if (plataformaPredominante is PlataformaMovil plataforma)
                {
                    _velocidadPlataformaAlSaltar = plataforma.VelocidadActual;
                }
                else
                {
                    _velocidadPlataformaAlSaltar = Vector2.Zero;
                }
            }
            else
            {
                // En el aire, conservamos la velocidad de la plataforma del momento del salto
                velocidadPlataforma = _velocidadPlataformaAlSaltar;
            }

            // Velocidad final
            velocidad = velocidadJugador + velocidadPlataforma;
        }

        return velocidad;
    }
}