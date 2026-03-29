using Godot;
using PrimerjuegoPlataformas2D.escenas.entidades.jugador;

namespace PrimerjuegoPlataformas2D.escenas.pantalla1;

public partial class BloqueRompible : StaticBody2D
{
    private bool _roto = false;
    private bool _animando = false;
    private Sprite2D[] _fragmentos;
    private Vector2[] _velocidades;
    private float _tiempoVida = 0f;

    [Export]
    public float Velocidad = 20;
    [Export]
    public float DuracionCaida = 0.6f;
    [Export]
    public float Gravedad = 600f;

    private readonly Vector2[] _fragOffsets = {
        new(-1f, -1f),
        new( 1f, -1f),
        new(-1f,  1f),
        new( 1f,  1f),
    };

    public override void _Ready()
    {
        _fragmentos = new[]
        {
            GetNode<Sprite2D>("FragmentoArribaIzquierda"),
            GetNode<Sprite2D>("FragmentoArribaDerecha"),
            GetNode<Sprite2D>("FragmentoAbajoIzquierda"),
            GetNode<Sprite2D>("FragmentoAbajoDerecha"),
        };
        _velocidades = new Vector2[4];
    }

    public void TryBreak(Jugador jugador, Vector2 normal)
    {
        if (_roto) return;

        bool impactoLateral = Mathf.Abs(normal.Y) < Mathf.Abs(normal.X) && jugador.Rodando;
        bool impactoDesdeArriba = normal.Y > 0f && jugador.Rodando;
        bool impactoVertical = Mathf.Abs(normal.Y) > Mathf.Abs(normal.X);
        bool jugadorPorDebajo = jugador.GlobalPosition.Y > GlobalPosition.Y + 14f;
        bool impactoDesdeAbajo = jugadorPorDebajo && impactoVertical;

        if (!impactoLateral && !impactoDesdeArriba && !impactoDesdeAbajo) return;

        BreakApart(-normal, impactoLateral ? Velocidad * 2 : Velocidad);
    }


    public override void _Process(double delta)
    {
        if (!_animando) return;

        float dt = (float)delta;
        _tiempoVida += dt;

        for (int i = 0; i < 4; i++)
        {
            // Aplica gravedad a la velocidad vertical
            _velocidades[i].Y += Gravedad * dt;

            // Mueve el fragmento
            _fragmentos[i].Position += _velocidades[i] * dt;

            // Fade al final
            float progreso = _tiempoVida / DuracionCaida;
            if (progreso > 0.5f)
            {
                _fragmentos[i].Modulate = _fragmentos[i].Modulate with
                {
                    A = Mathf.Lerp(1f, 0f, (progreso - 0.5f) / 0.5f)
                };
            }
        }

        if (_tiempoVida >= DuracionCaida)
            QueueFree();
    }

    public void BreakApart(Vector2 direction, float velocidad)
    {
        if (_roto) return;
        _roto = true;

        CollisionLayer = 0;
        CollisionMask = 0;
        this.SetDeferred("monitoring", false);

        Tween tween = CreateTween();
        tween.SetParallel(true);
        foreach (var fragmento in _fragmentos)
        {
            tween.TweenProperty(fragmento, "scale", Vector2.One * 0.4f, DuracionCaida)
                .SetTrans(Tween.TransitionType.Cubic)
                .SetEase(Tween.EaseType.Out);
        }

        float velocidadInicial = velocidad * 5f;

        // Ángulo de la dirección del golpe
        float angulo = direction.Angle();

        for (int i = 0; i < 4; i++)
        {
            // Rotamos el offset según la dirección del impacto
            Vector2 offsetRotado = _fragOffsets[i].Rotated(angulo);
            Vector2 dir = (direction + offsetRotado * 0.5f).Normalized();
            _velocidades[i] = dir * velocidadInicial;
        }

        _animando = true;
    }
}