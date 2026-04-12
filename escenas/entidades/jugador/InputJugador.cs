using Godot;

namespace PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

public struct InputJugador
{
    public bool RodarPresionado;

    public bool SaltoPresionado;
    public bool Salto;


    public bool ArribaPresionado;
    public bool Arriba;
    public bool AbajoPresionado;
    public bool Abajo;
    public bool Izquierda;
    public bool Derecha;

    public bool DispararPresionado;
    public bool Disparar;

    /// <summary>
    /// H -> 1 = derecha, -1 = izquierda.<br/>
    /// V -> 1 = abajo, -1 = arriba.
    /// </summary>
    public readonly (int H, int V) Direccion
    {
        get
        {
            int h = (Derecha ? 1 : 0) - (Izquierda ? 1 : 0);
            int v = (Abajo ? 1 : 0) - (Arriba ? 1 : 0);
            return (h, v);
        }
    }

    public InputJugador LeerInput()
    {
        InputJugador inputJugador = new InputJugador
        {
            RodarPresionado = Input.IsActionJustPressed("rodar"),

            SaltoPresionado = Input.IsActionJustPressed("ui_accept"),
            Salto = Input.IsActionPressed("ui_accept"),

            ArribaPresionado = Input.IsActionJustPressed("ui_up"),
            Arriba = Input.IsActionPressed("ui_up"),
            AbajoPresionado = Input.IsActionJustPressed("ui_down"),
            Abajo = Input.IsActionPressed("ui_down"),
            Izquierda = Input.IsActionPressed("ui_left"),
            Derecha = Input.IsActionPressed("ui_right"),

            DispararPresionado = Input.IsActionJustPressed("disparar"),
            Disparar = Input.IsActionPressed("disparar")
        };

        return inputJugador;
    }
}