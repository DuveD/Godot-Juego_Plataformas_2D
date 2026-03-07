
using System.Collections.Generic;
using Godot;
using PrimerjuegoPlataformas2D.escenas.pantalla1;

namespace PrimerjuegoPlataformas2D.escenas.entidades.jugador;

public partial class SistemaAtravesarPlataformas(Jugador jugador) : Node
{
    public float Gravedad = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private HashSet<PhysicsBody2D> _plataformas = [];

    public Vector2 AtravesarPlataformasDebajo(double delta, Vector2 velocidad)
    {
        var plataformas = ObtenerPlataformasDebajoJugador();
        if (plataformas.Count == 0)
            return velocidad;

        // Añadimos excepciones de colisión.
        AplicarExcepceionesDeColision(plataformas);

        // Empujamos 1px hacia abajo inmediatamente y aplciamos gravedad.
        jugador.Position += new Vector2(0, 1);
        velocidad.Y += Gravedad * (float)delta;

        // La restauración de colisiones se hará en el siguiente frame
        CallDeferred(nameof(RestaurarExcepceionesDeColision));

        return velocidad;
    }

    private void AplicarExcepceionesDeColision(List<PhysicsBody2D> plataformas)
    {
        foreach (var plataforma in plataformas)
            if (!jugador.GetCollisionExceptions().Contains(plataforma))
            {
                jugador.AddCollisionExceptionWith(plataforma);
                _plataformas.Add(plataforma);
            }
    }

    private void RestaurarExcepceionesDeColision()
    {
        foreach (var plataforma in _plataformas)
        {
            jugador.RemoveCollisionExceptionWith(plataforma);
            _plataformas.Remove(plataforma);
        }
    }

    private bool JugadorSigueSobrePlataforma(List<PhysicsBody2D> plataformas)
    {
        foreach (var plataforma in plataformas)
        {
            if (jugador.GlobalPosition.Y <= plataforma.GlobalPosition.Y)
                return true;
        }

        return false;
    }

    public List<PhysicsBody2D> ObtenerPlataformasDebajoJugador()
    {
        var space = jugador.GetWorld2D().DirectSpaceState;

        // Creamos un query usando el mismo CollisionShape2D del jugador.
        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = jugador.CollisionShape2D.Shape,
            Transform = new Transform2D(0, jugador.GlobalPosition + new Vector2(0, 2)), // Desplazamos 2px hacia abajo.
            CollideWithAreas = false,
            CollideWithBodies = true,
            Exclude = new Godot.Collections.Array<Rid> { jugador.GetRid() }
        };

        // Obtenemos todas las colisiones.
        var results = space.IntersectShape(query);

        // Obtenemos todas las colisiones de tipo Plataforma.
        List<PhysicsBody2D> plataformasDebajoJugador = [];
        foreach (var res in results)
        {
            var nodo = res["collider"].As<Node>();
            if (nodo is Plataforma plataforma)
            {
                if (!plataformasDebajoJugador.Contains(plataforma))
                    plataformasDebajoJugador.Add(plataforma);
            }
        }

        return plataformasDebajoJugador;
    }
}