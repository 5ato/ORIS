
using GameAndDot.Shared.Enums;
using System.Drawing;

namespace GameAndDot.Shared.Models;

public class EventMessage
{
    public EventType Type { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Color { get; set; }
    public Player[] Players { get; set; } = [];

    public double X { get; set; }
    public double Y { get; set; }
    public Point StartPoint { get; set; }
    public Point EndPoint { get; set; }
    public Point[] Points {  get; set; } = [];
    public bool ClearCanvas { get; set; }
    public int BrushSize { get; set; }
}

public class Player
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Color { get; set; }
}

