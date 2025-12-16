
namespace GameAndDot.MAUI;

/// <summary>
/// Сущность точки
/// </summary>
public class Dot
{
    public Point Position { get; set; }
    public Color Color { get; set; }
    public float Radius { get; set; } = 8f;
}

/// <summary>
/// Собственный класс для рисования точек на канвасе
/// </summary>
public class DotCanvasDrawable : IDrawable
{
    private readonly List<Dot> _dots;
    private readonly object _dotsLock = new object();

    public DotCanvasDrawable()
    {
        _dots = [];
    }

    /// <summary>
    /// Добавляет точку в список точек для рисования
    /// </summary>
    /// <param name="position"></param>
    /// <param name="color"></param>
    public void AddDot(Point position, Color color)
    {
        lock (_dotsLock)
        {
            _dots.Add(new Dot
            {
                Position = position,
                Color = color,
                Radius = 8f
            });
        }
    }

    /// <summary>
    /// Чистит все точки с канваса
    /// </summary>
    public void ClearDots()
    {
        lock (_dotsLock)
        {
            _dots.Clear();
        }
    }

    /// <summary>
    /// Получает все точки с канваса
    /// </summary>
    public int DotCount
    {
        get
        {
            lock (_dotsLock)
            {
                return _dots.Count;
            }
        }
    }

    /// <summary>
    /// Рисует все точки на канвасе
    /// Этот метод автоматически вызывается в GraphicsView
    /// </summary>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.Antialias = true;

        // Рисует каждую точку
        lock (_dotsLock)
        {
            foreach (var dot in _dots)
            {
                // Заполняет своим цветом
                canvas.FillColor = dot.Color;

                // Рисует точку как заполненый круг
                canvas.FillCircle(
                    (float)dot.Position.X,
                    (float)dot.Position.Y,
                    dot.Radius
                );

                canvas.StrokeColor = Colors.Black;
                canvas.StrokeSize = 1;
                canvas.DrawCircle(
                    (float)dot.Position.X,
                    (float)dot.Position.Y,
                    dot.Radius
                );
            }
        }
    }
}
