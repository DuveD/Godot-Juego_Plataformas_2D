using Godot;

namespace PrimerjuegoPlataformas2D.nucleo.utilidades;

public static class UtilidadesFisicas
{
    public static float ObtenerGravedad()
    {
        return (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    }
}