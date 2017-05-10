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
			this.labelPlaceholder = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// labelPlaceholder
			// 
			this.labelPlaceholder.AutoSize = true;
			this.labelPlaceholder.Location = new System.Drawing.Point(51, 57);
			this.labelPlaceholder.Name = "labelPlaceholder";
			this.labelPlaceholder.Size = new System.Drawing.Size(82, 13);
			this.labelPlaceholder.TabIndex = 0;
			this.labelPlaceholder.Text = "No help for you!";
			// 
			// Help
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.labelPlaceholder);
			this.Name = "Help";
			this.Text = "Help";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label labelPlaceholder;
	}
}