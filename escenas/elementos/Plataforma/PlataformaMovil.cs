using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

namespace PrimerjuegoPlataformas2D.escenas.elementos.Plataforma;

[Tool]
public partial class PlataformaMovil : Plataforma
{
    [Export]
    public bool Movimiento { get; set; } = false;

    [Export]
    public bool Unidireccional { get; set; } = false;

    public Vector2 PosicionInicial { get; set; }
    private Vector2 _posicionMovimiento;

    [Export]
    public Vector2 PosicionA { get; set; } = Vector2.Zero;
    [Export]
    public Vector2 PosicionB { get; set; } = Vector2.Zero;

    [Export]
    public bool FrenarAlLlegar { get; set; } = true;
    [Export]
    public float DistanciaFrenado = 20f;
    [Export]
    public float VelocidadMaxima = 50f;
    [Export]
    public float Aceleracion = 30f;

    private Vector2 _posicionAnterior;
    private float _aceleracionActual;
    private bool _haciaFin = true;

    [Export]
    public bool Caida
    {
        get;
        set
        {
            field = value;
            if (!IsNodeReady() || Engine.IsEditorHint()) return;
            Callable.From(() => _sensorJugador.ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled).CallDeferred();
        }
    } = false;

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

    const int FramesEstadoRestaurando = 2;
    private int _framesEstadoRestaurandoActuales = 0;

    private float _timer = 0f;

    #region Animaciones
    private List<Tween> _tweensTemblor;
    private Vector2 _offsetTemblor = Vector2.Zero;
    #endregion

    private Area2D _sensorJugador;
    protected CollisionShape2D _collisionShape2DSensorJugador;

    public override void _Ready()
    {
        _posicionMovimiento = PosicionInicial = _posicionAnterior = Position;
        _sensorJugador = GetNode<Area2D>("SensorJugador");
        _collisionShape2DSensorJugador = GetNode<CollisionShape2D>("SensorJugador/CollisionShape2D");

        base._Ready();

        if (Engine.IsEditorHint())
            SetPhysicsProcess(false);

        Inicializar();
    }

    private void Inicializar()
    {
        Caida = Caida;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Movimiento horizontal independiente.
        GestionarMovimiento(delta);

        // Gestión del estado de la plataforma.
        GestionarEstado(delta);

        Vector2 posicionActual = GlobalPosition;
        _posicionAnterior = posicionActual;
    }

    private void GestionarMovimiento(double delta)
    {
        if (!Movimiento)
            return;

        Vector2 target = _haciaFin ? PosicionB : PosicionA;
        Vector2 direccion = new Vector2(target.X - _posicionMovimiento.X, target.Y - _posicionMovimiento.Y);
        float distancia = direccion.Length();

        if (distancia < 0.01f)
        {
            if (FrenarAlLlegar)
            {
                _aceleracionActual = 0f;
            }

            if (!Unidireccional)
            {
                _posicionMovimiento = target;
                _haciaFin = !_haciaFin;
            }
        }
        else
        {
            float velocidad;
            if (FrenarAlLlegar)
            {
                _aceleracionActual += Aceleracion * (float)delta;
                _aceleracionActual = Mathf.Min(_aceleracionActual, VelocidadMaxima);

                velocidad = _aceleracionActual;

                if (distancia < DistanciaFrenado)
                {
                    float t = Mathf.Clamp(distancia / DistanciaFrenado, 0f, 1f);
                    float factor = Mathf.SmoothStep(0f, 1f, t);
                    velocidad *= factor;
                    velocidad = Mathf.Max(velocidad, 10);
                }
            }
            else
            {
                velocidad = Aceleracion;
            }

            float movimiento = Mathf.Min(velocidad * (float)delta, distancia);
            _posicionMovimiento += new Vector2(Mathf.Sign(direccion.X) * movimiento, Mathf.Sign(direccion.Y) * movimiento);
        }

        // Solo aplicar X a Position si no está cayendo
        if (_estado != EstadoPlataforma.Cayendo &&
            _estado != EstadoPlataforma.Reiniciando &&
            _estado != EstadoPlataforma.Restaurando)
        {
            Position = _posicionMovimiento;
        }
    }

