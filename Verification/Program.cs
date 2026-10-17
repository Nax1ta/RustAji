using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.IO.Compression;
using TutorialLibrary;

Exception? verificationError = null;
var verificationThread = new Thread(() =>
{
    try { Run(args); }
    catch (Exception error) { verificationError = error; }
});
verificationThread.SetApartmentState(ApartmentState.STA);
verificationThread.Start();
verificationThread.Join();
if (verificationError is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(verificationError).Throw();

static void Run(string[] args)
{
Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
string root = Path.Combine(Environment.CurrentDirectory, "verification-data", Guid.NewGuid().ToString("N"));
var welcomeStore = new DataStore(Path.Combine(root, "welcome"));
Check(WelcomeLibrary.Ensure(welcomeStore), "first launch creates example library");
var welcome = welcomeStore.Load();
Check(welcome.Folders.Count == 4 && welcome.Items.Count == 6 && welcome.Items.All(x => welcomeStore.ImagePath(x.ImageFile) is not null), "offline illustrated lessons and folders");
Check(!WelcomeLibrary.Ensure(welcomeStore) && welcomeStore.Load().Items.Count == 6, "existing library is never reseeded");
var first = new DataStore(Path.Combine(root, "first"));
var data = new LibraryData();
var mathFolder = new LibraryFolder { Name = "数学" };
var calculusFolder = new LibraryFolder { Name = "高等数学", ParentId = mathFolder.Id };
data.Folders.AddRange([mathFolder, calculusFolder]);
data.Items.Add(new StudyItem { FolderId = calculusFolder.Id, Title = "极限定义", Url = "https://example.com/course", Notes = "先写定义。" });
data.Settings.EntryLevels = 4;
data.Settings.FontSize = 11;
first.Save(data);
Check(first.Load().Items.Count == 1, "save/load count");
Check(first.Load().Folders.Count == 2 && first.Load().Items[0].FolderId == calculusFolder.Id, "folder persistence");
Check(first.Load().Settings.EntryLevels == 4, "settings persistence");
string image = first.SaveImage(Path.Combine(Environment.CurrentDirectory, "Assets", "avatar.png"));
data.Items[0].ImageFile = image;
first.Save(data);
var renamedStore = new DataStore(Path.Combine(root, "renamed"), first.Folder);
Check(renamedStore.Load().Items.Count == 1 && renamedStore.Load().Folders.Count == 2, "renamed app data copy");
Check(renamedStore.ImagePath(image) is not null && first.ImagePath(image) is not null, "renamed app retains images and old data");
string backup = Path.Combine(root, "library.tlb");
first.ExportBackup(data, backup);
var second = new DataStore(Path.Combine(root, "second"));
var restored = second.ImportBackup(backup);
Check(restored.Items.Count == 1 && restored.Items[0].Title == "极限定义", "backup restore record");
Check(restored.Folders.Count == 2 && restored.Items[0].FolderId == calculusFolder.Id, "backup restore folders");
Check(second.ImagePath(restored.Items[0].ImageFile) is not null, "backup restore image");
Check(restored.Settings.FontSize == 11, "backup restore settings");
Check(restored.Folders.All(x => x.Description == ""), "older backup loads without folder descriptions");
Check(restored.Folders.All(x => x.Tags.Count == 0) && restored.Items.All(x => x.Tags.Count == 0), "older backup loads without tags");
mathFolder.Description = "考研数学资料";
calculusFolder.Description = "极限与导数课程";
mathFolder.Tags = ["考研", "数学"];
mathFolder.LabelTextColor = "#335577";
mathFolder.LabelFillColor = "#EEEEDD";
mathFolder.LabelFillOpacity = 65;
mathFolder.LabelCornerRadius = 7;
mathFolder.IconColor = "#889977";
calculusFolder.Tags = ["极限"];
data.Items[0].Tags = ["真题", "极限"];
first.Save(data);
Check(first.Load().Folders.Single(x => x.Id == mathFolder.Id).Description == "考研数学资料", "folder description persistence");
Check(first.Load().Items[0].Tags.SequenceEqual(["真题", "极限"]), "tag persistence");
Check(first.Load().Folders.Single(x => x.Id == mathFolder.Id).IconColor == "#889977", "folder appearance persistence");
Check(ContentView.Build(data, null, "#真题", true, true, [], ContentSort.NameAscending).Single().Item?.Title == "极限定义", "search by tag across folders");
Check(ContentView.Build(data, null, "", true, false, [], ContentSort.NameAscending).Single().Name == "数学", "type filter shows folders only");
Check(ContentView.Build(data, null, "", true, true, ["数学"], ContentSort.NameAscending).Single().Name == "数学", "tag filter");
Check(ContentView.Build(data, null, "", true, true, ["真题"], ContentSort.NameAscending).Single().Folder?.Id == mathFolder.Id, "tag filter keeps ancestor folder at root");
Check(ContentView.Build(data, mathFolder.Id, "", true, true, ["真题"], ContentSort.NameAscending).Single().Folder?.Id == calculusFolder.Id, "tag filter keeps intermediate folder");
Check(ContentView.Build(data, calculusFolder.Id, "", true, true, ["真题"], ContentSort.NameAscending).Single().Item?.Title == "极限定义", "tag filter reaches tagged material");
Check(ContentView.FitColumnWidths([180, 68, 84, 110], 480, -1).Sum() == 480, "content columns fill available width");
Check(ContentView.FitColumnWidths([300, 68, 84, 110], 480, 0).Sum() == 480, "wide name column leaves no horizontal overflow");
Check(ContentView.FitColumnWidths([180, 68, 84, 110], 310, -1).Sum() == 310, "narrow content columns avoid horizontal overflow");
var dated = new LibraryData();
var parent = new LibraryFolder { Name = "父文件夹", UpdatedAt = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero) };
var child = new LibraryFolder { Name = "子文件夹", ParentId = parent.Id, UpdatedAt = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero) };
var sibling = new LibraryFolder { Name = "另一个文件夹", UpdatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero) };
var newest = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
dated.Folders.AddRange([parent, child, sibling]);
dated.Items.Add(new StudyItem { FolderId = child.Id, Title = "最新资料", UpdatedAt = newest });
var folderDates = ContentView.LatestFolderUpdates(dated);
Check(folderDates[parent.Id] == newest && folderDates[child.Id] == newest, "folder update includes nested material");
var datedRows = ContentView.Build(dated, null, "", true, true, [], ContentSort.UpdatedNewest);
Check(datedRows[0].Folder?.Id == parent.Id && datedRows[0].UpdatedAt == newest, "folder update controls display and sort");
dated.Items.Clear();
Check(ContentView.LatestFolderUpdates(dated)[parent.Id] == child.UpdatedAt, "folder update falls back to newest child folder");
var bounce = typeof(MainForm).GetMethod("DecorationScale", BindingFlags.Static | BindingFlags.NonPublic)!;
var squashed = ((float X, float Y))bounce.Invoke(null, [85d])!;
var resting = ((float X, float Y))bounce.Invoke(null, [400d])!;
Check(squashed.Y >= .88f && squashed.Y < .95f && squashed.X <= 1.04f && resting == (1f, 1f), "decoration bounce stays subtle and returns to rest");

