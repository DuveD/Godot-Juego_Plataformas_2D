
using Godot;
using PrimerjuegoPlataformas2D.escenas.elementos.proyectiles.Flecha;
using static PrimerjuegoPlataformas2D.escenas.entidades.Jugador.Jugador;

namespace PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

public partial class SistemaArco : Node
{
    private Flecha _flecha;
    private Vector2 _direccionApuntado = Vector2.Right;

    private float _tiempoDisparar = 0f;
    private const float TIEMPO_DISPARO = 1f; // segundos
    private bool _disparoPreparado = false;

    private Jugador _jugador;

    private AnimatedSprite2D _spriteJugador => _jugador?.SpriteJugador;

    private Node2D _carcaj => _jugador?.Carcaj;

    private Node2D _arco => _jugador?.Arco;

    private AnimatedSprite2D _spriteArco => _jugador?.SpriteArco;

    private Sprite2D _spriteCarcaj => _jugador?.SpriteCarcaj;

    private PackedScene _packedSceneFlecha => _jugador.PackedSceneFlecha;

    private SistemaArco(Jugador jugador)
    {
        this._jugador = jugador;
    }

    public static SistemaArco Inicializar(Jugador jugador)
    {
        var sistema = new SistemaArco(jugador);
        jugador.AddChild(sistema);
        return sistema;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_jugador.DesactivarFisicasYControles)
            return;

        InputJugador inputJugador = _jugador.InputJugadorActual;
        ProcesarDisparar(delta, inputJugador);
    }

    public void ProcesarDisparar(double delta, InputJugador inputJugador)
    {
        if (_jugador.EstadoAccion != EstadoAccionJugador.Ninguno && _jugador.EstadoAccion != EstadoAccionJugador.Disparando)
        {
            InterrumpirDisparo();
            return;
        }

        if (_jugador.EstadoLocomocion == EstadoLocomocionJugador.Escalando || _jugador.Rodando)
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
                ActualizarAnimacionDisparo();
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

    private void ActualizarAnimacionDisparo()
    {
        int framesAnimacion = _spriteArco.SpriteFrames.GetFrameCount("tensando");
        int Frame = Mathf.Min((int)(_tiempoDisparar / TIEMPO_DISPARO * framesAnimacion), framesAnimacion - 1);
        _spriteArco.Frame = Frame;
        if (Frame == 1 && !_disparoPreparado)
        {
            _disparoPreparado = true;
            GD.Print("Disparo preparado.");
            _flecha?.Position = new Vector2(_flecha.Position.X - 1, _flecha.Position.Y);
        }
    }

    private void ActualizarDireccionDisparo(InputJugador inputJugador)
    {
        _jugador.ActualizarDireccion(inputJugador.Direccion.H);

        float rotacion = 0f;
        if (inputJugador.Direccion.V == -1)
            rotacion = -40f;
        else if (inputJugador.Direccion.V == 1)
            rotacion = 40f;

        _arco.RotationDegrees = rotacion * _jugador.Direccion;
        _arco.Scale = new Vector2(_jugador.Direccion, 1);

        _carcaj.Scale = new Vector2(_jugador.Direccion, 1);
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
            _flecha.Disparar((_jugador.Direccion, inputJugador.Direccion.V), fuerzaDeDisparo);
            _flecha = null;
        }

        _jugador.ReanudarAnimacion();
    }

    private void PrepararDisparo()
    {
        _jugador.EstadoAccion = EstadoAccionJugador.Disparando;

        _arco.Visible = true;
        _spriteCarcaj.Visible = true;

        _tiempoDisparar = 0f;
        _disparoPreparado = false;

        _spriteArco.Play("tensando");
        _spriteArco.Frame = 0;

        // Instanciar flecha
        _flecha = _packedSceneFlecha.Instantiate<Flecha>();
        Vector2 puntoDisparoLocal = new Vector2(8, _spriteArco.Position.Y);
        _arco.AddChild(_flecha);
        _flecha.Position = puntoDisparoLocal;

        _jugador.PausarAnimacion();
    }

    public void InterrumpirDisparo()
    {
        if (_jugador.EstadoAccion == EstadoAccionJugador.Disparando)
            _jugador.EstadoAccion = EstadoAccionJugador.Ninguno;

        _arco.Visible = false;
        _spriteCarcaj.Visible = false;

        _tiempoDisparar = 0f;
        _disparoPreparado = false;

        if (_flecha != null && !_flecha.Disparada)
        {
            _flecha.QueueFree();
            _flecha = null;
        }

        _jugador.ReanudarAnimacion();
    }
}