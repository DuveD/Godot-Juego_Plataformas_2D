using Godot;

namespace PrimerjuegoPlataformas2D.escenas.entidades.jugador;

public partial class Jugador : CharacterBody2D
{
    public float VELOCIDAD = 130.0f;

    public float VELOCIDAD_SALTO = 300.0f;

    private AnimatedSprite2D _animatedSprite2D;

    public CollisionShape2D CollisionShape2D;

    public float Gravedad = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private SistemaAtravesarPlataformas _sistemaAtravesarPlataformas;

    public Jugador()
    {
        _sistemaAtravesarPlataformas = new SistemaAtravesarPlataformas(this);
    }

    public override void _Ready()
    {
        this._animatedSprite2D = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        this.CollisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocidad = Velocity;

        velocidad = AplicarGravedad(delta, velocidad);

        velocidad = OnSalto(velocidad);

        // Movimiento horizontal
        float direccion = Input.GetAxis("ui_left", "ui_right");
        if (direccion != 0)
        {
            velocidad.X = direccion * VELOCIDAD;
            _animatedSprite2D.FlipH = !(direccion > 0);
        }
        else
        {
            velocidad.X = Mathf.MoveToward(velocidad.X, 0f, VELOCIDAD * 10);
        }

        this.Velocity = velocidad;
        MoveAndSlide();
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

    private Vector2 OnSalto(Vector2 velocidad)
    {
        if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
        {
            if (Input.IsActionPressed("ui_down"))
                _sistemaAtravesarPlataformas.AtravesarPlataformaDebajo();
            else
                velocidad.Y = -VELOCIDAD_SALTO;
        }

        return velocidad;
    }
}