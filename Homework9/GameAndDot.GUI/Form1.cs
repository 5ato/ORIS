using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Extensions;
using GameAndDot.Shared.Models;
using System.Drawing.Drawing2D;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace GameAndDot.GUI
{
    public partial class Form1 : Form
    {
        private readonly Socket _client;
        private string host = "127.0.0.1";
        private int port = 8888;

        private string Username { get; set; } = string.Empty;
        private string Id { get; set; } = string.Empty;


        private bool IsMouse = false;

        private Bitmap _map;
        private Graphics _graphics;
        private Pen _pen;
        private Point _lastPoint;
        private Color _currentColor = Color.Black;
        private int _brushSize = 2;


        public Form1()
        {
            InitializeComponent();
            InitializeGraphics();
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _client.Connect(host, port); //подключение клиента

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void ColorLabel_Click(object sender, EventArgs e)
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            inputUsernameLabel.Visible = false;
            inputUsername.Visible = false;
            enterButton.Visible = false;

            usernameInfo.Visible = true;
            colorInfo.Visible = true;
            button1.Visible = true;
            usernameValue.Visible = true;
            listPlayerLabel.Visible = true;
            listBoxPlayer.Visible = true;
            canvas.Visible = true;

            Username = inputUsername.Text;
            usernameValue.Text = Username;

            await _client.ConnectAsync();

            Id = Guid.NewGuid().ToString();

            _ = Task.Run(async () => await ReceiveMessageAsync());

            var message = new EventMessage()
            {
                Type = EventType.PlayerConnected,
                Username = Username,
                Id = Id
            };

            await SendMessageAsync(message);
        }

        private async Task SendMessageAsync(EventMessage message)
        {
            await _client.SendPacket(message);
        }

        private async Task ReceiveMessageAsync()
        {
            while (true)
            {
                try
                {
                    var messageRequest = await _client.RecivePacket<EventMessage>();

                    if (messageRequest == null) break;

                    var playersName = messageRequest.Players.Select(p => p.Username).ToArray();

                    switch (messageRequest?.Type)
                    {
                        case EventType.PlayerConnected:
                            Invoke(() =>
                            {
                                listBoxPlayer.Items.Clear();
                                listBoxPlayer.Items.AddRange(playersName);
                            });

                            break;

                        case EventType.PlayerDisconnected:
                            Invoke(() =>
                            {
                                listBoxPlayer.Items.Clear();
                                listBoxPlayer.Items.AddRange(playersName);
                            });

                            break;
                        case EventType.PlayerDraw:
                            Invoke(() =>
                            {
                                DrawFromMessage(messageRequest);
                            });
                            break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void DrawFromMessage(EventMessage message)
        {
            if (message.Points.Length >= 2)
            {
                using (Pen pen = new Pen(Color.FromArgb(message.Color), message.BrushSize))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    for (int i = 0; i < message.Points.Length - 1; i++)
                    {
                        _graphics.DrawLine(pen, message.Points[i], message.Points[i + 1]);
                    }
                }

                canvas.Image = _map;
            }
            else if (message.StartPoint != Point.Empty && message.EndPoint != Point.Empty)
            {
                using (Pen pen = new Pen(Color.FromArgb(message.Color), message.BrushSize))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    _graphics.DrawLine(pen, message.StartPoint, message.EndPoint);
                    canvas.Image = _map;
                }
            }
        }

        private async void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            IsMouse = true;
            _lastPoint = e.Location;
            await SendDrawStart(e.Location);
        }

        private async Task SendDrawStart(Point location)
        {
            var message = new EventMessage()
            {
                Type = EventType.DrawStart,
                Id = Id,
                StartPoint = location,
                Username = Username,
                Color = _currentColor.ToArgb(),
                BrushSize = _brushSize,
            };

            await _client.SendEventMessage(message);
        }

        private async void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            IsMouse = false;
            await SendDrawEnd();
        }

        private async Task SendDrawEnd()
        {
            var message = new EventMessage()
            {
                Type = EventType.DrawEnd,
                Id = Id,
            };

            await _client.SendPacket(message);
        }


        private async void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouse) return;

            _graphics.DrawLines(_pen, _lastPoint, e.Location);

            await SendDrawPoint(e.Location);

            _lastPoint = e.Location;

            canvas.Image = _map;
        }

        private async Task SendDrawPoint(Point point)
        {
            var message = new EventMessage()
            {
                Type = EventType.PlayerDraw,
                Id = Id,
                Username = Username,
                Color = _currentColor.ToArgb(),
                BrushSize = _brushSize,
                Points = [_lastPoint, point], 
            };

            await _client.SendPacket(message);
        }

        private void InitializeGraphics()
        {
            Rectangle rectangle = Screen.PrimaryScreen!.Bounds;

            _map = new Bitmap(rectangle.Width, rectangle.Height);
            _graphics = Graphics.FromImage(_map);
            _pen = new Pen(Color.Black, _brushSize);

            _pen.StartCap = LineCap.Round;
            _pen.EndCap = LineCap.Round;
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string username = inputUsername.Text;
            var message = new EventMessage()
            {
                Type = EventType.PlayerDisconnected,
                Username = username,
                Id = Id,
            };

            await SendMessageAsync(message);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

            _client.Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                _currentColor = colorDialog1.Color;

                _pen.Color = _currentColor;
                button1.BackColor = _currentColor;
                var message = new EventMessage()
                {
                    Type = EventType.PlayerSwitchColor,
                    Color = _currentColor.ToArgb(),
                    Id = Id,
                    BrushSize = _brushSize,
                };
            }
        }
    }
}
