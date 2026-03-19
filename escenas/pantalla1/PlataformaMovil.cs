using System.Linq;
using Godot;
using Godot.Collections;
using PrimerjuegoPlataformas2D.escenas.entidades.jugador;

namespace PrimerjuegoPlataformas2D.escenas.pantalla1;

public partial class PlataformaMovil : Plataforma
{
    public Vector2 DeltaMovimiento { get; private set; }
    public Vector2 VelocidadActual { get; private set; }

    [Export]
    public bool Movimiento { get; set; } = false;

    public Vector2 PosicionInicial { get; set; }
    private Vector2 _posicionMovimiento;

    [Export]
    public Vector2 PosicionA { get; set; } = Vector2.Zero;
    [Export]
    public Vector2 PosicionB { get; set; } = Vector2.Zero;

    [Export]
    public float DistanciaFrenado = 20f;
    [Export]
    public float VelocidadMaxima = 50f;
    [Export]
    public float Aceleracion = 30f;

    private Vector2 _posicionAnterior;
    private float _aceleracionActual = 0f;
    private bool _haciaFin = true;

    [Export]
    public bool Caida { get; set; } = false;

    [Export]
    public float TiempoEsperaCaida = 1f; // tiempo antes de caer
    [Export]
    public float TiempoCayendo = 2f; // tiempo hasta reaparecer
    [Export]
    public float VelocidadCaida = 150f; // velocidad vertical al caer

    private bool _animacionTemblorIniciado = false;
    private bool _animacionDesvanecimientoIniciada = false;

    // --- Estados de la plataforma ---
    private enum EstadoPlataforma { Normal, EsperandoCaida, Cayendo, Reiniciando, Restaurando }
    private EstadoPlataforma _estado = EstadoPlataforma.Normal;

    private float _timer = 0f;

    #region Animaciones
    private Tween _tweenTemblor;
    private Vector2 _offsetTemblor = Vector2.Zero;
    #endregion

    private Area2D _sensorJugador;
    private CollisionShape2D _collisionShape2D;
    private Sprite2D _sprite;

    public override void _Ready()
    {
        _posicionMovimiento = PosicionInicial = _posicionAnterior = Position;
        _sensorJugador = GetNode<Area2D>("SensorJugador");
        _collisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
        _sprite = GetNode<Sprite2D>("Sprite2D"); // ajusta el nombre si es distinto

        if (Caida)
            _sensorJugador.BodyEntered += OnSensorJugadorBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Movimiento horizontal independiente
        GestionarMovimiento(delta);

        // Gestión de caída vertical según estado
        GestionarCaida(delta);

        // Guardar velocidad y delta
        Vector2 posicionActual = GlobalPosition;
        DeltaMovimiento = posicionActual - _posicionAnterior;
        VelocidadActual = DeltaMovimiento / (float)delta;
        _posicionAnterior = posicionActual;
    }

    private void GestionarMovimiento(double delta)
    {
        if (!Movimiento && (PosicionA == Vector2.Zero || PosicionB == Vector2.Zero))
            return;

        Vector2 target = _haciaFin ? PosicionB : PosicionA;
        // Usar _posicionMovimiento en lugar de Position
        Vector2 direccion = new Vector2(target.X - _posicionMovimiento.X, 0);
        float distancia = System.Math.Abs(direccion.X);

        if (distancia < 0.01f)
        {
            _posicionMovimiento = new Vector2(target.X, _posicionMovimiento.Y);
            _haciaFin = !_haciaFin;
            _aceleracionActual = 0f;
        }
        else
        {
            _aceleracionActual += Aceleracion * (float)delta;
            _aceleracionActual = Mathf.Min(_aceleracionActual, VelocidadMaxima);

            float velocidad = _aceleracionActual;

            if (distancia < DistanciaFrenado)
            {
                float t = Mathf.Clamp(distancia / DistanciaFrenado, 0f, 1f);
                float factor = Mathf.SmoothStep(0f, 1f, t);
                velocidad *= factor;
                velocidad = Mathf.Max(velocidad, 10);
            }

            float movimientoX = Mathf.Min(velocidad * (float)delta, distancia);
            _posicionMovimiento += new Vector2(Mathf.Sign(direccion.X) * movimientoX, 0);
        }

        // Solo aplicar X a Position si no está cayendo
        if (_estado != EstadoPlataforma.Cayendo &&
            _estado != EstadoPlataforma.Reiniciando &&
            _estado != EstadoPlataforma.Restaurando)
        {
            Position = new Vector2(_posicionMovimiento.X, Position.Y);
        }
    }