    private void GestionarEstado(double delta)
    {
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
        DetectarJugador();

        if (Unidireccional && (_posicionMovimiento - PosicionB).Length() < 0.01f)
        {
            _estado = EstadoPlataforma.Reiniciando;
            _collisionShape2D.Disabled = true;
        }
    }

    private void DetectarJugador()
    {
        if (!Caida)
            return;

        Array<Node2D> cuerposEnContacto = _sensorJugador.GetOverlappingBodies();
        if (cuerposEnContacto == null) return;

        foreach (var body in cuerposEnContacto)
        {
            if (body is Jugador jugador)
            {
                bool estaEncima = jugador.GlobalPosition.Y < GlobalPosition.Y;
                bool estaPosado = jugador.IsOnFloor();

                if (estaEncima && estaPosado)
                {
                    ActivarCaida();
                    return;
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

        ++_framesEstadoRestaurandoActuales;
        if (_framesEstadoRestaurandoActuales < FramesEstadoRestaurando)
            return;

        if (Unidireccional)
            _posicionMovimiento = PosicionA;

        Position = _posicionMovimiento;
        _timer = 0f;
        _estado = EstadoPlataforma.Restaurando;

        _framesEstadoRestaurandoActuales = 0;
    }

    private void GestionarEstadoRestaurando(double delta)
    {
        RestablecerSprite();

        // Volvemos a activar las colisiones.
        _collisionShape2D.Disabled = false;
        _estado = EstadoPlataforma.Normal;
    }

    private void IniciarAnimacionTemblor()
    {
        _animacionTemblorIniciado = true;

        _tweensTemblor?.ForEach(t => t.Kill());
        _tweensTemblor = new List<Tween>();

        Vector2 temblorDerecha = new Vector2(3, 0);
        Vector2 temblorIzquierda = new Vector2(-3, 0);
        float duracionTemblor = 0.05f;

        foreach (var sprite in _sprites)
        {
            var callable = Callable.From((Vector2 offset) => sprite.Offset = offset);

            var tweenTemblor = CreateTween().SetLoops();
            tweenTemblor.TweenMethod(callable, Vector2.Zero, temblorDerecha, duracionTemblor);
            tweenTemblor.TweenMethod(callable, temblorDerecha, temblorIzquierda, duracionTemblor);
            tweenTemblor.TweenMethod(callable, temblorIzquierda, Vector2.Zero, duracionTemblor);

            _tweensTemblor.Add(tweenTemblor);
        }
    }

    private void DetenerAnimacionTemblor()
    {
        _animacionTemblorIniciado = false;

        _tweensTemblor?.ForEach(t => t.Kill());
        _tweensTemblor = null;
        _sprites.ForEach(s => s.Offset = Vector2.Zero);
    }

    private void IniciarAnimacionDesvanecimiento()
    {
        _animacionDesvanecimientoIniciada = true;
        foreach (var sprite in _sprites)
        {
            var tween = CreateTween();
            tween.TweenProperty(sprite, "modulate:a", 0f, TiempoCayendo * 0.1f);
        }
    }

    private void RestablecerSprite()
    {
        _animacionDesvanecimientoIniciada = false;
        _sprites.ForEach(s => s.Modulate = Colors.White);
        _sprites.ForEach(s => s.Offset = Vector2.Zero);
    }

    protected override void ActualizarTamano()
    {
        if (!IsInsideTree())
            return;

        _collisionShape2DSensorJugador ??= GetNode<CollisionShape2D>("SensorJugador/CollisionShape2D");

        base.ActualizarTamano();

        ActualizarColisionSensorJugador();
    }

    private void ActualizarColisionSensorJugador()
    {
        if (_collisionShape2DSensorJugador == null)
            return;

        float tamano = TamanoTotal();
        var rect = new RectangleShape2D();
        rect.Size = new Vector2(tamano, _altoColision);

        _collisionShape2DSensorJugador.Position = new Vector2(0, -8.5f);
        _collisionShape2DSensorJugador.Shape = rect;
    }

}