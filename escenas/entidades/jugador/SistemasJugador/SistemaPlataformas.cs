
using System.Collections.Generic;
using System.Linq;
using Godot;
using PrimerjuegoPlataformas2D.escenas.elementos.Plataforma;

namespace PrimerjuegoPlataformas2D.escenas.entidades.Jugador.SistemasJugador;

public partial class SistemaPlataformas : SistemaJugador
{
    private float _tiempoAtravesandoPlataforma = 0f;
    private const float DURACION_DROP_PLATAFORMA = 0.18f;

    public float Gravedad = (float)ProjectSettings.GetSetting("physics/2d/default_gravity");

    private HashSet<Plataforma> _plataformasDebajo = [];
    private HashSet<Plataforma> _plataformasIgnoradas = [];

    private SistemaPlataformas(Jugador jugador) : base(jugador)
    {
    }

    public static SistemaPlataformas Inicializar(Jugador jugador)
    {
        var sistema = new SistemaPlataformas(jugador);
        jugador.AddChild(sistema);
        return sistema;
    }

    override public void _Ready()
    {
        this._jugador.SensorSuelo.BodyEntered += OnSensorSueloBodyEntered;
        this._jugador.SensorSuelo.BodyExited += OnSensorSueloBodyExited;
    }

    public void OnSensorSueloBodyEntered(Node body)
    {
        if (body is Plataforma plataforma)
        {
            _plataformasDebajo.Add(plataforma);
        }
    }

    public void OnSensorSueloBodyExited(Node body)
    {
        if (body is Plataforma plataforma)
        {
            _plataformasDebajo.Remove(plataforma);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_tiempoAtravesandoPlataforma > 0)
            _tiempoAtravesandoPlataforma -= (float)delta;
    }

    public Vector2 AtravesarPlataformasDebajo(double delta, Vector2 velocidad)
    {
        if (!this._jugador.IsOnFloor())
            return velocidad;

        var plataformas = ObtenerPlataformasDebajoJugador();
        if (plataformas.Count == 0)
            return velocidad;

        _tiempoAtravesandoPlataforma = DURACION_DROP_PLATAFORMA;

        // Añadimos excepciones de colisión.
        AplicarExcepceionesDeColision(plataformas);

        // Empujamos 1px hacia abajo inmediatamente y aplciamos gravedad.
        _jugador.Position += new Vector2(0, 1);
        velocidad.Y += Gravedad * (float)delta;

        // La restauración de colisiones se hará en el siguiente frame
        CallDeferred(nameof(RestaurarExcepceionesDeColision));

        return velocidad;
    }

    private void AplicarExcepceionesDeColision(List<Plataforma> plataformas)
    {
        foreach (var plataforma in plataformas)
        {
            if (!_jugador.GetCollisionExceptions().Contains(plataforma))
            {
                _jugador.AddCollisionExceptionWith(plataforma);
                _plataformasIgnoradas.Add(plataforma);
            }
        }
    }

    private void RestaurarExcepceionesDeColision()
    {
        foreach (var plataforma in _plataformasIgnoradas.ToList())
        {
            _jugador.RemoveCollisionExceptionWith(plataforma);
        }

        _plataformasIgnoradas.Clear();
    }

    public List<Plataforma> ObtenerPlataformasDebajoJugador()
    {
        return _plataformasDebajo.ToList();
    }

    public Plataforma ObtenerPlataformaDebajoJugadorPredominante()
    {
        PlataformaMovil plataformaCercana = null;
        float minDistancia = float.MaxValue;

        Vector2 centroJugador = _jugador.GlobalPosition;

        foreach (var piso in ObtenerPlataformasDebajoJugador())
        {
            if (piso is PlataformaMovil plataforma)
            {
                Vector2 centroPlataforma = plataforma.GlobalPosition;
                float distancia = centroJugador.DistanceTo(centroPlataforma);

                if (distancia < minDistancia)
                {
                    minDistancia = distancia;
                    plataformaCercana = plataforma;
                }
            }
        }

        return plataformaCercana;
    }

    public bool ExistenPlataformasIgnoradas()
    {
        return _plataformasIgnoradas.Count > 0;
    }

    public bool HayPlataformasDebajo()
    {
        return _plataformasDebajo.Count > 0;
    }

    public bool AtravesandoPlataformas()
    {
        return _tiempoAtravesandoPlataforma > 0;
    }
}