using Godot;
using PrimerjuegoPlataformas2D.escenas.entidades.jugador;

namespace PrimerjuegoPlataformas2D.escenas.bloques;

[Tool]
public partial class Escalera : Area2D
{
    #region Exports
    private int _alto = 1;

    [Export]
    public int Alto
    {
        get => _alto;
        set
        {
            _alto = Mathf.Max(1, value);
            ActualizarTamano();
        }
    }
    #endregion

    #region Límites
    public float CentroX => GlobalPosition.X;
    public float LimiteTop => GlobalPosition.Y;
    public float LimiteBottom => GlobalPosition.Y + TamanoTotal();
    private int _tamanoTramo = 16;
    private float TamanoTotal() => _tamanoTramo * _alto;
    private float _anchoColision = 16f;
    #endregion

    #region Nodos
    private Sprite2D _spriteUnico;
    private Sprite2D _spriteInicio;
    private Sprite2D _spriteFin;
    private Node2D _contenedorMedio;
    private Sprite2D _templateMedio;
    private CollisionShape2D _collisionShape;
    #endregion

    public override void _Ready()
    {
        _spriteUnico = GetNodeOrNull<Sprite2D>("SpriteUnico");
        _spriteInicio = GetNodeOrNull<Sprite2D>("SpriteInicio");
        _spriteFin = GetNodeOrNull<Sprite2D>("SpriteFin");
        _contenedorMedio = GetNodeOrNull<Node2D>("SpritesMedio");
        _templateMedio = GetNodeOrNull<Sprite2D>("SpritesMedio/Template");
        _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_collisionShape?.Shape is RectangleShape2D rect)
            _anchoColision = rect.Size.X;

        ActualizarTamano();

        // En runtime conectamos el Area2D
        if (!Engine.IsEditorHint())
        {
            this.BodyEntered += OnBodyEntered;
            this.BodyExited += OnBodyExited;
        }
    }

    #region Generación visual
    private void ActualizarTamano()
    {
        if (!IsInsideTree())
            return;

        _spriteUnico ??= GetNodeOrNull<Sprite2D>("SpriteUnico");
        _spriteInicio ??= GetNodeOrNull<Sprite2D>("SpriteInicio");
        _spriteFin ??= GetNodeOrNull<Sprite2D>("SpriteFin");
        _contenedorMedio ??= GetNodeOrNull<Node2D>("SpritesMedio");
        _templateMedio ??= GetNodeOrNull<Sprite2D>("SpritesMedio/Template");
        _collisionShape ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_spriteInicio == null || _spriteFin == null || _spriteUnico == null || _contenedorMedio == null || _templateMedio == null)
            return;

        ActualizarSprites();
        ActualizarColision();
    }

    private void ActualizarSprites()
    {
        LimpiarMedios();

        if (_alto == 1)
        {
            MostrarSolo(_spriteUnico);
            _spriteUnico.Position = new Vector2(0, 0);

            return;
        }

        // Tramos >= 2: inicio + [medios] + fin
        OcultarSprite(_spriteUnico);

        _spriteInicio.Visible = true;
        _spriteInicio.Position = new Vector2(0, 0);

        if (Alto > 2)
        {
            _contenedorMedio.Visible = true;
            for (int i = 0; i < _alto - 2; i++)
            {
                var medio = (Sprite2D)_templateMedio.Duplicate();
                medio.Visible = true;
                medio.Position = new Vector2(0, _tamanoTramo * -(i + 1));
                _contenedorMedio.AddChild(medio);

                // Necesario para que aparezca en el editor con @tool
                if (Engine.IsEditorHint())
                    medio.Owner = GetTree().EditedSceneRoot;
            }
        }
        else
        {
            _contenedorMedio.Visible = false;
        }

        _spriteFin.Visible = true;
        _spriteFin.Position = new Vector2(0, _tamanoTramo * -(_alto - 1));
    }

    private void LimpiarMedios()
    {
        if (_contenedorMedio == null) return;

        foreach (Node hijo in _contenedorMedio.GetChildren())
        {
            if (hijo != _templateMedio)
                hijo.QueueFree();
        }
    }

    private void MostrarSolo(Sprite2D objetivo)
    {
        OcultarSprite(_spriteInicio);
        OcultarSprite(_spriteFin);
        OcultarSprite(_spriteUnico);

        if (objetivo != null)
            objetivo.Visible = true;
    }

    private static void OcultarSprite(Sprite2D sprite)
    {
        if (sprite != null)
            sprite.Visible = false;
    }

    private void ActualizarColision()
    {
        if (_collisionShape == null)
            return;

        float tamano = TamanoTotal();
        tamano = (tamano > _tamanoTramo) ? tamano - _tamanoTramo : tamano;

        var rect = new RectangleShape2D();
        rect.Size = new Vector2(_anchoColision, tamano);

        float offset = (-tamano / 2 + _tamanoTramo / 2);
        _collisionShape.Position = new Vector2(0, offset);
        _collisionShape.Shape = rect;
    }
    #endregion

    #region Detección de jugador
    private void OnBodyEntered(Node2D body)
    {
        if (body is Jugador jugador)
            jugador.EntrarZonaEscalera(this);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is Jugador jugador)
            jugador.SalirZonaEscalera(this);
    }
    #endregion
}