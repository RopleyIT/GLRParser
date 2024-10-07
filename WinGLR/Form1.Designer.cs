
namespace WinGLR
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.chkTables = new System.Windows.Forms.CheckBox();
            this.chkDebugOutput = new System.Windows.Forms.CheckBox();
            this.chkCompressSates = new System.Windows.Forms.CheckBox();
            this.chkErrorToken = new System.Windows.Forms.CheckBox();
            this.chkGLRParser = new System.Windows.Forms.CheckBox();
            this.chkFSM = new System.Windows.Forms.CheckBox();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.ofdGrammar = new System.Windows.Forms.OpenFileDialog();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(32, 32);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(422, 40);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileOpen,
            this.mnuFileExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(71, 36);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileOpen
            // 
            this.mnuFileOpen.Name = "mnuFileOpen";
            this.mnuFileOpen.Size = new System.Drawing.Size(221, 44);
            this.mnuFileOpen.Text = "&Open...";
            this.mnuFileOpen.Click += new System.EventHandler(this.mnuFileOpen_Click);
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Name = "mnuFileExit";
            this.mnuFileExit.Size = new System.Drawing.Size(221, 44);
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // chkTables
            // 
            this.chkTables.AutoSize = true;
            this.chkTables.Checked = true;
            this.chkTables.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTables.Location = new System.Drawing.Point(34, 69);
            this.chkTables.Name = "chkTables";
            this.chkTables.Size = new System.Drawing.Size(296, 36);
            this.chkTables.TabIndex = 1;
            this.chkTables.Text = "Generate table data file";
            this.chkTables.UseVisualStyleBackColor = true;
            // 
            // chkDebugOutput
            // 
            this.chkDebugOutput.AutoSize = true;
            this.chkDebugOutput.Location = new System.Drawing.Point(34, 124);
            this.chkDebugOutput.Name = "chkDebugOutput";
            this.chkDebugOutput.Size = new System.Drawing.Size(298, 36);
            this.chkDebugOutput.TabIndex = 2;
            this.chkDebugOutput.Text = "Generate debug output";
            this.chkDebugOutput.UseVisualStyleBackColor = true;
            // 
            // chkCompressSates
            // 
            this.chkCompressSates.AutoSize = true;
            this.chkCompressSates.Checked = true;
            this.chkCompressSates.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCompressSates.Location = new System.Drawing.Point(34, 179);
            this.chkCompressSates.Name = "chkCompressSates";
            this.chkCompressSates.Size = new System.Drawing.Size(316, 36);
            this.chkCompressSates.TabIndex = 3;
            this.chkCompressSates.Text = "Compress identical states";
            this.chkCompressSates.UseVisualStyleBackColor = true;
            // 
            // chkErrorToken
            // 
            this.chkErrorToken.AutoSize = true;
            this.chkErrorToken.Checked = true;
            this.chkErrorToken.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkErrorToken.Location = new System.Drawing.Point(34, 234);
            this.chkErrorToken.Name = "chkErrorToken";
            this.chkErrorToken.Size = new System.Drawing.Size(319, 36);
            this.chkErrorToken.TabIndex = 4;
            this.chkErrorToken.Text = "Bison-style error recovery";
            this.chkErrorToken.UseVisualStyleBackColor = true;
            // 
            // chkGLRParser
            // 
            this.chkGLRParser.AutoSize = true;
            this.chkGLRParser.Location = new System.Drawing.Point(34, 289);
            this.chkGLRParser.Name = "chkGLRParser";
            this.chkGLRParser.Size = new System.Drawing.Size(331, 36);
            this.chkGLRParser.TabIndex = 5;
            this.chkGLRParser.Text = "GLR parser instead of LR(1)";
            this.chkGLRParser.UseVisualStyleBackColor = true;
            // 
            // chkFSM
            // 
            this.chkFSM.AutoSize = true;
            this.chkFSM.Location = new System.Drawing.Point(34, 344);
            this.chkFSM.Name = "chkFSM";
            this.chkFSM.Size = new System.Drawing.Size(309, 36);
            this.chkFSM.TabIndex = 6;
            this.chkFSM.Text = "FSM instead of grammar";
            this.chkFSM.UseVisualStyleBackColor = true;
            // 
            // btnGenerate
            // 
            this.btnGenerate.Enabled = false;
            this.btnGenerate.Location = new System.Drawing.Point(137, 400);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(150, 46);
            this.btnGenerate.TabIndex = 7;
            this.btnGenerate.Text = "Generate";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // ofdGrammar
            // 
            this.ofdGrammar.DefaultExt = "g";
            this.ofdGrammar.Filter = "Grammars (*.g)|*.g|All files (*.*)|*.*";
            this.ofdGrammar.Title = "Input grammar";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(422, 475);
            this.Controls.Add(this.btnGenerate);
            this.Controls.Add(this.chkFSM);
            this.Controls.Add(this.chkGLRParser);
            this.Controls.Add(this.chkErrorToken);
            this.Controls.Add(this.chkCompressSates);
            this.Controls.Add(this.chkDebugOutput);
            this.Controls.Add(this.chkTables);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "GLR Parser";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
        private System.Windows.Forms.ToolStripMenuItem mnuFileOpen;
        private System.Windows.Forms.CheckBox chkTables;
        private System.Windows.Forms.CheckBox chkDebugOutput;
        private System.Windows.Forms.CheckBox chkCompressSates;
        private System.Windows.Forms.CheckBox chkErrorToken;
        private System.Windows.Forms.CheckBox chkGLRParser;
        private System.Windows.Forms.CheckBox chkFSM;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.OpenFileDialog ofdGrammar;
    }
}

