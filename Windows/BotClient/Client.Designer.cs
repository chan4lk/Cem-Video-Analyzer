namespace BotClient
{
    partial class Client
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
            this.chatHistoryText = new System.Windows.Forms.TextBox();
            this.metaText = new System.Windows.Forms.TextBox();
            this.sendText = new System.Windows.Forms.TextBox();
            this.sendButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chatHistoryText
            // 
            this.chatHistoryText.Location = new System.Drawing.Point(16, 15);
            this.chatHistoryText.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.chatHistoryText.Multiline = true;
            this.chatHistoryText.Name = "chatHistoryText";
            this.chatHistoryText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.chatHistoryText.Size = new System.Drawing.Size(709, 414);
            this.chatHistoryText.TabIndex = 0;
            // 
            // metaText
            // 
            this.metaText.Location = new System.Drawing.Point(771, 15);
            this.metaText.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.metaText.Multiline = true;
            this.metaText.Name = "metaText";
            this.metaText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.metaText.Size = new System.Drawing.Size(321, 414);
            this.metaText.TabIndex = 1;
            // 
            // sendText
            // 
            this.sendText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.sendText.Location = new System.Drawing.Point(17, 490);
            this.sendText.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.sendText.Name = "sendText";
            this.sendText.Size = new System.Drawing.Size(708, 30);
            this.sendText.TabIndex = 2;
            this.sendText.KeyUp += new System.Windows.Forms.KeyEventHandler(this.sendText_KeyUp);
            // 
            // sendButton
            // 
            this.sendButton.Location = new System.Drawing.Point(771, 466);
            this.sendButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.sendButton.Name = "sendButton";
            this.sendButton.Size = new System.Drawing.Size(322, 76);
            this.sendButton.TabIndex = 3;
            this.sendButton.Text = "Send";
            this.sendButton.UseVisualStyleBackColor = true;
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            // 
            // Client
            // 
            this.AcceptButton = this.sendButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1115, 558);
            this.Controls.Add(this.sendButton);
            this.Controls.Add(this.sendText);
            this.Controls.Add(this.metaText);
            this.Controls.Add(this.chatHistoryText);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Client";
            this.Text = "Connect with bot";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Client_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox chatHistoryText;
        private System.Windows.Forms.TextBox metaText;
        private System.Windows.Forms.TextBox sendText;
        private System.Windows.Forms.Button sendButton;
    }
}

