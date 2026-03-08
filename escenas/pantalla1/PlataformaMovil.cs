using Godot;

namespace PrimerjuegoPlataformas2D.escenas.pantalla1;

public partial class PlataformaMovil : Plataforma
{
    private Vector2 _posAnterior;

    public Vector2 VelocidadActual { get; private set; }

    [Export]
    public Vector2 Inicio { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 Fin { get; set; } = Vector2.Zero;

    public float Velocidad = 50f;
    private bool _haciaFin = true;
    public override void _Ready()
    {
        _posAnterior = GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 target = _haciaFin ? Fin : Inicio;
        Vector2 direccion = (target - Position);

        if (direccion.Length() < 0.01f)
        {
            Position = target;      // Llegó al target exacto
            _haciaFin = !_haciaFin; // Cambiamos dirección
        }
        else
        {
            Vector2 movimiento = direccion.Normalized() * Velocidad * (float)delta;
            // Evitamos pasar del target
            if (movimiento.Length() > direccion.Length())
                Position = target;
            else
                Position += movimiento;
        }

        // Velocidad por segundo
        VelocidadActual = (Position - _posAnterior) / (float)delta;
        _posAnterior = Position;
    }
}