string movedFolder = DataLocation.Migrate(first, data, Path.Combine(root, "moved-library"));
var movedStore = new DataStore(movedFolder);
Check(movedStore.Load().Items.Count == data.Items.Count && movedStore.ImagePath(image) is not null, "data directory migration copies records and images");
Check(first.Load().Items.Count == data.Items.Count && first.ImagePath(image) is not null, "data directory migration preserves original library");
var mediaStore = new DataStore(Path.Combine(root, "media-cleanup"));
var mediaData = new LibraryData();
string usedCover = mediaStore.SaveImage(Path.Combine(Environment.CurrentDirectory, "Assets", "avatar.png"));
mediaData.Items.Add(new StudyItem { Title = "共享图片 A", ImageFile = usedCover });
mediaData.Items.Add(new StudyItem { Title = "共享图片 B", ImageFile = usedCover });
mediaStore.Save(mediaData);
mediaData.Items.RemoveAt(0);
mediaStore.Save(mediaData);
Check(mediaStore.ImagePath(usedCover) is not null, "shared cover survives deleting one item");
mediaData.Items.Clear();
mediaStore.Save(mediaData);
Check(mediaStore.ImagePath(usedCover) is not null, "cover needed by recovery copy is retained");
mediaStore.Save(mediaData);
Check(mediaStore.ImagePath(usedCover) is null, "unreferenced cover is removed after recovery copy advances");
var cloneSource = new DataStore(Path.Combine(root, "clone-source"));
cloneSource.Save(new LibraryData());
string abandoned = cloneSource.SaveImage(Path.Combine(Environment.CurrentDirectory, "Assets", "avatar.png"));
var cleanClone = new DataStore(Path.Combine(root, "clean-clone"), cloneSource.Folder);
Check(cleanClone.ImagePath(abandoned) is not null && cleanClone.Load().Items.Count == 0 && cleanClone.ImagePath(abandoned) is null, "first load removes unreferenced copied image");
string currentCover = mediaStore.SaveImage(Path.Combine(Environment.CurrentDirectory, "Assets", "avatar.png"));
mediaData.Items.Add(new StudyItem { Title = "导出测试", ImageFile = currentCover });
mediaStore.Save(mediaData);
string exported = Path.Combine(root, "minimal-backup.tlb");
mediaStore.ExportBackup(mediaData, exported);
using (var compactBackup = ZipFile.OpenRead(exported))
    Check(compactBackup.Entries.Count(x => x.FullName.StartsWith("images/", StringComparison.Ordinal)) == 1, "backup includes only referenced images");