    private void GestionarCaida(double delta)
    {
        if (!Caida)
            return;

        switch (_estado)
        {
            case EstadoPlataforma.Normal:
                GestionarEstadoNormal(delta);
                break;

            case EstadoPlataforma.EsperandoCaida:
                GestionarEstadoEsperandoCaida(delta);
                break;

            case EstadoPlataforma.Cayendo:
                GestionarEstadoCayendo(delta);
                break;

            case EstadoPlataforma.Reiniciando:
                GestionarEstadoReiniciando(delta);
                break;


            case EstadoPlataforma.Restaurando:
                GestionarEstadoRestaurando(delta);
                break;
        }
    }

    private void GestionarEstadoNormal(double delta)
    {
        Array<Node2D> cuerposEnContacto = _sensorJugador.GetOverlappingBodies();
        if (cuerposEnContacto != null && cuerposEnContacto.Count > 0)
        {
            bool hayJugador = cuerposEnContacto.OfType<Jugador>().Any();
            if (hayJugador)
                ActivarCaida();
        }
    }

    private void GestionarEstadoEsperandoCaida(double delta)
    {
        _timer += (float)delta;
        if (_timer >= TiempoEsperaCaida)
        {
            DetenerAnimacionTemblor();
            _estado = EstadoPlataforma.Cayendo;
            _timer = 0f;
        }
        else if (_timer >= TiempoEsperaCaida * 0.6f && !_animacionTemblorIniciado)
        {
            IniciarAnimacionTemblor();
        }
    }

    private void GestionarEstadoCayendo(double delta)
    {
        _timer += (float)delta;
        Position += Vector2.Down * VelocidadCaida * (float)delta;

        if (_timer >= TiempoCayendo)
        {
            _estado = EstadoPlataforma.Reiniciando;
            _collisionShape2D.Disabled = true;
        }
        else if (_timer >= TiempoCayendo * 0.90f && !_animacionDesvanecimientoIniciada)
        {
            IniciarAnimacionDesvanecimiento();
        }
    }

    private void GestionarEstadoReiniciando(double delta)
    {
        Position = _posicionMovimiento;
        _timer = 0f;
        _estado = EstadoPlataforma.Restaurando;
    }

    private void GestionarEstadoRestaurando(double delta)
    {
        RestablecerSprite();
        // Volvemos a activar las colisiones.
        _collisionShape2D.Disabled = false;
        _estado = EstadoPlataforma.Normal;
    }

    private void OnSensorJugadorBodyEntered(Node2D body)
    {
        if (body is Jugador jugador)
        {
            if (jugador.IsOnFloor())
            {
                KinematicCollision2D collider = jugador.GetLastSlideCollision();
                if (collider != null && collider.GetCollider() == this)
                {
                    ActivarCaida();
                }
            }
        }
    }

    // Llamar cuando el jugador pisa la plataforma
    public void ActivarCaida()
    {
        if (_estado == EstadoPlataforma.Normal)
        {
            _estado = EstadoPlataforma.EsperandoCaida;
            _timer = 0f;
        }
    }

    private void IniciarAnimacionTemblor()
    {
        _animacionTemblorIniciado = true;

        _tweenTemblor?.Kill();
        _tweenTemblor = CreateTween().SetLoops();
        _tweenTemblor.TweenMethod(Callable.From((Vector2 offset) =>
        {
            _sprite.Offset = offset;
        }), Vector2.Zero, new Vector2(3, 0), 0.05f);
        _tweenTemblor.TweenMethod(Callable.From((Vector2 offset) =>
        {
            _sprite.Offset = offset;
        }), new Vector2(3, 0), new Vector2(-3, 0), 0.05f);
        _tweenTemblor.TweenMethod(Callable.From((Vector2 offset) =>
        {
            _sprite.Offset = offset;
        }), new Vector2(-3, 0), Vector2.Zero, 0.05f);
    }

    private void DetenerAnimacionTemblor()
    {
        _animacionTemblorIniciado = false;

        _tweenTemblor?.Kill();
        _tweenTemblor = null;
        _sprite.Offset = Vector2.Zero;
    }

    private void IniciarAnimacionDesvanecimiento()
    {
        _animacionDesvanecimientoIniciada = true;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate:a", 0f, TiempoCayendo * 0.1f);
    }

    private void RestablecerSprite()
    {
        _animacionDesvanecimientoIniciada = true;
        _sprite.Modulate = Colors.White;
        _sprite.Offset = Vector2.Zero;
    }
}