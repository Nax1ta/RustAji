using System.Text.Json;

namespace TutorialLibrary;

public static class DataLocation
{
    const string AppFolderName = "RustAji";
    static string ConfigFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);
    static string ConfigFile => Path.Combine(ConfigFolder, "location.json");
    public static string DefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppFolderName);
    public static string LegacyDefaultFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "腐味");

    public static string Read() => ReadInstalledFromRoots(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string ReadInstalledFromRoots(string localRoot, string roamingRoot)
    {
        string current = ReadFromRoots(localRoot, roamingRoot);
        string configFolder = Path.Combine(roamingRoot, AppFolderName);
        string request = Path.Combine(configFolder, "install-location.txt");
        if (!File.Exists(request)) return current;

        string selected = File.ReadAllText(request).Trim().TrimStart('\uFEFF');
        if (!Path.IsPathFullyQualified(selected)) throw new InvalidDataException("安装时选择的资料路径无效。");
        selected = Path.GetFullPath(selected);
        string previous = Path.GetFullPath(current);
        if (selected.Equals(previous, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(request);
            return current;
        }
        if (selected.StartsWith(previous + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            previous.StartsWith(selected + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("资料目录不能是原资料目录的上级或下级。");
        if (File.Exists(Path.Combine(selected, "library.json")))
        {
            // Selecting an existing library means opening it, not replacing its contents.
            WriteConfig(Path.Combine(configFolder, "location.json"), selected);
            File.Delete(request);
            return selected;
        }
        if (Directory.Exists(selected) && Directory.EnumerateFileSystemEntries(selected).Any())
            throw new IOException("安装时选择的资料目录不是空目录，也不是已有的 RustAji 资料库。");
        if (File.Exists(Path.Combine(current, "library.json"))) CopyLegacyLibrary(current, selected);
        else Directory.CreateDirectory(selected);
        WriteConfig(Path.Combine(configFolder, "location.json"), selected);
        File.Delete(request);
        return selected;
    }

    // Separate roots let migration be checked without touching a user's real library.
    public static string ReadFromRoots(string localRoot, string roamingRoot)
    {
        string currentDefault = Path.Combine(localRoot, AppFolderName);
        string oldDefault = Path.Combine(localRoot, "腐味");
        string currentConfig = Path.Combine(roamingRoot, AppFolderName, "location.json");
        string oldConfig = Path.Combine(roamingRoot, "腐味", "location.json");
        bool fromLegacyConfig = !File.Exists(currentConfig) && File.Exists(oldConfig);
        string? configured = File.Exists(currentConfig) ? ReadConfig(currentConfig) : fromLegacyConfig ? ReadConfig(oldConfig) : null;

        if (configured is not null)
        {
            string oldLibrary = Path.GetFullPath(oldDefault);
            if (Path.GetFullPath(configured).Equals(oldLibrary, StringComparison.OrdinalIgnoreCase))
            {
                if (!File.Exists(Path.Combine(currentDefault, "library.json"))) CopyLegacyLibrary(oldDefault, currentDefault);
                WriteConfig(currentConfig, currentDefault);
                return currentDefault;
            }
            if (fromLegacyConfig && Path.GetFileName(Path.TrimEndingDirectorySeparator(configured)).Equals("腐味资料", StringComparison.OrdinalIgnoreCase))
            {
                string replacement = Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(configured))!, "RustAjiLibrary");
                if (!File.Exists(Path.Combine(replacement, "library.json"))) CopyLegacyLibrary(configured, replacement);
                WriteConfig(currentConfig, replacement);
                return replacement;
            }
            string existing = RequireLibrary(configured);
            if (fromLegacyConfig) WriteConfig(currentConfig, existing);
            return existing;
        }
        if (File.Exists(Path.Combine(oldDefault, "library.json")) && !File.Exists(Path.Combine(currentDefault, "library.json")))
            CopyLegacyLibrary(oldDefault, currentDefault);
        return currentDefault;
    }

    static string ReadConfig(string configFile)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(configFile));
        return document.RootElement.GetProperty("dataFolder").GetString() ?? throw new InvalidDataException("资料位置设置为空。 ");
    }

    static string RequireLibrary(string folder)
    {
        if (!Path.IsPathFullyQualified(folder)) throw new InvalidDataException("资料位置必须是完整路径。 ");
        if (!Directory.Exists(folder) || !File.Exists(Path.Combine(folder, "library.json")))
            throw new DirectoryNotFoundException("资料目录无法访问。请连接保存资料的磁盘或同步盘：" + folder);
        return Path.GetFullPath(folder);
    }

    static void CopyLegacyLibrary(string source, string destination)
    {
        if (!File.Exists(Path.Combine(source, "library.json"))) throw new FileNotFoundException("旧资料目录缺少 library.json。", source);
        if (File.Exists(Path.Combine(destination, "library.json"))) return;
        Directory.CreateDirectory(destination);
        foreach (string subfolder in new[] { "images", "assets" })
        {
            string from = Path.Combine(source, subfolder), to = Path.Combine(destination, subfolder);
            Directory.CreateDirectory(to);
            if (!Directory.Exists(from)) continue;
            foreach (string file in Directory.EnumerateFiles(from, "*", SearchOption.TopDirectoryOnly))
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
        }
        string oldBackup = Path.Combine(source, "library.json.bak");
        if (File.Exists(oldBackup)) File.Copy(oldBackup, Path.Combine(destination, "library.json.bak"), true);
        string temp = Path.Combine(destination, "library.json.tmp");
        File.Copy(Path.Combine(source, "library.json"), temp, true);
        File.Move(temp, Path.Combine(destination, "library.json"), true);
    }

    public static void Save(string folder)
    {
        WriteConfig(ConfigFile, RequireLibrary(folder));
    }

    static void WriteConfig(string configFile, string folder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configFile)!);
        string temp = configFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(new { dataFolder = folder }));
        File.Move(temp, configFile, true);
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
        string staging = Path.Combine(parent, ".rustaji-migrate-" + Guid.NewGuid().ToString("N"));
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
            Destination = Path.Combine(picker.SelectedPath, "RustAjiLibrary");
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
