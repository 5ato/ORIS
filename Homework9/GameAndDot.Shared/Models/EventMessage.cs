
using GameAndDot.Shared.Enums;
using Signal.Core.Protocol.NMTP.Attributes;
using System.Drawing;

namespace GameAndDot.Shared.Models;

public class EventMessage
{
    [Field(1)]
    public EventType Type { get; set; }
    [Field(2)]
    public string Id { get; set; } = string.Empty;
    [Field(3)]
    public string Username { get; set; } = string.Empty;
    [Field(4)]
    public int Color { get; set; }
    [Field(5)]
    public Player[] Players { get; set; } = [];

    [Field(6)]
    public double X { get; set; }
    [Field(7)]
    public double Y { get; set; }
    [Field(8)]
    public Point StartPoint { get; set; }
    [Field(9)]
    public Point EndPoint { get; set; }
    [Field(10)]
    public Point[] Points {  get; set; } = [];
    [Field(11)]
    public int BrushSize { get; set; }
}

public class Player
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Color { get; set; }
}

