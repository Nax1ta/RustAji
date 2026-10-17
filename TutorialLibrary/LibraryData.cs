using System.Drawing;
using System.IO.Compression;
using System.Text.Json;

namespace TutorialLibrary;

public sealed class LibraryData
{
    public int Version { get; set; } = 3;
    public List<LibraryFolder> Folders { get; set; } = [];
    public List<StudyItem> Items { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
}

public sealed class LibraryFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public string? ImageFile { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? LabelTextColor { get; set; }
    public string? LabelFillColor { get; set; }
    public int LabelFillOpacity { get; set; } = 100;
    public int LabelCornerRadius { get; set; } = 3;
    public string? IconColor { get; set; }
}

public sealed class StudyItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FolderId { get; set; }
    // Kept for migration from the first release and old backups.
    public List<string> Categories { get; set; } = [];
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string Notes { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public string? ImageFile { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class AppSettings
{
    public int EntryLevels { get; set; } = 3;
    public List<string> LevelLabels { get; set; } = ["科目", "类别", "章节", "主题", "细分"];
    public string FontFamily { get; set; } = "Microsoft YaHei UI";
    public float FontSize { get; set; } = 9f;
    public string ThemeName { get; set; } = "经典灰";
    public string WindowColor { get; set; } = "#ECECEC";
    public string PanelColor { get; set; } = "#FFFFFF";
    public string TextColor { get; set; } = "#202020";
    public string AccentColor { get; set; } = "#74836E";
    public string ButtonColor { get; set; } = "#E8E8E8";
    public string BorderColor { get; set; } = "#A0A0A0";
    public string? WallpaperFile { get; set; }
    public string? DecorationFile { get; set; }
    public int DecorationSize { get; set; } = 36;
    public bool AnimateDecoration { get; set; } = true;
}

public sealed class DataStore
{
    static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public string Folder { get; }
    public string ImageFolder => Path.Combine(Folder, "images");
    public string AssetFolder => Path.Combine(Folder, "assets");
    string DataFile => Path.Combine(Folder, "library.json");

    public DataStore(string? folder = null, string? legacyFolder = null)
    {
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Folder = folder ?? Path.Combine(localData, "RustAji");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(ImageFolder);
        Directory.CreateDirectory(AssetFolder);
        if (folder is null || legacyFolder is not null)
            CopyLegacyLibrary(legacyFolder ?? Path.Combine(localData, "腐味"));
    }

    void CopyLegacyLibrary(string legacyFolder)
    {
        if (File.Exists(DataFile) || !File.Exists(Path.Combine(legacyFolder, "library.json"))) return;
        foreach (var (source, target) in new[] { ("images", ImageFolder), ("assets", AssetFolder) })
        {
            string oldDirectory = Path.Combine(legacyFolder, source);
            if (!Directory.Exists(oldDirectory)) continue;
            foreach (var file in Directory.EnumerateFiles(oldDirectory, "*", SearchOption.TopDirectoryOnly))
            {
                string destination = Path.Combine(target, Path.GetFileName(file));
                if (!File.Exists(destination)) File.Copy(file, destination);
            }
        }
        string oldBackup = Path.Combine(legacyFolder, "library.json.bak");
        if (File.Exists(oldBackup)) File.Copy(oldBackup, DataFile + ".bak", true);
        File.Copy(Path.Combine(legacyFolder, "library.json"), DataFile);
    }

    public LibraryData Load()
    {
        if (!File.Exists(DataFile)) return new();
        try
        {
            var loaded = ReadData(DataFile);
            try { CleanupUnusedMedia(loaded); } catch { /* A cleanup failure must not prevent opening valid data. */ }
            return loaded;
        }
        catch
        {
            string backup = DataFile + ".bak";
            if (!File.Exists(backup)) throw;
            var restored = ReadData(backup);
            File.Copy(backup, DataFile, true);
            MessageBox.Show("主数据文件无法读取，已从上次保存的副本恢复。请立即导出备份。", "数据恢复", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            try { CleanupUnusedMedia(restored); } catch { }
            return restored;
        }
    }

    static LibraryData ReadData(string path)
    {
        var data = JsonSerializer.Deserialize<LibraryData>(File.ReadAllText(path), Json) ?? throw new InvalidDataException("数据文件为空。 ");
        if (data.Version == 2) MigrateFolders(data);
        Validate(data);
        return data;
    }

    public static void MigrateFolders(LibraryData data)
    {
        if (data.Version != 2) return;
        data.Folders ??= [];
        foreach (var item in data.Items)
        {
            Guid? parent = null;
            foreach (var part in item.Categories)
            {
                var folder = data.Folders.FirstOrDefault(x => x.ParentId == parent && x.Name == part);
                if (folder is null)
                {
                    folder = new LibraryFolder { ParentId = parent, Name = part };
                    data.Folders.Add(folder);
                }
                parent = folder.Id;
            }
            item.FolderId = parent;
        }
        data.Version = 3;
    }

    public void Save(LibraryData data)
    {
        Validate(data);
        string temp = DataFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Json));
        if (File.Exists(DataFile)) File.Replace(temp, DataFile, DataFile + ".bak", true);
        else File.Move(temp, DataFile);
        try { CleanupUnusedMedia(data); }
        catch { /* Data is already committed; media cleanup must not turn a successful save into a failed save. */ }
    }

    public (int Images, int Assets) CleanupUnusedMedia(LibraryData current)
    {
        var keptImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keptAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IncludeMedia(current, keptImages, keptAssets);
        string backup = DataFile + ".bak";
        if (File.Exists(backup))
        {
            try { IncludeMedia(ReadData(backup), keptImages, keptAssets); }
            catch { return (0, 0); } // Do not remove files while the recovery copy cannot be inspected.
        }
        return (Prune(ImageFolder, keptImages), Prune(AssetFolder, keptAssets));
    }

    static void IncludeMedia(LibraryData data, HashSet<string> images, HashSet<string> assets)
    {
        foreach (var folder in data.Folders) if (folder.ImageFile is { } name) images.Add(name);
        foreach (var item in data.Items) if (item.ImageFile is { } name) images.Add(name);
        if (data.Settings.WallpaperFile is { } wallpaper) assets.Add(wallpaper);
        if (data.Settings.DecorationFile is { } decoration) assets.Add(decoration);
    }

    static int Prune(string folder, HashSet<string> kept)
    {
        int deleted = 0;
        foreach (string path in Directory.EnumerateFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(path);
            if (kept.Contains(name) || name.Length != 36 || !Guid.TryParseExact(name[..32], "N", out _)) continue;
            try { File.Delete(path); deleted++; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return deleted;
    }

    static void Validate(LibraryData data)
    {
        if (data.Version != 3 || data.Items is null || data.Folders is null || data.Settings is null) throw new InvalidDataException("数据版本不受支持。 ");
        if (data.Items.Count > 100000) throw new InvalidDataException("资料数量超过上限。 ");
        if (data.Folders.Count > 100000) throw new InvalidDataException("文件夹数量超过上限。 ");
        var folders = data.Folders.ToDictionary(x => x.Id);
        foreach (var folder in data.Folders)
        {
            if (folder.Id == Guid.Empty || string.IsNullOrWhiteSpace(folder.Name) || folder.Name.Length > 100 || folder.Name.IndexOfAny(['/', '\\']) >= 0)
                throw new InvalidDataException("文件夹名称无效。 ");
            if (folder.ParentId is Guid parent && !folders.ContainsKey(parent)) throw new InvalidDataException("文件夹的上级不存在。 ");
            if (folder.ImageFile is not null && Path.GetFileName(folder.ImageFile) != folder.ImageFile) throw new InvalidDataException("文件夹图片路径无效。 ");
            if (folder.LabelFillOpacity is < 0 or > 100 || folder.LabelCornerRadius is < 0 or > 16 ||
                !FolderAppearance.IsValidColor(folder.LabelTextColor) || !FolderAppearance.IsValidColor(folder.LabelFillColor) || !FolderAppearance.IsValidColor(folder.IconColor))
                throw new InvalidDataException("文件夹外观设置无效。 ");
            var seen = new HashSet<Guid>();
            LibraryFolder? current = folder;
            while (current is not null)
            {
                if (!seen.Add(current.Id)) throw new InvalidDataException("文件夹不能放进自己的子文件夹。 ");
                current = current.ParentId is Guid id ? folders[id] : null;
            }
        }
        if (data.Folders.GroupBy(x => (x.ParentId, x.Name), new FolderNameComparer()).Any(x => x.Count() > 1))
            throw new InvalidDataException("同一位置不能有重名文件夹。 ");
        foreach (var item in data.Items)
        {
            if (item.Id == Guid.Empty || string.IsNullOrWhiteSpace(item.Title) || item.FolderId is Guid folderId && !folders.ContainsKey(folderId))
                throw new InvalidDataException("资料内容不完整。 ");
            if (!string.IsNullOrWhiteSpace(item.Url) && !IsWebUrl(item.Url)) throw new InvalidDataException("资料包含无效网址。 ");
            if (item.ImageFile is not null && Path.GetFileName(item.ImageFile) != item.ImageFile) throw new InvalidDataException("图片路径无效。 ");
        }
    }

    sealed class FolderNameComparer : IEqualityComparer<(Guid? ParentId, string Name)>
    {
        public bool Equals((Guid? ParentId, string Name) a, (Guid? ParentId, string Name) b) => a.ParentId == b.ParentId && string.Equals(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
        public int GetHashCode((Guid? ParentId, string Name) value) => HashCode.Combine(value.ParentId, StringComparer.CurrentCultureIgnoreCase.GetHashCode(value.Name));
    }

    public static bool IsWebUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public string? ImagePath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)) return null;
        string path = Path.Combine(ImageFolder, fileName);
        return File.Exists(path) ? path : null;
    }

    public string? AssetPath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName)) return null;
        string path = Path.Combine(AssetFolder, fileName);
        return File.Exists(path) ? path : null;
    }

    public string SaveImage(byte[] data)
    {
        if (data.Length > 20 * 1024 * 1024) throw new InvalidDataException("图片超过 20 MB，请先压缩。 ");
        using var input = new MemoryStream(data);
        using var image = Image.FromStream(input, true, true);
        if ((long)image.Width * image.Height > 40_000_000) throw new InvalidDataException("图片尺寸过大。 ");
        string name = Guid.NewGuid().ToString("N") + ".png";
        string temp = Path.Combine(ImageFolder, name + ".tmp");
        string target = Path.Combine(ImageFolder, name);
        using (var bitmap = new Bitmap(image)) bitmap.Save(temp, System.Drawing.Imaging.ImageFormat.Png);
        File.Move(temp, target);
        return name;
    }

    public string SaveImage(string sourcePath) => SaveImage(File.ReadAllBytes(sourcePath));

    public string SaveAsset(string sourcePath)
    {
        var name = Guid.NewGuid().ToString("N") + ".png";
        string target = Path.Combine(AssetFolder, name);
        using var image = Image.FromFile(sourcePath);
        if ((long)image.Width * image.Height > 40_000_000) throw new InvalidDataException("图片尺寸过大。 ");
        using var bitmap = new Bitmap(image);
        bitmap.Save(target, System.Drawing.Imaging.ImageFormat.Png);
        return name;
    }

    public void ExportBackup(LibraryData data, string destination)
    {
        Save(data);
        string temp = destination + ".tmp";
        if (File.Exists(temp)) File.Delete(temp);
        try
        {
            using (var zip = ZipFile.Open(temp, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(DataFile, "library.json", CompressionLevel.Optimal);
                var images = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                IncludeMedia(data, images, assets);
                foreach (string name in images) zip.CreateEntryFromFile(Path.Combine(ImageFolder, name), "images/" + name, CompressionLevel.Optimal);
                foreach (string name in assets) zip.CreateEntryFromFile(Path.Combine(AssetFolder, name), "assets/" + name, CompressionLevel.Optimal);
            }
            File.Move(temp, destination, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public LibraryData ImportBackup(string source)
    {
        if (string.Equals(Path.GetExtension(source), ".json", StringComparison.OrdinalIgnoreCase)) return ImportBrowserBackup(source);
        string staging = Path.Combine(Folder, "import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            long total = 0;
            using (var zip = ZipFile.OpenRead(source))
            {
                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith('/')) continue;
                    string normalized = entry.FullName.Replace('\\', '/');
                    bool allowed = normalized == "library.json" || normalized.StartsWith("images/", StringComparison.Ordinal) || normalized.StartsWith("assets/", StringComparison.Ordinal);
                    if (!allowed || normalized.Contains("..") || normalized.Split('/').Length > 2 || entry.Length > 25 * 1024 * 1024)
                        throw new InvalidDataException("备份包含无效文件。 ");
                    total += entry.Length;
                    if (total > 500L * 1024 * 1024) throw new InvalidDataException("备份过大。 ");
                    string output = Path.Combine(staging, normalized.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    entry.ExtractToFile(output, true);
                }
            }
            string dataPath = Path.Combine(staging, "library.json");
            if (!File.Exists(dataPath)) throw new InvalidDataException("备份没有数据文件。 ");
            LibraryData imported = ReadData(dataPath);
            string importedImages = Path.Combine(staging, "images");
            string importedAssets = Path.Combine(staging, "assets");
            var requiredImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requiredAssets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IncludeMedia(imported, requiredImages, requiredAssets);
            if (requiredImages.Any(name => !File.Exists(Path.Combine(importedImages, name))) ||
                requiredAssets.Any(name => !File.Exists(Path.Combine(importedAssets, name))))
                throw new InvalidDataException("备份缺少资料或设置引用的图片。 ");
            if (Directory.Exists(importedImages))
                foreach (var file in Directory.EnumerateFiles(importedImages, "*", SearchOption.TopDirectoryOnly)) File.Copy(file, Path.Combine(ImageFolder, Path.GetFileName(file)), true);
            if (Directory.Exists(importedAssets))
                foreach (var file in Directory.EnumerateFiles(importedAssets, "*", SearchOption.TopDirectoryOnly)) File.Copy(file, Path.Combine(AssetFolder, Path.GetFileName(file)), true);
            Save(imported);
            return imported;
        }
        finally { Directory.Delete(staging, true); }
    }

    LibraryData ImportBrowserBackup(string source)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(source));
        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) || format.GetString() != "tutorial-library" || !root.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("这不是旧版网页工具导出的 JSON 备份。 ");
        var result = new LibraryData();
        foreach (var record in records.EnumerateArray())
        {
            string subject = GetString(record, "subject"), topic = GetString(record, "topic"), title = GetString(record, "title");
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(title)) throw new InvalidDataException("旧版备份中有不完整的资料。 ");
            var item = new StudyItem { Categories = [subject, topic, "未分类"], Title = title, Url = GetString(record, "url"), Notes = GetString(record, "highlights") };
            string dataUrl = GetString(record, "image");
            int comma = dataUrl.IndexOf(',');
            if (dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && comma > 0)
                item.ImageFile = SaveImage(Convert.FromBase64String(dataUrl[(comma + 1)..]));
            result.Items.Add(item);
        }
        result.Version = 2;
        MigrateFolders(result);
        Save(result);
        return result;
    }

    static string GetString(JsonElement obj, string name) => obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
}
