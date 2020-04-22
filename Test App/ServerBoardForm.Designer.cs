namespace testApp
{
    partial class ServerBoardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServerBoardForm));
            this._messageArea = new System.Windows.Forms.TextBox();
            this._message = new System.Windows.Forms.TextBox();
            this._start = new System.Windows.Forms.Button();
            this._send = new System.Windows.Forms.Button();
            this._server = new TcpServer.TcpServer(this.components);
            this.SuspendLayout();
            // 
            // _messageArea
            // 
            this._messageArea.Location = new System.Drawing.Point(12, 12);
            this._messageArea.Multiline = true;
            this._messageArea.Name = "_messageArea";
            this._messageArea.ReadOnly = true;
            this._messageArea.Size = new System.Drawing.Size(539, 209);
            this._messageArea.TabIndex = 0;
            // 
            // _message
            // 
            this._message.Location = new System.Drawing.Point(12, 227);
            this._message.Multiline = true;
            this._message.Name = "_message";
            this._message.Size = new System.Drawing.Size(539, 71);
            this._message.TabIndex = 1;
            // 
            // _start
            // 
            this._start.Location = new System.Drawing.Point(12, 305);
            this._start.Name = "_start";
            this._start.Size = new System.Drawing.Size(103, 23);
            this._start.TabIndex = 2;
            this._start.Text = "Start";
            this._start.UseVisualStyleBackColor = true;
            this._start.Click += new System.EventHandler(this._start_Click);
            // 
            // _send
            // 
            this._send.Location = new System.Drawing.Point(450, 304);
            this._send.Name = "_send";
            this._send.Size = new System.Drawing.Size(103, 23);
            this._send.TabIndex = 3;
            this._send.Text = "Send";
            this._send.UseVisualStyleBackColor = true;
            this._send.Click += new System.EventHandler(this._send_Click);
            // 
            // _server
            // 
            this._server.Encoding = ((System.Text.Encoding)(resources.GetObject("_server.Encoding")));
            this._server.IdleTime = 50;
            this._server.IsOpen = false;
            this._server.MaxCallbackThreads = 100;
            this._server.MaxSendAttempts = 3;
            this._server.Port = -1;
            this._server.VerifyConnectionInterval = 100;
            this._server.OnConnect += new TcpServer.TcpServerConnectionChanged(this._server_OnConnect);
            this._server.OnDataAvailable += new TcpServer.TcpServerConnectionChanged(this._server_OnDataAvailable);
            this._server.OnError += new TcpServer.TcpServerError(this._server_OnError);
            // 
            // ServerBoardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(563, 336);
            this.Controls.Add(this._send);
            this.Controls.Add(this._start);
            this.Controls.Add(this._message);
            this.Controls.Add(this._messageArea);
            this.Name = "ServerBoardForm";
            this.Text = "ServerBoardForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ServerBoardForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox _messageArea;
        private System.Windows.Forms.TextBox _message;
        private System.Windows.Forms.Button _start;
        private System.Windows.Forms.Button _send;
        private TcpServer.TcpServer _server;
    }
}