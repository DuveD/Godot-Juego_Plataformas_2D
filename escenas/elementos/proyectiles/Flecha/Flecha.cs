using System;
using Godot;
using PrimerjuegoPlataformas2D.escenas.entidades.enemigos.Slime;
using PrimerjuegoPlataformas2D.nucleo.constantes;
using PrimerjuegoPlataformas2D.nucleo.utilidades;

namespace PrimerjuegoPlataformas2D.escenas.elementos.proyectiles.Flecha;

public partial class Flecha : CharacterBody2D
{
    Sprite2D Sprite;

    #region Físicas
    [Export]
    public float VelocidadFlecha = 200f;

    public const float MAXIMA_VELOCIDAD_CAIDA = 300f;

    private const float DESACELERACION = 30f; // píxeles/segundo²

    private float _tiempoTransicionGravedad = 0.2f; // tiempo para alcanzar gravedad completa
    private float _tiempoVuelo = 0f;

    private float _tiempoVueloRecto;

    private float _gravedad = 0;
    #endregion

    public bool Disparada = false;

    private (int H, int V) _direccionInicial;

    private float _velocidadHorizontalInicial = 0f;

    public const float DISTANCIA_CLAVADO = 5f;

    public override void _Ready()
    {
        Sprite = GetNode<Sprite2D>("Sprite2D");
    }

    public void Disparar((int H, int V) direccion, float fuerzaDeDisparo)
    {
        _direccionInicial = direccion;
        _tiempoVuelo = 0f;

        if (_direccionInicial.V < 0)
        {
            _tiempoVueloRecto = 0f;
            _gravedad = UtilidadesFisicas.ObtenerGravedad() * 0.5f;
            _tiempoTransicionGravedad = 0.2f;
        }
        else
        {
            _tiempoVueloRecto = 0.1f * fuerzaDeDisparo;
            _gravedad = UtilidadesFisicas.ObtenerGravedad();
            _tiempoTransicionGravedad = 0.4f;
        }

        Vector2 vectorDireccion = UtilidadesDirecciones.AVector2(_direccionInicial);
        Velocity = vectorDireccion * VelocidadFlecha * fuerzaDeDisparo;
        _velocidadHorizontalInicial = Velocity.X; // guardar la X inicial
        Rotation = new Vector2(direccion.H, direccion.V).Angle();

        Disparada = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Disparada) return;

        Vector2 velocidad = Velocity;
        _tiempoVuelo += (float)delta;

        if (_tiempoVuelo > _tiempoVueloRecto)
        {
            velocidad.X = Mathf.MoveToward(velocidad.X, 0, DESACELERACION * (float)delta);
        }

        float t = Mathf.Clamp((_tiempoVuelo - _tiempoVueloRecto) / _tiempoTransicionGravedad, 0f, 1f);
        float gravedadActual = Mathf.Lerp(0f, _gravedad, t);
        velocidad.Y = Mathf.Min(velocidad.Y + gravedadActual * (float)delta, MAXIMA_VELOCIDAD_CAIDA);

        Rotation = velocidad.Angle();
        Velocity = velocidad;

        MoveAndSlide();

        ProcesarColisiones();
    }

    private void ProcesarColisiones()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var colision = GetSlideCollision(i);
            var colisionador = colision.GetCollider();

            if (colisionador is Slime enemigo)
            {
                Clavarse(enemigo);
                return;
            }

            if (colisionador is StaticBody2D or TileMapLayer or Area2D)
            {
                Clavarse((Node2D)colisionador);
                return;
            }
        }
    }

    private void Clavarse(Node2D colisionador)
    {
        Vector2 posicionGlobal = GlobalPosition;
        float rotacionGlobal = GlobalRotation;

        GetParent().RemoveChild(this);
        colisionador.AddChild(this);

        GlobalPosition = posicionGlobal;
        GlobalRotation = rotacionGlobal;

        this.ZIndex = -20;

        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);

        // Avanzamos el sprite para que quede clavada en la pared o enemigo
        Sprite.Position = GlobalPosition.Normalized() * DISTANCIA_CLAVADO;
    }
}