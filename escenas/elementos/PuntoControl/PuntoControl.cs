using Godot;
using PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

namespace PrimerjuegoPlataformas2D.escenas.elementos.PuntoControl;

public partial class PuntoControl : Marker2D
{
	[Export]
	public bool Activado
	{
		get;
		set
		{
			field = value;
			if (!IsNodeReady()) return;
			Callable.From(() => _detectorJugador.ProcessMode = value ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit).CallDeferred();
		}
	} = false;

	private Area2D _detectorJugador;
	private CollisionShape2D _collisionShape2D;
	private Sprite2D _sprite2D;

	StaticBody2D _colisionCartel = null;

	[Export]
	public int Direccion = 1;

	[Export]
	public bool Animar = true;

	public override void _Ready()
	{
		_detectorJugador = GetNode<Area2D>("DetectorJugador");
		_collisionShape2D = _detectorJugador.GetNode<CollisionShape2D>("CollisionShape2D");
		_sprite2D = GetNode<Sprite2D>("Sprite2D");
		_colisionCartel = GetNode<StaticBody2D>("ColisionCartel");

		_detectorJugador.BodyEntered += OnBodyEntered;

		Inicializar();
	}

	private void Inicializar()
	{
		Activado = Activado;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public override void _Process(double delta)
	{
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Jugador jugador)
			OnJugadorEntered(jugador);
	}

	private void OnJugadorEntered(Jugador jugador)
	{
		if (Activado)
			return;

		jugador.PuntoControl = this;
		Activado = true;

		if (Animar)
			ActivarAnimacion();
	}

	private void ActivarAnimacion()
	{
		var tween = CreateTween();

		float duracion = 0.08f;
		float escala = 1f;

		for (int i = 0; i < 6; i++)
		{
			escala *= -1;

			tween.TweenProperty(
				this,
				"scale:x",
				escala,
				duracion
			);

			duracion *= 1.5f; // cada vuelta más lenta
		}
	}
}
