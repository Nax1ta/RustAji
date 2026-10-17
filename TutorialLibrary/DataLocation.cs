using System.Text.Json;

namespace TutorialLibrary;

public static class DataLocation
{
    static string ConfigFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "腐味");
    static string ConfigFile => Path.Combine(ConfigFolder, "location.json");
    public static string DefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "腐味");

    public static string Read()
    {
        if (!File.Exists(ConfigFile)) return DefaultFolder;
        using var document = JsonDocument.Parse(File.ReadAllText(ConfigFile));
        string folder = document.RootElement.GetProperty("dataFolder").GetString() ?? throw new InvalidDataException("资料位置设置为空。 ");
        if (!Path.IsPathFullyQualified(folder)) throw new InvalidDataException("资料位置必须是完整路径。 ");
        if (!Directory.Exists(folder) || !File.Exists(Path.Combine(folder, "library.json")))
            throw new DirectoryNotFoundException("资料目录无法访问。请连接保存资料的磁盘或同步盘：" + folder);
        return Path.GetFullPath(folder);
    }

    public static void Save(string folder)
    {
        folder = Path.GetFullPath(folder);
        if (!File.Exists(Path.Combine(folder, "library.json"))) throw new InvalidDataException("目标目录没有腐味资料文件。 ");
        Directory.CreateDirectory(ConfigFolder);
        string temp = ConfigFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new { dataFolder = folder }));
        File.Move(temp, ConfigFile, true);
    }

    public static string Migrate(DataStore source, LibraryData data, string destination)
    {
        destination = Path.GetFullPath(destination);
        string original = Path.GetFullPath(source.Folder);
        if (destination.Equals(original, StringComparison.OrdinalIgnoreCase) ||
            destination.StartsWith(original + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            original.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("新位置不能是当前资料目录或它的上级、下级。 ");
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new IOException("目标位置已存在。请选择另一个父文件夹，或使用“打开已有资料库”。 ");
        source.Save(data);
        string parent = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(parent);
        string staging = Path.Combine(parent, ".腐味迁移-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            foreach (string name in new[] { "library.json", "library.json.bak" })
            {
                string file = Path.Combine(original, name);
                if (File.Exists(file)) File.Copy(file, Path.Combine(staging, name));
            }
            foreach (string name in new[] { "images", "assets" })
            {
                string from = Path.Combine(original, name), to = Path.Combine(staging, name);
                Directory.CreateDirectory(to);
                if (!Directory.Exists(from)) continue;
                foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.TopDirectoryOnly))
                    File.Copy(file, Path.Combine(to, Path.GetFileName(file)));
            }
            var check = new DataStore(staging).Load();
            if (check.Folders.Count != data.Folders.Count || check.Items.Count != data.Items.Count)
                throw new InvalidDataException("迁移后资料数量与原目录不一致。 ");
            Directory.Move(staging, destination);
            return destination;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }
}

public enum DataLocationChoice { None, Migrate, OpenExisting }

public sealed class DataLocationForm : Form
{
    public DataLocationChoice Choice { get; private set; }
    public string? Destination { get; private set; }

    public DataLocationForm(string currentFolder)
    {
        Text = "资料位置";
        Width = 640; Height = 310; MinimumSize = new Size(540, 290);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        Font = new Font("Microsoft YaHei UI", 9);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(18, 15, 18, 12) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 47));
        Controls.Add(layout);
        layout.Controls.Add(new Label { Text = "当前资料目录", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        layout.Controls.Add(new TextBox { Text = currentFolder, ReadOnly = true, Dock = DockStyle.Fill, TabStop = false }, 0, 1);
        layout.Controls.Add(new Label { Text = "迁移会复制资料、图片与备份到新位置；原目录保留，确认无误后可自行处理。", Dock = DockStyle.Fill, ForeColor = Color.DimGray }, 0, 2);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true };
        var migrate = new Button { Text = "迁移当前资料…", AutoSize = true, Height = 31 };
        var open = new Button { Text = "打开已有资料库…", AutoSize = true, Height = 31 };
        var folder = new Button { Text = "查看当前文件夹", AutoSize = true, Height = 31 };
        migrate.Click += (_, _) =>
        {
            using var picker = new FolderBrowserDialog { Description = "选择新资料目录的父文件夹", UseDescriptionForTitle = true };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            Destination = Path.Combine(picker.SelectedPath, "腐味资料");
            Choice = DataLocationChoice.Migrate;
            DialogResult = DialogResult.OK;
        };
        open.Click += (_, _) =>
        {
            using var picker = new FolderBrowserDialog { Description = "选择含有 library.json 的资料文件夹", UseDescriptionForTitle = true };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            Destination = picker.SelectedPath;
            Choice = DataLocationChoice.OpenExisting;
            DialogResult = DialogResult.OK;
        };
        folder.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(currentFolder) { UseShellExecute = true }); }
            catch (Exception error) { MessageBox.Show(this, error.Message, "无法打开文件夹", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        };
        actions.Controls.AddRange([migrate, open, folder]);
        Shown += (_, _) => migrate.Focus();
        layout.Controls.Add(actions, 0, 3);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        bottom.Controls.Add(new Button { Text = "关闭", Width = 78, Height = 29, DialogResult = DialogResult.Cancel });
        layout.Controls.Add(bottom, 0, 4);
    }
}
