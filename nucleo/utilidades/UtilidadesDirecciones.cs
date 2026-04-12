using Godot;

namespace PrimerjuegoPlataformas2D.nucleo.utilidades;

public class UtilidadesDirecciones
{
    /// <summary>
    /// Convierte una dirección dada por componentes enteros en un Vector2 normalizado.<br/>
    /// H -> 1 = derecha, -1 = izquierda.<br/>
    /// V -> 1 = abajo, -1 = arriba.
    /// </summary>
    public static Vector2 AVector2(int h, int v)
    {
        Vector2 dir = new(h, v);
        return dir == Vector2.Zero ? Vector2.Zero : dir.Normalized();
    }

    public static Vector2 AVector2((int H, int V) direccion) => AVector2(direccion.H, direccion.V);
}