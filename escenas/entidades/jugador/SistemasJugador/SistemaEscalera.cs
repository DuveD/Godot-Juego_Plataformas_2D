using Godot;
using PrimerjuegoPlataformas2D.escenas.elementos.Escalera;

namespace PrimerjuegoPlataformas2D.escenas.entidades.Jugador.SistemasJugador;

public partial class SistemaEscalera : SistemaJugador
{
    // Constantes
    private const float VELOCIDAD_ESCALAR = 80f;
    private const float DISTANCIA_CAMBIO_FRAME = 20f;
    private const float MAX_VELOCIDAD_SNAP_X = 200f;

    private Escalera _escaleraActual = null;
    private bool _agarradoEscalera = false;
    public bool AgarradoEscalera => _agarradoEscalera;
    private float _distanciaEscalada = 0f;
    private bool _moviendoseEscaleraAnterior = false;
    private int _frameEscalera = 0;

    public bool EstaEscalando => _agarradoEscalera && _escaleraActual != null;

    private SistemaEscalera(Jugador jugador) : base(jugador)
    {
    }

    public static SistemaEscalera Inicializar(Jugador jugador)
    {
        var sistema = new SistemaEscalera(jugador);
        jugador.AddChild(sistema);
        return sistema;
    }

    // --- Física ---

    public Vector2? ProcesarMovimientoEscalera(double delta, Vector2 velocidad, InputJugador input)
    {
        if (_jugador.EstadoAccion == Jugador.EstadoAccionJugador.Disparando)
            return null;

        if (SoltarEscaleraSiEnSuelo())
            return null;

        IntentarAgarrarEscalera(ref velocidad, input);

        if (!EstaEscalando)
            return null;

        if (SaltarDesdeEscalera(ref velocidad))
            return velocidad;

        CalcularVelocidadEscalando(delta, ref velocidad, input);

        return velocidad;
    }

    private bool SoltarEscaleraSiEnSuelo()
    {
        bool soltarEscalera = false;

        if (_agarradoEscalera && _jugador.EnSuelo)
        {
            _agarradoEscalera = false;
            GD.Print("Escalera soltada al aterrizar.");

            soltarEscalera = true;
        }

        return soltarEscalera;
    }

    private void IntentarAgarrarEscalera(ref Vector2 velocidad, InputJugador input)
    {
        if (_escaleraActual == null || _agarradoEscalera || _jugador.Rodando)
            return;

        bool subir = input.ArribaPresionado;
        bool bajar = input.AbajoPresionado && !_jugador.EnSuelo;

        if (!subir && !bajar)
            return;

        _agarradoEscalera = true;
        _distanciaEscalada = 0f;
        _moviendoseEscaleraAnterior = false;
        _jugador.ResetearCoyoteTime();

        // Detenemos el jugador.
        velocidad = Vector2.Zero;
    }

    private bool SaltarDesdeEscalera(ref Vector2 velocidad)
    {
        if (!_jugador.HayInputSalto())
            return false;

        _agarradoEscalera = false;
        velocidad.Y = -_jugador.VELOCIDAD_SALTO;
        _jugador.ResetearCoyoteTime();
        _jugador.ResetearBufferSalto();

        return true;
    }

    private void CalcularVelocidadEscalando(double delta, ref Vector2 velocidad, InputJugador input)
    {
        float dirY = input.Arriba ? -1f : input.Abajo ? 1f : 0f;
        velocidad.Y = dirY * VELOCIDAD_ESCALAR;

        velocidad.X = Mathf.Clamp(
            (_escaleraActual.CentroX - _jugador.GlobalPosition.X) / (float)delta,
            -MAX_VELOCIDAD_SNAP_X, MAX_VELOCIDAD_SNAP_X
        );

        float movimiento = Mathf.Abs(velocidad.Y * (float)delta);
        if (movimiento > 0)
            _distanciaEscalada += movimiento;
    }

    // --- Animación ---

    public void ActualizarAnimacion(InputJugador input)
    {
        bool moviendose = input.Arriba || input.Abajo;

        _jugador.ReproducirAnimacion(AnimacionJugador.Escalando);

        if (moviendose)
        {
            if (!_moviendoseEscaleraAnterior)
            {
                _frameEscalera = 1 - _jugador.SpriteJugador.Frame;
                _distanciaEscalada = 0f;
            }
            else
            {
                _distanciaEscalada += Mathf.Abs(_jugador.Velocity.Y * (float)_jugador.GetPhysicsProcessDeltaTime());

                if (_distanciaEscalada >= DISTANCIA_CAMBIO_FRAME)
                {
                    _distanciaEscalada -= DISTANCIA_CAMBIO_FRAME;
                    _frameEscalera = 1 - _frameEscalera;
                }
            }

            _jugador.SpriteJugador.Frame = _frameEscalera;
        }

        _moviendoseEscaleraAnterior = moviendose;
    }

    // --- Zona escalera ---

    public void EntrarZonaEscalera(Escalera escalera)
    {
        if (escalera == null)
            return;

        _escaleraActual = escalera;
    }

    public void SalirZonaEscalera(Escalera escalera)
    {
        if (escalera == null || _escaleraActual != escalera)
            return;

        _escaleraActual = null;
        _agarradoEscalera = false;
    }
}