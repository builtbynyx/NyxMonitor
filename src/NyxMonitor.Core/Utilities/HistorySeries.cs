namespace NyxMonitor.Core.Utilities;

/// <summary>
/// Fixed-capacity rolling history. Allocates once; missing samples are stored as <see cref="double.NaN"/>.
/// Not thread-safe: add and read from the same (UI) thread.
/// </summary>
public sealed class HistorySeries
{
    private readonly double[] _buffer;
    private int _head;   // index of the oldest sample
    private int _count;

    public HistorySeries(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);
        _buffer = new double[capacity];
        Array.Fill(_buffer, double.NaN);
    }

    public event EventHandler? Changed;

    public int Capacity => _buffer.Length;
    public int Count => _count;

    /// <summary>Sample at <paramref name="index"/>, where 0 is the oldest.</summary>
    public double this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _buffer[(_head + index) % _buffer.Length];
        }
    }

    public double? Latest
    {
        get
        {
            if (_count == 0)
            {
                return null;
            }

            double value = this[_count - 1];
            return double.IsNaN(value) ? null : value;
        }
    }

    public void Add(double? value)
    {
        double sample = value is { } v && double.IsFinite(v) ? v : double.NaN;

        if (_count < _buffer.Length)
        {
            _buffer[(_head + _count) % _buffer.Length] = sample;
            _count++;
        }
        else
        {
            _buffer[_head] = sample;
            _head = (_head + 1) % _buffer.Length;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
        Array.Fill(_buffer, double.NaN);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
