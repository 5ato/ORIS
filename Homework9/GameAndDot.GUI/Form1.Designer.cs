namespace GameAndDot.GUI
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            listBoxPlayer = new ListBox();
            inputUsernameLabel = new Label();
            usernameValue = new Label();
            listPlayerLabel = new Label();
            inputUsername = new TextBox();
            enterButton = new Button();
            colorInfo = new Label();
            usernameInfo = new Label();
            canvas = new PictureBox();
            colorDialog1 = new ColorDialog();
            button1 = new Button();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // listBoxPlayer
            // 
            listBoxPlayer.FormattingEnabled = true;
            listBoxPlayer.Items.AddRange(new object[] { "user2", "user3" });
            listBoxPlayer.Location = new Point(624, 128);
            listBoxPlayer.Name = "listBoxPlayer";
            listBoxPlayer.Size = new Size(139, 264);
            listBoxPlayer.TabIndex = 0;
            listBoxPlayer.Visible = false;
            // 
            // inputUsernameLabel
            // 
            inputUsernameLabel.AutoSize = true;
            inputUsernameLabel.Location = new Point(149, 237);
            inputUsernameLabel.Name = "inputUsernameLabel";
            inputUsernameLabel.Size = new Size(83, 20);
            inputUsernameLabel.TabIndex = 1;
            inputUsernameLabel.Text = "Ваше Имя:";
            inputUsernameLabel.Click += label1_Click;
            // 
            // usernameValue
            // 
            usernameValue.AutoSize = true;
            usernameValue.Location = new Point(706, 22);
            usernameValue.Name = "usernameValue";
            usernameValue.Size = new Size(48, 20);
            usernameValue.TabIndex = 3;
            usernameValue.Text = "Color:";
            usernameValue.Visible = false;
            usernameValue.Click += ColorLabel_Click;
            // 
            // listPlayerLabel
            // 
            listPlayerLabel.AutoSize = true;
            listPlayerLabel.Location = new Point(634, 105);
            listPlayerLabel.Name = "listPlayerLabel";
            listPlayerLabel.Size = new Size(120, 20);
            listPlayerLabel.TabIndex = 4;
            listPlayerLabel.Text = "Список игроков";
            listPlayerLabel.Visible = false;
            // 
            // inputUsername
            // 
            inputUsername.Location = new Point(287, 237);
            inputUsername.Name = "inputUsername";
            inputUsername.Size = new Size(125, 27);
            inputUsername.TabIndex = 2;
            // 
            // enterButton
            // 
            enterButton.Location = new Point(447, 235);
            enterButton.Name = "enterButton";
            enterButton.Size = new Size(94, 29);
            enterButton.TabIndex = 5;
            enterButton.Text = "Войти";
            enterButton.UseVisualStyleBackColor = true;
            enterButton.Click += button1_Click;
            // 
            // colorInfo
            // 
            colorInfo.AutoSize = true;
            colorInfo.Location = new Point(634, 65);
            colorInfo.Name = "colorInfo";
            colorInfo.Size = new Size(45, 20);
            colorInfo.TabIndex = 6;
            colorInfo.Text = "Color";
            colorInfo.Visible = false;
            colorInfo.Click += label4_Click;
            // 
            // usernameInfo
            // 
            usernameInfo.AutoSize = true;
            usernameInfo.Location = new Point(624, 22);
            usernameInfo.Name = "usernameInfo";
            usernameInfo.Size = new Size(75, 20);
            usernameInfo.TabIndex = 7;
            usernameInfo.Text = "Username";
            usernameInfo.Visible = false;
            usernameInfo.Click += label5_Click;
            // 
            // canvas
            // 
            canvas.Location = new Point(12, 12);
            canvas.Name = "canvas";
            canvas.Size = new Size(590, 426);
            canvas.TabIndex = 9;
            canvas.TabStop = false;
            canvas.Visible = false;
            canvas.MouseDown += pictureBox1_MouseDown;
            canvas.MouseMove += pictureBox1_MouseMove;
            canvas.MouseUp += pictureBox1_MouseUp;
            // 
            // colorDialog1
            // 
            colorDialog1.AnyColor = true;
            // 
            // button1
            // 
            button1.BackColor = Color.Black;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(694, 61);
            button1.Name = "button1";
            button1.Size = new Size(30, 30);
            button1.TabIndex = 10;
            button1.UseVisualStyleBackColor = false;
            button1.Visible = false;
            button1.Click += button1_Click_1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(canvas);
            Controls.Add(usernameInfo);
            Controls.Add(colorInfo);
            Controls.Add(enterButton);
            Controls.Add(listPlayerLabel);
            Controls.Add(usernameValue);
            Controls.Add(inputUsername);
            Controls.Add(inputUsernameLabel);
            Controls.Add(listBoxPlayer);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox listBoxPlayer;
        private Label inputUsernameLabel;
        private Label usernameValue;
        private Label listPlayerLabel;
        private TextBox inputUsername;
        private Button enterButton;
        private Label colorInfo;
        private Label usernameInfo;
        private PictureBox canvas;
        private ColorDialog colorDialog1;
        private Button button1;
    }
}
