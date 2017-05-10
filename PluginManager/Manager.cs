using Rynchodon.PluginLoader;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Rynchodon.PluginManager
{
	internal class Manager : Form
	{
		private readonly Loader _loader = new Loader(false);
		private readonly List<string> _errors = new List<string>();

		private bool _needsSave;

		private DataGridView PluginConfig;
		private Button buttonSave;
		private TextBox textBoxOutput;
		private DataGridViewTextBoxColumn ColumnAuthor;
		private DataGridViewTextBoxColumn ColumnRepo;
		private DataGridViewCheckBoxColumn ColumnPreRelease;
		private Button buttonHelp;
		private Button Launch;
		private Button buttonLaunchDs;
		private Help help;

		public Manager()
		{
			InitializeComponent();
			foreach (PluginConfig config in _loader.GitHubConfig)
				PluginConfig.Rows.Add(config.name.author, config.name.repository, config.downloadPrerelease);
			PluginConfig.CellEndEdit += PluginConfig_CellEndEdit;
		}

		private void PluginConfig_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			_needsSave = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			if (_needsSave)
			{
				DialogResult choice = MessageBox.Show("Save changes?", "Save", MessageBoxButtons.YesNo);
				switch (choice)
				{
					case DialogResult.Yes:
						SavePluginConfig(false);
						break;
					case DialogResult.No:
						break;
				}
			}
			base.OnClosed(e);
		}

		private void SavePluginConfig(bool checkForRepo)
		{
			_needsSave = false;
			List<PluginConfig> pluginConfigs = new List<PluginConfig>();
			List<GitHubClient> checkRepo = new List<GitHubClient>();

			foreach (DataGridViewRow row in PluginConfig.Rows)
			{
				PluginConfig info = new PluginConfig(
					new PluginName((string)row.Cells[0].EditedFormattedValue,
					(string)row.Cells[1].EditedFormattedValue),
					(bool)row.Cells[2].EditedFormattedValue);

				bool noAuthor = string.IsNullOrWhiteSpace(info.name.author);
				bool noRepo = string.IsNullOrWhiteSpace(info.name.repository);
				if (noAuthor && noRepo)
					continue;

				if (noAuthor)
				{
					WriteLine("ERROR: No author for " + info.name.repository);
					continue;
				}

				if (noRepo)
				{
					WriteLine("ERROR: No repo for " + info.name.author);
					continue;
				}

				pluginConfigs.Add(info);
				if (checkForRepo)
					checkRepo.Add(new GitHubClient(info.name));
			}

			_loader.GitHubConfig = pluginConfigs;

			if (checkForRepo)
				foreach (GitHubClient client in checkRepo)
					if (client.GetReleases() == null)
						WriteLine("WARNING: Failed to connect to " + client.name);
		}

		private void WriteLine(string line)
		{
			_errors.Add(line);
			if (_errors.Count > 100)
				_errors.RemoveAt(0);

			textBoxOutput.ResetText();
			bool first = true;
			foreach (string error in _errors)
			{
				if (first)
					first = false;
				else
					textBoxOutput.AppendText(Environment.NewLine);
				textBoxOutput.AppendText(error);
			}
		}

		private void InitializeComponent()
		{
			this.Launch = new System.Windows.Forms.Button();
			this.PluginConfig = new System.Windows.Forms.DataGridView();
			this.ColumnAuthor = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnRepo = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnPreRelease = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.buttonSave = new System.Windows.Forms.Button();
			this.textBoxOutput = new System.Windows.Forms.TextBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.buttonLaunchDs = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.PluginConfig)).BeginInit();
			this.SuspendLayout();
			// 
			// Launch
			// 
			this.Launch.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.Launch.Location = new System.Drawing.Point(12, 236);
			this.Launch.Name = "Launch";
			this.Launch.Size = new System.Drawing.Size(160, 23);
			this.Launch.TabIndex = 0;
			this.Launch.Text = "Launch SE and SEPL";
			this.Launch.UseVisualStyleBackColor = true;
			this.Launch.Click += new System.EventHandler(this.Launch_Click);
			// 
			// PluginConfig
			// 
			this.PluginConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PluginConfig.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.PluginConfig.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnAuthor,
            this.ColumnRepo,
            this.ColumnPreRelease});
			this.PluginConfig.Location = new System.Drawing.Point(12, 12);
			this.PluginConfig.Name = "PluginConfig";
			this.PluginConfig.Size = new System.Drawing.Size(360, 189);
			this.PluginConfig.TabIndex = 2;
			// 
			// ColumnAuthor
			// 
			this.ColumnAuthor.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnAuthor.HeaderText = "Author";
			this.ColumnAuthor.Name = "ColumnAuthor";
			this.ColumnAuthor.ToolTipText = "GitHub author of the plugin.";
			this.ColumnAuthor.Width = 63;
			// 
			// ColumnRepo
			// 
			this.ColumnRepo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnRepo.HeaderText = "Repository";
			this.ColumnRepo.Name = "ColumnRepo";
			this.ColumnRepo.ToolTipText = "Name of GitHub repository to download the plugin from.";
			this.ColumnRepo.Width = 82;
			// 
			// ColumnPreRelease
			// 
			this.ColumnPreRelease.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnPreRelease.HeaderText = "Pre-Release";
			this.ColumnPreRelease.Name = "ColumnPreRelease";
			this.ColumnPreRelease.ToolTipText = "When enabled, SEPL will download pre-releases of the plugin.";
			this.ColumnPreRelease.Width = 71;
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.Location = new System.Drawing.Point(62, 207);
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size(76, 23);
			this.buttonSave.TabIndex = 3;
			this.buttonSave.Text = "Save";
			this.buttonSave.UseVisualStyleBackColor = true;
			this.buttonSave.Click += new System.EventHandler(this.button1_Click);
			// 
			// textBoxOutput
			// 
			this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxOutput.Location = new System.Drawing.Point(12, 286);
			this.textBoxOutput.Multiline = true;
			this.textBoxOutput.Name = "textBoxOutput";
			this.textBoxOutput.ReadOnly = true;
			this.textBoxOutput.Size = new System.Drawing.Size(360, 100);
			this.textBoxOutput.TabIndex = 4;
			this.textBoxOutput.WordWrap = false;
			// 
			// buttonHelp
			// 
			this.buttonHelp.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonHelp.Location = new System.Drawing.Point(246, 207);
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Size = new System.Drawing.Size(76, 23);
			this.buttonHelp.TabIndex = 5;
			this.buttonHelp.Text = "Help";
			this.buttonHelp.UseVisualStyleBackColor = true;
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			// 
			// buttonLaunchDs
			// 
			this.buttonLaunchDs.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonLaunchDs.Location = new System.Drawing.Point(180, 236);
			this.buttonLaunchDs.Name = "buttonLaunchDs";
			this.buttonLaunchDs.Size = new System.Drawing.Size(192, 23);
			this.buttonLaunchDs.TabIndex = 6;
			this.buttonLaunchDs.Text = "Launch DS and SEPL";
			this.buttonLaunchDs.UseVisualStyleBackColor = true;
			// 
			// Manager
			// 
			this.ClientSize = new System.Drawing.Size(384, 398);
			this.Controls.Add(this.buttonLaunchDs);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.textBoxOutput);
			this.Controls.Add(this.buttonSave);
			this.Controls.Add(this.PluginConfig);
			this.Controls.Add(this.Launch);
			this.Name = "Manager";
			((System.ComponentModel.ISupportInitialize)(this.PluginConfig)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		private void Launch_Click(object sender, EventArgs e)
		{
			Close();
			DllInjector.Run();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			SavePluginConfig(true);
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			help?.Close();
			help = new Help();
			help.Show();
		}
	}
}
