namespace Rynchodon.PluginManager
{
	partial class Help
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Help));
			this.textBoxHelp = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// textBoxHelp
			// 
			this.textBoxHelp.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxHelp.Location = new System.Drawing.Point(0, 0);
			this.textBoxHelp.Multiline = true;
			this.textBoxHelp.Name = "textBoxHelp";
			this.textBoxHelp.ReadOnly = true;
			this.textBoxHelp.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textBoxHelp.Size = new System.Drawing.Size(869, 521);
			this.textBoxHelp.TabIndex = 1;
			this.textBoxHelp.Text = resources.GetString("textBoxHelp.Text");
			// 
			// Help
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(869, 521);
			this.Controls.Add(this.textBoxHelp);
			this.Name = "Help";
			this.Text = "SEPL Help";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxHelp;
	}
}