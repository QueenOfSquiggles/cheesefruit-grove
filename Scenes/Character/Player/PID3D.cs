using System;
using Godot;

/// <summary>
/// Used for applying physics forces to correct towards a desired speed. Generally will require applying a factor of 0.01f to outputs for some reason.
/// </summary>
public partial class PID3D : RefCounted
{

    private float _p;
    private float _i;
    private float _d;

    private Vector3 _prev_error;
    private Vector3 _error_integral;

    public PID3D(float p = 1.0f, float i = 0.1f, float d = 1.0f)
    {
        _p = p;
        _i = i;
        _d = d;
    }

    public Vector3 Update(Vector3 error, float delta)
    {
        _error_integral += error * delta;
        var error_derivative = (error - _prev_error) / delta;
        _prev_error = error;
        return _p * error + _i * _error_integral + _d * error_derivative;
    }
}