var copyItem = LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Item, data.Items[0].Id, false), calculusFolder.Id);
Check(copyItem.Data.Items.Count == 2 && copyItem.ResultId != data.Items[0].Id && copyItem.Data.Items.Single(x => x.Id == copyItem.ResultId).Title == "极限定义 - 副本", "copy item and resolve name conflict");
Check(copyItem.Data.Items.Single(x => x.Id == copyItem.ResultId).ImageFile == image && data.Items.Count == 1, "copy retains image without changing source");
Check(copyItem.Data.Items.Single(x => x.Id == copyItem.ResultId).Tags.Contains("真题"), "item copy retains tags");
var moveItem = LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Item, data.Items[0].Id, true), null);
Check(moveItem.Data.Items.Count == 1 && moveItem.Data.Items[0].Id == data.Items[0].Id && moveItem.Data.Items[0].FolderId is null, "cut item moves with original identity");
var copyFolder = LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Folder, mathFolder.Id, false), null);
var copiedChild = copyFolder.Data.Folders.Single(x => x.ParentId == copyFolder.ResultId);
Check(copyFolder.Data.Folders.Count == 4 && copyFolder.Data.Items.Count == 2 && copiedChild.Id != calculusFolder.Id, "copy folder includes nested folders and items");
Check(copyFolder.Data.Folders.Single(x => x.Id == copyFolder.ResultId).Name == "数学 - 副本" && copyFolder.Data.Items.Single(x => x.FolderId == copiedChild.Id).ImageFile == image, "folder copy name and cover references");
Check(copyFolder.Data.Folders.Single(x => x.Id == copyFolder.ResultId).Description == "考研数学资料" && copiedChild.Description == "极限与导数课程", "folder copy includes descriptions");
Check(copyFolder.Data.Folders.Single(x => x.Id == copyFolder.ResultId).LabelFillOpacity == 65 && copyFolder.Data.Folders.Single(x => x.Id == copyFolder.ResultId).IconColor == "#889977", "folder copy includes appearance");
Check(copyFolder.Data.Folders.Single(x => x.Id == copyFolder.ResultId).Tags.Contains("数学") && copyFolder.Data.Items.Single(x => x.FolderId == copiedChild.Id).Tags.Contains("真题"), "folder copy includes tags");
first.Save(copyFolder.Data);
Check(first.Load().Folders.Count == 4 && first.Load().Items.Count == 2, "copied structure persists");
bool rejectedCycle = false;
try { LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Folder, mathFolder.Id, true), calculusFolder.Id); }
catch (InvalidOperationException) { rejectedCycle = true; }
Check(rejectedCycle && data.Folders[0].ParentId is null, "reject moving folder into descendant");
Check(!LibraryOrganizer.CanMove(data, new LibraryTransfer(LibraryEntryType.Folder, mathFolder.Id, true), calculusFolder.Id), "drag refuses folder cycle");
Check(LibraryOrganizer.CanMove(data, new LibraryTransfer(LibraryEntryType.Item, data.Items[0].Id, true), null), "drag permits item to root");
Check(!LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Folder, mathFolder.Id, true), null).Changed, "cut and paste in same location is a no-op");
var archive = new LibraryFolder { Name = "归档" };
var occupied = new LibraryFolder { Name = "高等数学", ParentId = archive.Id };
data.Folders.AddRange([archive, occupied]);
var moveFolder = LibraryOrganizer.Paste(data, new LibraryTransfer(LibraryEntryType.Folder, calculusFolder.Id, true), archive.Id);
Check(moveFolder.Data.Folders.Single(x => x.Id == calculusFolder.Id).ParentId == archive.Id && moveFolder.Data.Folders.Single(x => x.Id == calculusFolder.Id).Name == "高等数学 - 2", "move folder resolves target name conflict");
Check(moveFolder.Data.Items[0].FolderId == calculusFolder.Id && data.Folders.Single(x => x.Id == calculusFolder.Id).ParentId == mathFolder.Id, "move preserves descendants without changing source");
Check(moveFolder.Data.Folders.Single(x => x.Id == calculusFolder.Id).Description == "极限与导数课程", "folder move retains description");
data.Folders.Remove(archive);
data.Folders.Remove(occupied);
first.Save(data);

