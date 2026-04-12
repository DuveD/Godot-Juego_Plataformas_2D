
using Godot;

namespace PrimerjuegoPlataformas2D.nucleo.constantes;

public static class Direcciones
{
    public static readonly Vector2 Derecha = Vector2.Right;
    public static readonly Vector2 Izquierda = Vector2.Left;
    public static readonly Vector2 Arriba = Vector2.Up;
    public static readonly Vector2 Abajo = Vector2.Down;
    public static readonly Vector2 DerechaArriba = (Vector2.Right + Vector2.Up).Normalized();
    public static readonly Vector2 DerechaAbajo = (Vector2.Right + Vector2.Down).Normalized();
    public static readonly Vector2 IzquierdaArriba = (Vector2.Left + Vector2.Up).Normalized();
    public static readonly Vector2 IzquierdaAbajo = (Vector2.Left + Vector2.Down).Normalized();
}