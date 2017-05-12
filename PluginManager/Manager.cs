using Rynchodon.PluginLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Rynchodon.PluginManager
{
	internal class Manager : Form
	{
		private enum Status { None, ConnectionFailed, Malformed, Searching, Success }

		private readonly Loader _loader = new Loader(false);
		private readonly List<string> _errors = new List<string>();

		private bool _needsSave;

		private DataGridView PluginConfig;
		private Button buttonSave;
		private TextBox textBoxOutput;
		private Button buttonHelp;
		private Button Launch;
		private Button buttonLaunchDs;
		private DataGridViewImageColumn dataGridViewImageColumn1;
		private DataGridViewCheckBoxColumn ColumnEnabled;
		private DataGridViewTextBoxColumn ColumnAuthor;
		private DataGridViewTextBoxColumn ColumnRepo;
		private DataGridViewCheckBoxColumn ColumnPreRelease;
		private DataGridViewImageColumn ColumnStatus;
		private DataGridViewImageColumn ColumnDelete;
		private TextBox textBoxPathToGit;
		private Label labelPathToGit;
		private PictureBox pictureBoxPathToGit;
		private Help help;

		public Manager()
		{
			InitializeComponent();

			PluginConfig.CellEndEdit += PluginConfig_CellEndEdit;
			PluginConfig.KeyPress += PluginConfig_KeyPress;
			PluginConfig.RowsAdded += PluginConfig_RowsAdded;
			PluginConfig.UserDeletedRow += PluginConfig_UserDeletedRow;

			for (int i = PluginConfig.Rows.Count - 1; i >= 0; --i)
				RowAdded(i);

			foreach (PluginConfig config in _loader.GitHubConfig)
			{
				DataGridViewRow newRow = PluginConfig.Rows[PluginConfig.Rows.Add()];
				newRow.Cells[ColumnEnabled.Index].Value = config.enabled;
				newRow.Cells[ColumnAuthor.Index].Value = config.name.author;
				newRow.Cells[ColumnRepo.Index].Value = config.name.repository;
				newRow.Cells[ColumnPreRelease.Index].Value = config.downloadPrerelease;
				CheckRow(newRow.Index);
			}

			textBoxPathToGit.Text = _loader.PathToGit;
			CheckGit();
			SetDeleteImage();

			_needsSave = false;
		}

		private bool CheckGit()
		{
			string path = textBoxPathToGit.Text;
			if (string.IsNullOrWhiteSpace(path))
			{
				pictureBoxPathToGit.Image = Properties.Resources.blank;
				return false;
			}
			if (File.Exists(path) && path.EndsWith("git.exe"))
			{
				pictureBoxPathToGit.Image = Properties.Resources.success;
				return true;
			}
			pictureBoxPathToGit.Image = Properties.Resources.malformed;
			return false;
		}

		private void RowAdded(int rowIndex)
		{
			PluginConfig.Rows[rowIndex].Cells[ColumnEnabled.Index].Value = true;
			PluginConfig.Rows[rowIndex].Cells[ColumnStatus.Index].Value = Properties.Resources.blank;
			PluginConfig.Rows[rowIndex].Cells[ColumnDelete.Index].Value = Properties.Resources.blank;
		}

		private void CheckRow(int rowIndex)
		{
			SetStatus(rowIndex, Status.Searching);
			DataGridViewRow row = PluginConfig.Rows[rowIndex];

			PluginConfig info = new PluginConfig(
					new PluginName((string)row.Cells[ColumnAuthor.Index].EditedFormattedValue,
					(string)row.Cells[ColumnRepo.Index].EditedFormattedValue),
					(bool)row.Cells[ColumnPreRelease.Index].EditedFormattedValue,
					(bool)row.Cells[ColumnEnabled.Index].EditedFormattedValue);

			bool noAuthor = string.IsNullOrWhiteSpace(info.name.author);
			bool noRepo = string.IsNullOrWhiteSpace(info.name.repository);
			if (noAuthor && noRepo)
			{
				SetStatus(rowIndex, Status.None);
				return;
			}

			if (noAuthor)
			{
				WriteLine("ERROR: No author for " + info.name.repository);
				SetStatus(rowIndex, Status.Malformed);
				return;
			}

			if (noRepo)
			{
				WriteLine("ERROR: No repo for " + info.name.author);
				SetStatus(rowIndex, Status.Malformed);
				return;
			}

			if ((new GitHubClient(info.name)).GetReleases() != null)
			{
				WriteLine("Connect to " + info.name.fullName);
				SetStatus(rowIndex, Status.Success);
			}
			else
			{
				WriteLine("WARNING: Failed to connect to " + info.name.fullName);
				SetStatus(rowIndex, Status.ConnectionFailed);
			}
		}

		private void SetStatus(int rowIndex, Status status)
		{
			DataGridViewCell statusCell = PluginConfig.Rows[rowIndex].Cells[ColumnStatus.Index];

			switch (status)
			{
				case Status.None:
					statusCell.Value = Properties.Resources.blank;
					break;
				case Status.ConnectionFailed:
					statusCell.Value = Properties.Resources.connection_failed;
					break;
				case Status.Malformed:
					statusCell.Value = Properties.Resources.malformed;
					break;
				case Status.Searching:
					statusCell.Value = Properties.Resources.search;
					break;
				case Status.Success:
					statusCell.Value = Properties.Resources.success;
					break;
			}

			DataGridViewCell deleteCell = PluginConfig.Rows[rowIndex].Cells[ColumnDelete.Index];
		}

		private void SetDeleteImage()
		{
			int rowIndex = PluginConfig.Rows.Count - 1;
			PluginConfig.Rows[rowIndex].Cells[ColumnDelete.Index].Value = Properties.Resources.blank;
			--rowIndex;
			for (; rowIndex >= 0; --rowIndex)
				PluginConfig.Rows[rowIndex].Cells[ColumnDelete.Index].Value = Properties.Resources.garbage;
		}

		private void SavePluginConfig()
		{
			_needsSave = false;
			List<PluginConfig> pluginConfigs = new List<PluginConfig>();

			foreach (DataGridViewRow row in PluginConfig.Rows)
			{
				PluginConfig info = new PluginConfig(
					new PluginName((string)row.Cells[ColumnAuthor.Index].EditedFormattedValue,
					(string)row.Cells[ColumnRepo.Index].EditedFormattedValue),
					(bool)row.Cells[ColumnPreRelease.Index].EditedFormattedValue,
					(bool)row.Cells[ColumnEnabled.Index].EditedFormattedValue);

				if (string.IsNullOrWhiteSpace(info.name.author) || string.IsNullOrWhiteSpace(info.name.repository))
					continue;

				pluginConfigs.Add(info);
			}

			_loader.GitHubConfig = pluginConfigs;
			if (CheckGit())
				_loader.PathToGit = textBoxPathToGit.Text;
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

		protected override void OnClosed(EventArgs e)
		{
			if (_needsSave)
			{
				DialogResult choice = MessageBox.Show("Save changes?", "Save", MessageBoxButtons.YesNo);
				switch (choice)
				{
					case DialogResult.Yes:
						SavePluginConfig();
						break;
					case DialogResult.No:
						break;
				}
			}
			base.OnClosed(e);
		}

		#region Event Handlers

		private void PluginConfig_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == ColumnDelete.Index && e.RowIndex < PluginConfig.Rows.Count - 1)
			{
				_needsSave = true;
				PluginConfig.Rows.RemoveAt(e.RowIndex);
			}
		}

		private void PluginConfig_CellEndEdit(object sender, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == ColumnAuthor.Index || e.ColumnIndex == ColumnRepo.Index)
			{
				_needsSave = true;
				CheckRow(e.RowIndex);
			}
		}

		private void PluginConfig_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == ' ' && PluginConfig.SelectedCells.Count == 1 && PluginConfig.SelectedCells[0].ColumnIndex == ColumnDelete.Index && PluginConfig.SelectedCells[0].RowIndex < PluginConfig.Rows.Count - 1)
			{
				_needsSave = true;
				PluginConfig.Rows.RemoveAt(PluginConfig.SelectedCells[0].RowIndex);
			}
		}

		private void PluginConfig_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			for (int rowIndex = e.RowCount + e.RowIndex - 1; rowIndex >= e.RowIndex; --rowIndex)
				RowAdded(rowIndex);
			SetDeleteImage();
		}

		private void PluginConfig_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
		{
			_needsSave = true;
		}

		private void Launch_Click(object sender, EventArgs e)
		{
			Close();
			DllInjector.Run(Launcher.PathBin64);
		}

		private void buttonSave_Click(object sender, EventArgs e)
		{
			SavePluginConfig();
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			help?.Close();
			help = new Help();
			help.Show();
		}

		private void textBoxPathToGit_TextChanged(object sender, EventArgs e)
		{
			_needsSave = true;
			CheckGit();
		}

		private void buttonLaunchDs_Click(object sender, EventArgs e)
		{
			Close();
			DllInjector.Run(Launcher.PathDedicated64);
		}

		#endregion Event Handlers

		private void InitializeComponent()
		{
			this.Launch = new System.Windows.Forms.Button();
			this.PluginConfig = new System.Windows.Forms.DataGridView();
			this.ColumnEnabled = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.ColumnAuthor = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnRepo = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnPreRelease = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.ColumnStatus = new System.Windows.Forms.DataGridViewImageColumn();
			this.ColumnDelete = new System.Windows.Forms.DataGridViewImageColumn();
			this.buttonSave = new System.Windows.Forms.Button();
			this.textBoxOutput = new System.Windows.Forms.TextBox();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.buttonLaunchDs = new System.Windows.Forms.Button();
			this.dataGridViewImageColumn1 = new System.Windows.Forms.DataGridViewImageColumn();
			this.textBoxPathToGit = new System.Windows.Forms.TextBox();
			this.labelPathToGit = new System.Windows.Forms.Label();
			this.pictureBoxPathToGit = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.PluginConfig)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxPathToGit)).BeginInit();
			this.SuspendLayout();
			// 
			// Launch
			// 
			this.Launch.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.Launch.Location = new System.Drawing.Point(205, 198);
			this.Launch.Name = "Launch";
			this.Launch.Size = new System.Drawing.Size(150, 23);
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
            this.ColumnEnabled,
            this.ColumnAuthor,
            this.ColumnRepo,
            this.ColumnPreRelease,
            this.ColumnStatus,
            this.ColumnDelete});
			this.PluginConfig.Location = new System.Drawing.Point(12, 12);
			this.PluginConfig.Name = "PluginConfig";
			this.PluginConfig.Size = new System.Drawing.Size(499, 180);
			this.PluginConfig.TabIndex = 2;
			this.PluginConfig.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.PluginConfig_CellContentClick);
			// 
			// ColumnEnabled
			// 
			this.ColumnEnabled.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnEnabled.HeaderText = "Enabled";
			this.ColumnEnabled.Name = "ColumnEnabled";
			this.ColumnEnabled.Width = 52;
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
			// ColumnStatus
			// 
			this.ColumnStatus.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnStatus.HeaderText = "Status";
			this.ColumnStatus.Name = "ColumnStatus";
			this.ColumnStatus.ReadOnly = true;
			this.ColumnStatus.Width = 43;
			// 
			// ColumnDelete
			// 
			this.ColumnDelete.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnDelete.HeaderText = "Delete";
			this.ColumnDelete.Name = "ColumnDelete";
			this.ColumnDelete.ReadOnly = true;
			this.ColumnDelete.Width = 44;
			// 
			// buttonSave
			// 
			this.buttonSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonSave.Location = new System.Drawing.Point(12, 198);
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.Size = new System.Drawing.Size(76, 23);
			this.buttonSave.TabIndex = 3;
			this.buttonSave.Text = "Save";
			this.buttonSave.UseVisualStyleBackColor = true;
			this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
			// 
			// textBoxOutput
			// 
			this.textBoxOutput.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxOutput.Location = new System.Drawing.Point(12, 269);
			this.textBoxOutput.Multiline = true;
			this.textBoxOutput.Name = "textBoxOutput";
			this.textBoxOutput.ReadOnly = true;
			this.textBoxOutput.Size = new System.Drawing.Size(499, 100);
			this.textBoxOutput.TabIndex = 4;
			this.textBoxOutput.WordWrap = false;
			// 
			// buttonHelp
			// 
			this.buttonHelp.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonHelp.Location = new System.Drawing.Point(94, 198);
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
			this.buttonLaunchDs.Location = new System.Drawing.Point(361, 198);
			this.buttonLaunchDs.Name = "buttonLaunchDs";
			this.buttonLaunchDs.Size = new System.Drawing.Size(150, 23);
			this.buttonLaunchDs.TabIndex = 6;
			this.buttonLaunchDs.Text = "Launch DS and SEPL";
			this.buttonLaunchDs.UseVisualStyleBackColor = true;
			this.buttonLaunchDs.Click += new System.EventHandler(this.buttonLaunchDs_Click);
			// 
			// dataGridViewImageColumn1
			// 
			this.dataGridViewImageColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.dataGridViewImageColumn1.HeaderText = "Status";
			this.dataGridViewImageColumn1.Name = "dataGridViewImageColumn1";
			this.dataGridViewImageColumn1.ReadOnly = true;
			// 
			// textBoxPathToGit
			// 
			this.textBoxPathToGit.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.textBoxPathToGit.Location = new System.Drawing.Point(145, 236);
			this.textBoxPathToGit.Name = "textBoxPathToGit";
			this.textBoxPathToGit.Size = new System.Drawing.Size(340, 20);
			this.textBoxPathToGit.TabIndex = 7;
			this.textBoxPathToGit.TextChanged += new System.EventHandler(this.textBoxPathToGit_TextChanged);
			// 
			// labelPathToGit
			// 
			this.labelPathToGit.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.labelPathToGit.AutoSize = true;
			this.labelPathToGit.Location = new System.Drawing.Point(12, 236);
			this.labelPathToGit.Name = "labelPathToGit";
			this.labelPathToGit.Size = new System.Drawing.Size(127, 13);
			this.labelPathToGit.TabIndex = 8;
			this.labelPathToGit.Text = "(Modder Only) Path to git:";
			// 
			// pictureBoxPathToGit
			// 
			this.pictureBoxPathToGit.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.pictureBoxPathToGit.Location = new System.Drawing.Point(491, 236);
			this.pictureBoxPathToGit.Name = "pictureBoxPathToGit";
			this.pictureBoxPathToGit.Size = new System.Drawing.Size(20, 20);
			this.pictureBoxPathToGit.TabIndex = 9;
			this.pictureBoxPathToGit.TabStop = false;
			// 
			// Manager
			// 
			this.ClientSize = new System.Drawing.Size(523, 381);
			this.Controls.Add(this.pictureBoxPathToGit);
			this.Controls.Add(this.labelPathToGit);
			this.Controls.Add(this.textBoxPathToGit);
			this.Controls.Add(this.buttonLaunchDs);
			this.Controls.Add(this.buttonHelp);
			this.Controls.Add(this.textBoxOutput);
			this.Controls.Add(this.buttonSave);
			this.Controls.Add(this.Launch);
			this.Controls.Add(this.PluginConfig);
			this.Name = "Manager";
			((System.ComponentModel.ISupportInitialize)(this.PluginConfig)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxPathToGit)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

	}
}
