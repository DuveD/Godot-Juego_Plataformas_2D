using Godot;

namespace PrimerjuegoPlataformas2D.nucleo.utilidades;

public static class UtilidadesFisicas
{
    private static float? _gravedad;
    public static float ObtenerGravedad()
    {
        return _gravedad ??= (float)ProjectSettings.GetSetting("physics/2d/default_gravity");
    }
}