

using Godot;
using PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

public partial class SistemaJugador : Node
{
    protected Jugador _jugador;

    protected SistemaJugador(Jugador jugador)
    {
        this._jugador = jugador;
    }
}