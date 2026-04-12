using System;
using Godot;
using PrimerjuegoPlataformas2D.escenas.elementos.proyectiles.Flecha;
using PrimerjuegoPlataformas2D.escenas.entidades.Jugador;

public partial class ZonaMuerte : Area2D
{
	// Called when the node enters the scene tree for the first time.

	public override void _Ready()
	{
		this.BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Jugador jugador)
		{
			jugador.Muerte();
		}
		else if (body is Flecha flecha)
		{
			flecha.QueueFree();
			GD.Print($"ZonaMuerte: Flecha destruida al entrar en zona de muerte: {flecha.Name}");
		}
		else if (body is StaticBody2D or CharacterBody2D or RigidBody2D)
		{
			body.QueueFree();
			GD.Print($"ZonaMuerte: Nodo destruido al entrar en zona de muerte: {body.Name} ({body.GetType()})");
		}
		else
		{
			GD.Print($"ZonaMuerte: Colisionador inesperado: {body.Name} ({body.GetType()})");
		}
	}
}
