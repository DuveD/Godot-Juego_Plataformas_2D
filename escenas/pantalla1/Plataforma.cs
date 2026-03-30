using System.Collections.Generic;
using Godot;

namespace PrimerjuegoPlataformas2D.escenas.pantalla1;

[Tool]
public partial class Plataforma : AnimatableBody2D
{
    #region Exports
    private int _ancho = 1;

    [Export]
    public int Ancho
    {
        get => _ancho;
        set
        {
            _ancho = Mathf.Max(1, value);
            ActualizarTamano();
        }
    }
    #endregion

    #region Límites
    public float CentroX => GlobalPosition.X;
    public float LimiteTop => GlobalPosition.Y;
    public float LimiteBottom => GlobalPosition.Y + TamanoTotal();
    private int _tamanoTramo = 16;
    protected float TamanoTotal() => _tamanoTramo * _ancho;
    protected float _altoColision = 1f;
    #endregion

    #region Nodos
    protected Sprite2D _spriteUnico;
    protected Sprite2D _spriteInicio;
    protected Sprite2D _spriteFin;
    protected Node2D _contenedorMedio;
    protected Sprite2D _templateMedio;
    protected CollisionShape2D _collisionShape2D;
    protected List<Sprite2D> _sprites = new List<Sprite2D>();
    #endregion

    public override void _Ready()
    {
        _spriteUnico = GetNodeOrNull<Sprite2D>("SpriteUnico");
        _spriteInicio = GetNodeOrNull<Sprite2D>("SpriteInicio");
        _spriteFin = GetNodeOrNull<Sprite2D>("SpriteFin");
        _contenedorMedio = GetNodeOrNull<Node2D>("SpritesMedio");
        _templateMedio = GetNodeOrNull<Sprite2D>("SpritesMedio/Template");
        _collisionShape2D = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_collisionShape2D?.Shape is RectangleShape2D rect)
            _altoColision = rect.Size.Y;

        ActualizarTamano();
    }

    #region Generación visual
    protected virtual void ActualizarTamano()
    {
        if (!IsInsideTree())
            return;

        _spriteUnico ??= GetNodeOrNull<Sprite2D>("SpriteUnico");
        _spriteInicio ??= GetNodeOrNull<Sprite2D>("SpriteInicio");
        _spriteFin ??= GetNodeOrNull<Sprite2D>("SpriteFin");
        _contenedorMedio ??= GetNodeOrNull<Node2D>("SpritesMedio");
        _templateMedio ??= GetNodeOrNull<Sprite2D>("SpritesMedio/Template");
        _collisionShape2D ??= GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

        if (_spriteInicio == null || _spriteFin == null || _spriteUnico == null || _contenedorMedio == null || _templateMedio == null)
            return;

        ActualizarSprites();
        ActualizarColision();
    }

    private void ActualizarSprites()
    {
        LimpiarMedios();

        float offsetX = -TamanoTotal() / 2 + _tamanoTramo / 2;

        if (_ancho == 1)
        {
            MostrarSolo(_spriteUnico);
            _spriteUnico.Position = new Vector2(offsetX, 0);
            _sprites.Add(_spriteUnico);
            return;
        }

        OcultarSprite(_spriteUnico);

        _spriteInicio.Visible = true;
        _sprites.Add(_spriteInicio);
        _spriteInicio.Position = new Vector2(offsetX, 0);

        if (Ancho > 2)
        {
            _contenedorMedio.Visible = true;
            for (int i = 0; i < _ancho - 2; i++)
            {
                var medio = (Sprite2D)_templateMedio.Duplicate();
                medio.Visible = true;
                _sprites.Add(medio);
                medio.Position = new Vector2(offsetX + _tamanoTramo * (i + 1), 0);
                _contenedorMedio.AddChild(medio);

                if (Engine.IsEditorHint())
                    medio.Owner = GetTree().EditedSceneRoot;
            }
        }
        else
        {
            _contenedorMedio.Visible = false;
        }

        _spriteFin.Visible = true;
        _sprites.Add(_spriteFin);
        _spriteFin.Position = new Vector2(offsetX + _tamanoTramo * (_ancho - 1), 0);
    }

    private void LimpiarMedios()
    {
        if (_contenedorMedio == null) return;

        foreach (Node hijo in _contenedorMedio.GetChildren())
        {
            if (hijo != _templateMedio)
                hijo.QueueFree();
        }

        _sprites.Clear();
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
        if (_collisionShape2D == null)
            return;

        float tamano = TamanoTotal();
        var rect = new RectangleShape2D();
        rect.Size = new Vector2(tamano, _altoColision);

        _collisionShape2D.Position = new Vector2(0, -7.5f);
        _collisionShape2D.Shape = rect;
    }
    #endregion
}