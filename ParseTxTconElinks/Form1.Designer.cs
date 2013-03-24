namespace ParseTxTconElinks
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripDropDownButton_Opciones = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem_HTML_TextoPlano = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_HTML_New = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.textBox_TxT_Path = new System.Windows.Forms.TextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripDropDownButton_Opciones});
            this.statusStrip1.Location = new System.Drawing.Point(0, 237);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(478, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Margin = new System.Windows.Forms.Padding(0, 3, 100, 2);
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(214, 17);
            this.toolStripStatusLabel1.Text = "Ningún fichero txt con elinks cargado...";
            // 
            // toolStripDropDownButton_Opciones
            // 
            this.toolStripDropDownButton_Opciones.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripDropDownButton_Opciones.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton_Opciones.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_HTML_TextoPlano,
            this.toolStripMenuItem_HTML_New});
            this.toolStripDropDownButton_Opciones.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton_Opciones.Image")));
            this.toolStripDropDownButton_Opciones.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton_Opciones.Name = "toolStripDropDownButton_Opciones";
            this.toolStripDropDownButton_Opciones.Size = new System.Drawing.Size(70, 20);
            this.toolStripDropDownButton_Opciones.Text = "Opciones";
            this.toolStripDropDownButton_Opciones.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // toolStripMenuItem_HTML_TextoPlano
            // 
            this.toolStripMenuItem_HTML_TextoPlano.CheckOnClick = true;
            this.toolStripMenuItem_HTML_TextoPlano.Name = "toolStripMenuItem_HTML_TextoPlano";
            this.toolStripMenuItem_HTML_TextoPlano.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_HTML_TextoPlano.Text = "HTML Texto Plano";
            this.toolStripMenuItem_HTML_TextoPlano.Click += new System.EventHandler(this.toolStripMenuItem_HTML_TextoPlano_Click);
            // 
            // toolStripMenuItem_HTML_New
            // 
            this.toolStripMenuItem_HTML_New.Checked = true;
            this.toolStripMenuItem_HTML_New.CheckOnClick = true;
            this.toolStripMenuItem_HTML_New.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripMenuItem_HTML_New.Name = "toolStripMenuItem_HTML_New";
            this.toolStripMenuItem_HTML_New.Size = new System.Drawing.Size(172, 22);
            this.toolStripMenuItem_HTML_New.Text = "HTML \"Bonito\"";
            this.toolStripMenuItem_HTML_New.Click += new System.EventHandler(this.toolStripMenuItem_HTML_New_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.AllowDrop = true;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.IsSplitterFixed = true;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.AllowDrop = true;
            this.splitContainer1.Panel1.Controls.Add(this.textBox_TxT_Path);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(478, 237);
            this.splitContainer1.SplitterDistance = 409;
            this.splitContainer1.SplitterWidth = 1;
            this.splitContainer1.TabIndex = 1;
            // 
            // textBox_TxT_Path
            // 
            this.textBox_TxT_Path.AllowDrop = true;
            this.textBox_TxT_Path.BackColor = System.Drawing.Color.White;
            this.textBox_TxT_Path.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBox_TxT_Path.Location = new System.Drawing.Point(0, 0);
            this.textBox_TxT_Path.Multiline = true;
            this.textBox_TxT_Path.Name = "textBox_TxT_Path";
            this.textBox_TxT_Path.ReadOnly = true;
            this.textBox_TxT_Path.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_TxT_Path.Size = new System.Drawing.Size(409, 237);
            this.textBox_TxT_Path.TabIndex = 0;
            this.textBox_TxT_Path.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBox1_DragDrop);
            this.textBox_TxT_Path.DragOver += new System.Windows.Forms.DragEventHandler(this.textBox1_DragOver);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.IsSplitterFixed = true;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.button1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.button2);
            this.splitContainer2.Size = new System.Drawing.Size(68, 237);
            this.splitContainer2.SplitterDistance = 129;
            this.splitContainer2.SplitterWidth = 1;
            this.splitContainer2.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button1.Location = new System.Drawing.Point(0, 0);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(68, 129);
            this.button1.TabIndex = 0;
            this.button1.Text = "Buscar txt con elinks";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.button2.Enabled = false;
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(0, 0);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(68, 107);
            this.button2.TabIndex = 0;
            this.button2.Text = "Generar HTML";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "txt";
            this.openFileDialog1.Filter = "txt files |*.txt";
            this.openFileDialog1.Multiselect = true;
            this.openFileDialog1.ShowReadOnly = true;
            this.openFileDialog1.Title = "Selecciona los txt\'s que contienen los elinks";
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "html";
            this.saveFileDialog1.FileName = "Recopilacion_de_eLinks";
            this.saveFileDialog1.Filter = "HTML |*.html";
            this.saveFileDialog1.OverwritePrompt = false;
            this.saveFileDialog1.RestoreDirectory = true;
            this.saveFileDialog1.SupportMultiDottedExtensions = true;
            this.saveFileDialog1.Title = "Guardar HTML con los eLinks procesados";
            this.saveFileDialog1.FileOk += new System.ComponentModel.CancelEventHandler(this.saveFileDialog1_FileOk);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 259);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Convertidor de TxT con elinks a HTML";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.TextBox textBox_TxT_Path;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton_Opciones;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_HTML_TextoPlano;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_HTML_New;
    }
}

