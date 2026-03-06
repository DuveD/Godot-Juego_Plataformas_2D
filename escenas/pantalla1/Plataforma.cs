using Godot;

namespace PrimerjuegoPlataformas2D.escenas.pantalla1;

public partial class Plataforma : AnimatableBody2D
{
    public CollisionShape2D CollisionShape2D;

    public override void _Ready()
    {
        CollisionShape2D = GetNode<CollisionShape2D>("CollisionShape2D");
    }
}