
using System.Collections.Generic;
using Godot;
using PrimerjuegoPlataformas2D.escenas.pantalla1;

namespace PrimerjuegoPlataformas2D.escenas.entidades.jugador;

public class SistemaAtravesarPlataformas(Jugador jugador)
{
    public async void AtravesarPlataformaDebajo()
    {
        if (jugador.CollisionShape2D == null)
            return;

        List<PhysicsBody2D> plataformasDebajoJugador = ObtenerPlataformasDebajoJugador();
        if (plataformasDebajoJugador.Count == 0)
            return;

        // Aplicamos excepciones de colisión a todas las plataformas detectadas.
        foreach (var plataforma in plataformasDebajoJugador)
            jugador.AddCollisionExceptionWith(plataforma);

        // Esperamos un tiempo corto para atravesar las plataformas.
        await jugador.ToSignal(jugador.GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);

        // Volvemos a habilitar colisión de las paltaformas.
        foreach (var plataforma in plataformasDebajoJugador)
            jugador.RemoveCollisionExceptionWith(plataforma);
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
            Exclude = [jugador.GetRid()] // Excluimos al jugador.
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