string browserJson = Path.Combine(root, "browser.json");
File.WriteAllText(browserJson, JsonSerializer.Serialize(new { format = "tutorial-library", version = 1, records = new[] { new { subject = "英语", topic = "阅读", title = "长难句", url = "https://example.com", highlights = "找主干" } } }));
var migrated = second.ImportBackup(browserJson);
Check(migrated.Items.Count == 1 && migrated.Folders.Count == 3 && migrated.Items[0].FolderId is not null, "browser backup migration");

string legacyRoot = Path.Combine(root, "legacy");
Directory.CreateDirectory(legacyRoot);
File.WriteAllText(Path.Combine(legacyRoot, "library.json"), JsonSerializer.Serialize(new LibraryData { Version = 2, Items = [new StudyItem { Categories = ["计算机", "数据结构", "树"], Title = "二叉树" }] }));
var legacy = new DataStore(legacyRoot).Load();
Check(legacy.Version == 3 && legacy.Folders.Count == 3 && legacy.Items[0].FolderId == legacy.Folders[^1].Id, "old desktop data migration");

Uri? og = ThumbnailService.FindImageUrl(new Uri("https://example.com/article"), "<html><meta content='/cover.png' property='og:image'></html>");
Check(og?.ToString() == "https://example.com/cover.png", "Open Graph cover parser");
Uri? twitter = ThumbnailService.FindImageUrl(new Uri("https://example.com/article"), "<meta name=twitter:image content=https://images.example.com/a.jpg>");
Check(twitter?.ToString() == "https://images.example.com/a.jpg", "Twitter cover parser");
Check(ThumbnailService.GetYouTubeThumbnailUrl(new Uri("https://www.youtube.com/watch?v=l_jfyIEddfw"))?.ToString() == "https://i.ytimg.com/vi/l_jfyIEddfw/hqdefault.jpg", "YouTube watch thumbnail URL");
Check(ThumbnailService.GetYouTubeThumbnailUrl(new Uri("https://youtu.be/l_jfyIEddfw?t=42")) is not null && ThumbnailService.GetYouTubeThumbnailUrl(new Uri("https://www.youtube.com/shorts/l_jfyIEddfw")) is not null, "YouTube short and shorts URLs");
Check(ThumbnailService.GetYouTubeThumbnailUrl(new Uri("https://www.youtube.com.evil.example/watch?v=l_jfyIEddfw")) is null, "reject imitation YouTube host");
CheckCoverDownloadAsync(Path.Combine(Environment.CurrentDirectory, "Assets", "avatar.png")).GetAwaiter().GetResult();
if (args.Contains("--youtube"))
{
    byte[]? youtubeCover = ThumbnailService.FindCoverAsync("https://www.youtube.com/watch?v=l_jfyIEddfw").GetAwaiter().GetResult();
    using var imageBytes = new MemoryStream(youtubeCover ?? []);
    using var youtubePicture = System.Drawing.Image.FromStream(imageBytes);
    Check(youtubePicture.Width >= 320 && youtubePicture.Height >= 180, "actual YouTube video thumbnail download");
}
using (var folderWindow = new MainForm(first))
{
    var tree = Descendants(folderWindow).OfType<TreeView>().First();
    var list = Descendants(folderWindow).OfType<ListView>().First();
    Check(tree.Nodes.Count == 1 && tree.Nodes[0].Text == "数学", "top-level folders without total folder");
    Check(tree.AllowDrop && list.AllowDrop, "folder tree and content list accept drag moves");
    var menuBar = (MenuStrip)typeof(MainForm).GetField("menu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(folderWindow)!;
    Check(!Descendants(folderWindow).OfType<Button>().Any(button => button.Text == "新建资料"), "new material removed from toolbar");
    Check(new[] { "upButton", "addFolderButton", "sortButton", "editButton", "deleteButton", "imageButton" }
        .Select(name => (Button)typeof(MainForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(folderWindow)!)
        .All(button => button.GetType().Name == "ChromeButton" && button.Region is null), "secondary buttons paint complete rounded corners");
    Check(menuBar.Items[0].Text == "查看所有资料" && menuBar.Items[0] is ToolStripMenuItem { DropDownItems.Count: 0 }, "flat library opens directly");
    Check(menuBar.Items[1] is ToolStripMenuItem backupMenu && backupMenu.DropDownItems.Count == 2, "backup menu contains only import and export");
    var folderContext = (ContextMenuStrip)typeof(MainForm).GetField("folderMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(folderWindow)!;
    var itemContext = (ContextMenuStrip)typeof(MainForm).GetField("itemMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(folderWindow)!;
    var locationContext = (ContextMenuStrip)typeof(MainForm).GetField("locationMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(folderWindow)!;
    Check(folderContext.Items[1].Text == "新建资料" && locationContext.Items[1].Text == "新建资料", "folder and blank-area menus offer new material");
    Check(folderContext.Font.Equals(folderWindow.Font) && itemContext.Renderer.GetType().Name == "HeaderMenuRenderer", "context menus share window typography and rendering");
    Check(folderContext.Items.Cast<ToolStripItem>().Any(x => x.Text == "外观与图标颜色") && folderContext.Items.Cast<ToolStripItem>().Any(x => x.Text == "属性") && itemContext.Items.Cast<ToolStripItem>().Any(x => x.Text == "属性"), "appearance and properties context actions");
    typeof(MainForm).GetField("selectedFolderId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(folderWindow, mathFolder.Id);
    typeof(MainForm).GetMethod("ShowSelected", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(folderWindow, null);
    Check(Descendants(folderWindow).OfType<TextBox>().Any(x => x.ReadOnly && x.Multiline && x.Text == "考研数学资料"), "folder description shown in details");
}
using (var folderEditor = new FolderEditor(data.Settings, mathFolder))
    Check(folderEditor.Text == "编辑文件夹" && Descendants(folderEditor).OfType<TextBox>().Any(x => x.Multiline && x.Text == "考研数学资料"), "folder description editor construction");
using (var tagEditor = new TagEditor(data.Settings, ["真题", "重点"]))
    Check(tagEditor.Result.SequenceEqual(["真题", "重点"]) && Descendants(tagEditor).OfType<Button>().Any(x => x.Text.Contains("真题")), "tag editor construction");
using (var appearanceEditor = new FolderAppearanceEditor(data.Settings, mathFolder))
{
    Check(appearanceEditor.TextColor is not null && appearanceEditor.IconColor is not null && appearanceEditor.FillOpacity == 65 && appearanceEditor.CornerRadius == 7, "folder appearance editor construction");
    var colorTargets = Descendants(appearanceEditor).OfType<ListBox>().Single();
    Check(colorTargets.Items.Count == 3 && Descendants(appearanceEditor).OfType<TrackBar>().Count() == 2, "color plane and transparency controls are available together");
    appearanceEditor.StartPosition = FormStartPosition.Manual;
    appearanceEditor.Location = new System.Drawing.Point(-3000, -3000);
    appearanceEditor.ShowInTaskbar = false;
    appearanceEditor.Show();
    Application.DoEvents();
    colorTargets.SelectedIndex = 1;
    var colorPlane = Descendants(appearanceEditor).Single(x => x.GetType().Name == "SaturationValuePlane");
    string? originalFill = appearanceEditor.FillColor;
    colorPlane.GetType().GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(colorPlane, [new MouseEventArgs(MouseButtons.Left, 1, colorPlane.Width / 4, colorPlane.Height / 4, 0)]);
    Check(appearanceEditor.FillColor is not null && appearanceEditor.FillColor != originalFill, "dragging on color plane changes selected fill");
    appearanceEditor.Close();
}
using (var folderProperties = new EntryPropertiesForm(data, mathFolder, first))
    Check(folderProperties.Text.Contains("属性") && Descendants(folderProperties).OfType<TabControl>().Any(), "folder properties construction");
using (var itemProperties = new EntryPropertiesForm(data, data.Items[0], first))
    Check(itemProperties.Text.Contains("属性") && Descendants(itemProperties).OfType<TabControl>().Any(), "item properties construction");
using (var itemProperties = new EntryPropertiesForm(data, data.Items[0], first))
    Check(Descendants(itemProperties).OfType<ListView>().Single().ContextMenuStrip?.Items.Count == 2, "property rows support copying");
using (var allItems = new AllItemsForm(data))
{
    var rows = Descendants(allItems).OfType<ListView>().Single().Items;
    Check(rows.Count == data.Items.Count && rows[0].SubItems[1].Text.Contains("数学") && rows[0].SubItems[2].Text.Contains("example.com"), "flat library shows name path and link");
}
if (!args.Contains("--ui") && !args.Contains("--render-only")) { Console.WriteLine("数据和封面验证通过。"); return; }
if (args.Contains("--render-only"))
{
    using var window = new MainForm(first);
    window.StartPosition = FormStartPosition.Manual;
    window.Location = new System.Drawing.Point(-3000, -3000);
    window.ShowInTaskbar = false;
    window.Show();
    Application.DoEvents();
    var tree = Descendants(window).OfType<TreeView>().First();
    Console.WriteLine($"文件夹树：节点={tree.Nodes.Count} 首节点={tree.Nodes[0].Text} 区域={tree.Bounds}");
    using var imageOutput = new System.Drawing.Bitmap(window.Width, window.Height);
    window.DrawToBitmap(imageOutput, new System.Drawing.Rectangle(0, 0, imageOutput.Width, imageOutput.Height));
    imageOutput.Save(Path.Combine(root, "window-preview.png"));
    window.Close();
    using (var appearancePreview = new FolderAppearanceEditor(data.Settings, mathFolder))
    {
        appearancePreview.StartPosition = FormStartPosition.Manual;
        appearancePreview.Location = new System.Drawing.Point(-3000, -3000);
        appearancePreview.ShowInTaskbar = false;
        appearancePreview.Show();
        Application.DoEvents();
        using var appearanceImage = new System.Drawing.Bitmap(appearancePreview.Width, appearancePreview.Height);
        appearancePreview.DrawToBitmap(appearanceImage, new System.Drawing.Rectangle(0, 0, appearanceImage.Width, appearanceImage.Height));
        appearanceImage.Save(Path.Combine(root, "folder-appearance-preview.png"));
        appearancePreview.Close();
    }
    using (var locationPreview = new DataLocationForm(first.Folder))
    {
        locationPreview.StartPosition = FormStartPosition.Manual;
        locationPreview.Location = new System.Drawing.Point(-3000, -3000);
        locationPreview.ShowInTaskbar = false;
        locationPreview.Show();
        Application.DoEvents();
        using var locationImage = new System.Drawing.Bitmap(locationPreview.Width, locationPreview.Height);
        locationPreview.DrawToBitmap(locationImage, new System.Drawing.Rectangle(0, 0, locationImage.Width, locationImage.Height));
        locationImage.Save(Path.Combine(root, "location-preview.png"));
        locationPreview.Close();
    }
    Console.WriteLine("界面预览：" + Path.Combine(root, "window-preview.png"));
    return;
}

using (var main = new MainForm(first))
{
    Check(main.Text == "腐味 · RustAji", "main window construction");
    main.StartPosition = FormStartPosition.Manual;
    main.Location = new System.Drawing.Point(80, 80);
    main.ShowInTaskbar = false;
    Console.WriteLine("窗口显示前");
    main.Show();
    Console.WriteLine("窗口显示后");
    Application.DoEvents();
    Console.WriteLine("消息处理后");
    foreach (Control control in main.Controls) Console.WriteLine($"布局：{control.GetType().Name} {control.Bounds} 可见={control.Visible}");
    using var bitmap = new System.Drawing.Bitmap(main.Width, main.Height);
    main.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
    string preview = Path.Combine(root, "window-preview.png");
    bitmap.Save(preview, System.Drawing.Imaging.ImageFormat.Png);
    Check(new FileInfo(preview).Length > 10000, "window render");
    main.Close();
}
using (var welcomeWindow = new MainForm(welcomeStore))
{
    welcomeWindow.StartPosition = FormStartPosition.Manual;
    welcomeWindow.Location = new System.Drawing.Point(-3000, -3000);
    welcomeWindow.ShowInTaskbar = false;
    welcomeWindow.Show();
    Application.DoEvents();
    var introId = welcome.Folders.Single(x => x.Name == "从这里开始").Id;
    var welcomeTree = Descendants(welcomeWindow).OfType<TreeView>().First();
    welcomeTree.SelectedNode = welcomeTree.Nodes.Cast<TreeNode>().Single(node => node.Tag is Guid id && id == introId);
    var welcomeList = Descendants(welcomeWindow).OfType<ListView>().First();
    if (welcomeList.Items.Count > 0)
    {
        welcomeList.Items[0].Selected = true;
        typeof(MainForm).GetMethod("ShowSelected", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(welcomeWindow, null);
    }
    SaveWindow(welcomeWindow, Path.Combine(root, "welcome-window-preview.png"));
}
using (var empty = new MainForm(new DataStore(Path.Combine(root, "empty"))))
{
    empty.Icon = new System.Drawing.Icon(Path.Combine(Environment.CurrentDirectory, "TutorialLibrary", "Assets", "avatar.ico"));
    SaveWindow(empty, Path.Combine(root, "empty-window-preview.png"));
}
using (var allItems = new AllItemsForm(data))
    SaveWindow(allItems, Path.Combine(root, "all-items-preview.png"));
using (var itemEditor = new ItemEditor(data, null))
{
    Check(itemEditor.Text == "添加新资料", "item editor construction");
    SaveWindow(itemEditor, Path.Combine(root, "item-editor-preview.png"));
}
using (var settings = new SettingsForm(data.Settings, first))
{
    Check(settings.Text == "设置", "settings window construction");
    SaveWindow(settings, Path.Combine(root, "settings-preview.png"));
}
using (var appearance = new SettingsForm(data.Settings, first))
{
    appearance.Controls.OfType<TabControl>().First().SelectedIndex = 0;
    SaveWindow(appearance, Path.Combine(root, "appearance-preview.png"));
}
using (var pictures = new SettingsForm(data.Settings, first))
{
    pictures.Controls.OfType<TabControl>().First().SelectedIndex = 1;
    SaveWindow(pictures, Path.Combine(root, "pictures-preview.png"));
}
using (var theme = new SettingsForm(data.Settings, first))
{
    var tabs = theme.Controls.OfType<TabControl>().First();
    tabs.SelectedIndex = 0;
    var preset = Descendants(tabs).OfType<ComboBox>().First(box => box.Items.Contains("夜间"));
    preset.SelectedItem = "夜间";
    theme.ShowInTaskbar = false;
    theme.Show();
    Application.DoEvents();
    Descendants(theme).OfType<Button>().First(button => button.Text == "保存设置").PerformClick();
    Check(theme.Result.ThemeName == "夜间" && theme.Result.WindowColor == "#292B2D", "theme preset selection");
}
Console.WriteLine("全部验证通过。数据目录：" + root);

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception("验证失败：" + name);
    Console.WriteLine("通过：" + name);
}

static void SaveWindow(Form window, string output)
{
    window.StartPosition = FormStartPosition.Manual;
    window.Location = new System.Drawing.Point(80, 80);
    window.ShowInTaskbar = false;
    window.Show();
    Application.DoEvents();
    using var bitmap = new System.Drawing.Bitmap(window.Width, window.Height);
    window.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height));
    bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);
    window.Close();
    Check(new FileInfo(output).Length > 5000, Path.GetFileName(output));
}

static async Task CheckCoverDownloadAsync(string imagePath)
{
    using var server = new TcpListener(IPAddress.Loopback, 0);
    server.Start();
    int port = ((IPEndPoint)server.LocalEndpoint).Port;
    byte[] cover = File.ReadAllBytes(imagePath);
    var serve = Task.Run(async () =>
    {
        for (int i = 0; i < 2; i++)
        {
            using var client = await server.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            string request = await reader.ReadLineAsync() ?? "";
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }
            bool isCover = request.Contains("/cover.png", StringComparison.Ordinal);
            byte[] body = isCover ? cover : Encoding.UTF8.GetBytes("<meta property='og:image' content='/cover.png'>");
            string headers = $"HTTP/1.1 200 OK\r\nContent-Type: {(isCover ? "image/png" : "text/html; charset=utf-8")}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(headers);
            await stream.WriteAsync(headerBytes);
            await stream.WriteAsync(body);
        }
    });
    byte[]? fetched = await ThumbnailService.FindCoverAsync($"http://127.0.0.1:{port}/page");
    await serve.WaitAsync(TimeSpan.FromSeconds(10));
    Check(fetched is not null && fetched.SequenceEqual(cover), "automatic cover download");
}

static IEnumerable<Control> Descendants(Control root)
{
    foreach (Control child in root.Controls)
    {
        yield return child;
        foreach (var nested in Descendants(child)) yield return nested;
    }
}
}
