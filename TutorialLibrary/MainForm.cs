using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace TutorialLibrary;

public sealed class MainForm : Form
{
    DataStore store;
    LibraryData data;
    readonly TableLayoutPanel frameLayout = new();
    readonly MenuStrip menu = new();
    readonly Panel header = new();
    readonly TableLayoutPanel headerRow = new();
    readonly Panel searchHost = new();
    readonly Panel outer = new();
    readonly TableLayoutPanel mainLayout = new();
    readonly Panel categoryPanel = new();
    readonly Panel categoryBar = new();
    readonly FlowLayoutPanel folderActions = new();
    readonly Panel itemPanel = new();
    readonly Panel itemBar = new();
    readonly FlowLayoutPanel sortActions = new();
    readonly Panel detailPanel = new();
    readonly TreeView categoryTree = new();
    readonly ListView itemList = new();
    readonly ImageList smallIcons = new() { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
    readonly Dictionary<Guid, int> folderIconIndices = [];
    readonly TextBox search = new();
    readonly PictureBox preview = new();
    readonly PictureBox decoration = new();
    Image? decorationImage;
    readonly Stopwatch decorationClock = new();
    readonly System.Windows.Forms.Timer decorationTimer = new() { Interval = 16 };
    readonly Label title = new();
    readonly LinkLabel url = new();
    readonly TextBox notes = new();
    readonly Label categoryHeading = new();
    readonly Label itemHeading = new();
    readonly Label notesHeading = new();
    readonly Label previewHeading = new();
    readonly TableLayoutPanel details = new();
    readonly StatusStrip status = new();
    readonly ToolStripStatusLabel statusText = new();
    readonly ContextMenuStrip folderMenu = new();
    readonly ContextMenuStrip itemMenu = new();
    readonly ContextMenuStrip locationMenu = new();
    readonly ContextMenuStrip imageMenu = new();
    readonly ContextMenuStrip sortMenu = new();
    readonly ContextMenuStrip typeFilterMenu = new();
    readonly ContextMenuStrip tagFilterMenu = new();
    readonly ToolTip folderTips = new();
    readonly ToolTip tagHoverTip = new();
    readonly System.Windows.Forms.Timer tagHoverTimer = new() { Interval = 450 };
    ListViewItem? tagHoverRow;
    Point tagHoverPoint;
    bool updatingTagFilterMenu;
    readonly List<Button> themedButtons = [];
    Button editButton = null!, deleteButton = null!, imageButton = null!, addFolderButton = null!, upButton = null!, sortButton = null!;
    bool rebuilding;
    Guid? selectedId;
    Guid? selectedFolderId;
    Guid? contextFolderId;
    LibraryTransfer? pendingTransfer;
    readonly HashSet<string> selectedTagFilters = new(StringComparer.CurrentCultureIgnoreCase);
    ContentSort contentSort = ContentSort.NameAscending;
    bool showFolders = true, showItems = true, fittingColumns;
    readonly CancellationTokenSource closing = new();

    public MainForm(DataStore? testStore = null, bool showWelcome = false)
    {
        store = testStore ?? new DataStore(DataLocation.Read(), DataLocation.LegacyDefaultFolder);
        data = store.Load();
        Text = "腐味 · RustAji";
        Width = 1190;
        Height = 770;
        MinimumSize = new Size(950, 620);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        BuildUi();
        ApplyTheme();
        RefreshCategories(showWelcome ? data.Folders.FirstOrDefault(folder => folder.Name == "从这里开始")?.Id : null);
        if (showWelcome && itemList.Items.Count > 0) itemList.Items[0].Selected = true;
        FormClosing += (_, _) => closing.Cancel();
        FormClosed += (_, _) =>
        {
            decorationTimer.Dispose();
            decorationImage?.Dispose();
            foreach (var band in new[] { header, categoryBar, itemBar }) band.BackgroundImage?.Dispose();
        };
        Shown += (_, _) => FitContentColumns(-1);
    }

    void BuildUi()
    {
        smallIcons.Images.Add(CreateSmallIcon(true));
        smallIcons.Images.Add(CreateSmallIcon(false));
        frameLayout.Dock = DockStyle.Fill;
        frameLayout.ColumnCount = 1;
        frameLayout.RowCount = 3;
        frameLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        frameLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        frameLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        frameLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(frameLayout);
        MainMenuStrip = menu;
        menu.Dock = DockStyle.Fill;
        menu.Margin = Padding.Empty;
        menu.Padding = Padding.Empty;
        menu.AutoSize = false;
        var allItems = new ToolStripMenuItem("查看所有资料");
        allItems.Click += (_, _) => new AllItemsForm(data).ShowDialog(this);
        menu.Items.Add(allItems);
        var backup = new ToolStripMenuItem("备份");
        backup.DropDownItems.Add("导出完整备份", null, (_, _) => ExportBackup());
        backup.DropDownItems.Add("导入备份", null, (_, _) => ImportBackup());
        menu.Items.Add(backup);
        var settingsMenu = new ToolStripMenuItem("设置");
        settingsMenu.DropDownItems.Add("界面设置", null, (_, _) => OpenSettings());
        settingsMenu.DropDownItems.Add("资料位置", null, (_, _) => OpenDataLocation());
        menu.Items.Add(settingsMenu);
        var help = new ToolStripMenuItem("关于");
        help.Click += (_, _) => MessageBox.Show(this, "RustAji 腐味 1.0.1\n\n资料保存在：\n" + store.Folder + "\n\n建议定期使用“备份 → 导出完整备份”。", "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
        menu.Items.Add(help);
        foreach (ToolStripItem item in menu.Items)
        {
            item.AutoSize = false;
            item.Size = new Size(item == allItems ? 106 : 43, ChromeStyle.HeaderHeight);
            item.Margin = new Padding(0, 0, ChromeStyle.ControlGap, 0);
        }
        header.Dock = DockStyle.Fill;
        header.Height = 65;
        header.Padding = new Padding(10, 10, 10, 8);
        header.Margin = Padding.Empty;
        frameLayout.Controls.Add(header, 0, 0);
        headerRow.Dock = DockStyle.Top;
        headerRow.Height = ChromeStyle.HeaderHeight;
        headerRow.ColumnCount = 4;
        headerRow.RowCount = 1;
        headerRow.Margin = Padding.Empty;
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 263));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        headerRow.RowStyles.Add(new RowStyle(SizeType.Absolute, ChromeStyle.HeaderHeight));
        header.Controls.Add(headerRow);
        headerRow.Controls.Add(menu, 0, 0);
        searchHost.Dock = DockStyle.Fill;
        searchHost.Margin = Padding.Empty;
        searchHost.Paint += (_, e) => DrawSearchBox(e.Graphics);
        searchHost.Resize += (_, _) => LayoutSearchField();
        searchHost.Click += (_, _) => search.Focus();
        headerRow.Controls.Add(searchHost, 2, 0);
        search.BorderStyle = BorderStyle.None;
        search.PlaceholderText = "搜索名称、要点或 #标签";
        search.TextChanged += (_, _) => RefreshItems();
        searchHost.Controls.Add(search);
        LayoutSearchField();
        decoration.Dock = DockStyle.Fill;
        decoration.Margin = Padding.Empty;
        decoration.Cursor = Cursors.Hand;
        decoration.AccessibleName = "装饰图片";
        decoration.Paint += (_, e) => DrawDecoration(e.Graphics);
        decoration.Click += (_, _) =>
        {
            if (!data.Settings.AnimateDecoration) return;
            decorationClock.Restart();
            decorationTimer.Start();
        };
        decorationTimer.Tick += (_, _) =>
        {
            if (decorationClock.ElapsedMilliseconds >= 400) decorationTimer.Stop();
            decoration.Invalidate();
        };
        headerRow.Controls.Add(decoration, 3, 0);

        status.Dock = DockStyle.Fill;
        status.Margin = Padding.Empty;
        status.Items.Add(statusText);
        frameLayout.Controls.Add(status, 0, 2);
        outer.Dock = DockStyle.Fill;
        outer.Padding = new Padding(10);
        outer.Margin = Padding.Empty;
        frameLayout.Controls.Add(outer, 0, 1);

        mainLayout.Dock = DockStyle.Fill;
        mainLayout.ColumnCount = 3;
        mainLayout.RowCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.Controls.Add(mainLayout);
        categoryPanel.Dock = DockStyle.Fill;
        categoryPanel.Margin = new Padding(0, 0, 8, 0);
        itemPanel.Dock = DockStyle.Fill;
        itemPanel.Margin = new Padding(0, 0, 8, 0);
        detailPanel.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(categoryPanel, 0, 0);
        mainLayout.Controls.Add(itemPanel, 1, 0);
        mainLayout.Controls.Add(detailPanel, 2, 0);
        categoryHeading.Text = "文件夹";
        categoryHeading.Dock = DockStyle.Fill;
        categoryHeading.Height = 32;
        categoryHeading.TextAlign = ContentAlignment.MiddleLeft;
        categoryHeading.Padding = new Padding(7, 0, 0, 0);
        categoryTree.Dock = DockStyle.Fill;
        categoryTree.HideSelection = false;
        categoryTree.BorderStyle = BorderStyle.FixedSingle;
        categoryTree.DrawMode = TreeViewDrawMode.OwnerDrawText;
        categoryTree.DrawNode += DrawFolderNode;
        categoryTree.ItemDrag += (_, e) =>
        {
            if (e.Item is TreeNode node && node.Tag is Guid id)
                categoryTree.DoDragDrop(new LibraryTransfer(LibraryEntryType.Folder, id, true), DragDropEffects.Move);
        };
        categoryTree.AfterSelect += (_, _) => { if (!rebuilding) RefreshItems(); };
        categoryTree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right && e.Node is { } node) { categoryTree.SelectedNode = node; contextFolderId = node.Tag as Guid?; folderMenu.Show(categoryTree, e.Location); } };
        categoryTree.MouseUp += (_, e) =>
        {
            if (categoryTree.GetNodeAt(e.Location) is not null) return;
            if (e.Button == MouseButtons.Left) SelectFolder(null);
            else if (e.Button == MouseButtons.Right) { SelectFolder(null); contextFolderId = null; folderMenu.Show(categoryTree, e.Location); }
        };
        categoryTree.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Back) { GoUp(); e.Handled = true; }
            else if (HandleTransferShortcut(e, categoryTree.SelectedNode?.Tag as Guid?, null)) e.SuppressKeyPress = true;
        };
        categoryTree.AllowDrop = true;
        categoryTree.DragEnter += (_, e) => e.Effect = DragEffect(e, null);
        categoryTree.DragOver += (_, e) =>
        {
            var at = categoryTree.PointToClient(new Point(e.X, e.Y));
            e.Effect = DragEffect(e, categoryTree.GetNodeAt(at)?.Tag as Guid?);
        };
        categoryTree.DragDrop += (_, e) =>
        {
            var at = categoryTree.PointToClient(new Point(e.X, e.Y));
            MoveDroppedEntry(e, categoryTree.GetNodeAt(at)?.Tag as Guid?);
        };
        categoryPanel.Controls.Add(categoryTree);
        categoryBar.Dock = DockStyle.Top;
        categoryBar.Height = 50;
        categoryBar.Padding = new Padding(0, 6, 4, 11);
        categoryBar.Controls.Add(categoryHeading);
        categoryPanel.Controls.Add(categoryBar);
        folderActions.Dock = DockStyle.Right;
        folderActions.Width = 72;
        folderActions.WrapContents = false;
        folderActions.Padding = new Padding(0, 2, 0, 0);
        folderActions.Margin = Padding.Empty;
        categoryBar.Controls.Add(folderActions);
        upButton = new ChromeButton(() => data.Settings) { Width = 28, Height = 28, Margin = Padding.Empty,
            GlyphPainter = (graphics, button) => DrawFolderActionGlyph(graphics, button, false) };
        upButton.AccessibleName = "返回上级文件夹";
        upButton.Click += (_, _) => GoUp();
        folderTips.SetToolTip(upButton, "返回上级文件夹");
        folderActions.Controls.Add(upButton);
        addFolderButton = new ChromeButton(() => data.Settings) { Width = 28, Height = 28, Margin = new Padding(8, 0, 0, 0),
            GlyphPainter = (graphics, button) => DrawFolderActionGlyph(graphics, button, true) };
        addFolderButton.AccessibleName = "新建顶层文件夹";
        addFolderButton.Click += (_, _) => AddFolder(null);
        folderTips.SetToolTip(addFolderButton, "新建顶层文件夹");
        folderActions.Controls.Add(addFolderButton);
        themedButtons.Add(upButton);
        themedButtons.Add(addFolderButton);

        itemHeading.Text = "内容";
        itemHeading.Dock = DockStyle.Fill;
        itemHeading.TextAlign = ContentAlignment.MiddleLeft;
        itemHeading.Padding = new Padding(7, 0, 0, 0);
        itemBar.Dock = DockStyle.Top;
        itemBar.Height = 50;
        itemBar.Padding = new Padding(0, 6, 4, 11);
        itemBar.Controls.Add(itemHeading);
        sortActions.Dock = DockStyle.Right;
        sortActions.Width = 36;
        sortActions.WrapContents = false;
        sortActions.Padding = new Padding(4, 2, 0, 0);
        itemBar.Controls.Add(sortActions);
        sortButton = new ChromeButton(() => data.Settings) { Width = 28, Height = 28, Margin = Padding.Empty, AccessibleName = "排序",
            GlyphPainter = DrawSortGlyph };
        sortButton.Click += (_, _) => sortMenu.Show(sortButton, new Point(0, sortButton.Height));
        folderTips.SetToolTip(sortButton, "排序");
        sortActions.Controls.Add(sortButton);
        themedButtons.Add(sortButton);
        itemList.Dock = DockStyle.Fill;
        itemList.View = View.Details;
        itemList.FullRowSelect = true;
        itemList.HideSelection = false;
        itemList.MultiSelect = false;
        itemList.SmallImageList = smallIcons;
        itemList.Columns.Add("名称", 180);
        itemList.Columns.Add("类型 ▾", 68);
        itemList.Columns.Add("更新", 96);
        itemList.Columns.Add("标签 ▾", 110);
        itemList.OwnerDraw = true;
        itemList.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        itemList.DrawItem += (_, e) => { if (itemList.View != View.Details) e.DrawDefault = true; };
        itemList.DrawSubItem += DrawContentSubItem;
        tagHoverTimer.Tick += (_, _) =>
        {
            tagHoverTimer.Stop();
            if (tagHoverRow?.ListView != itemList) return;
            var tags = TagsForRow(tagHoverRow);
            if (tags.Count > 0) tagHoverTip.Show("标签" + Environment.NewLine + string.Join(Environment.NewLine, tags), itemList, tagHoverPoint.X + 12, tagHoverPoint.Y + 18, 12000);
        };
        itemList.MouseMove += (_, e) =>
        {
            var row = itemList.GetItemAt(e.X, e.Y);
            tagHoverPoint = e.Location;
            if (row == tagHoverRow) return;
            ClearTagHover();
            tagHoverRow = row;
            if (row is not null && TagsForRow(row).Count > 0) tagHoverTimer.Start();
        };
        itemList.MouseLeave += (_, _) => ClearTagHover();
        itemList.MouseWheel += (_, _) => ClearTagHover();
        itemList.MouseDown += (_, _) => ClearTagHover();
        itemList.Resize += (_, _) => FitContentColumns(-1);
        itemList.ColumnWidthChanging += (_, e) =>
        {
            if (fittingColumns || itemList.ClientSize.Width < 300) return;
            int[] minimum = [120, 64, 96, 90];
            int maximum = itemList.ClientSize.Width - 2 - minimum.Where((_, i) => i != e.ColumnIndex).Sum();
            e.NewWidth = Math.Clamp(e.NewWidth, minimum[e.ColumnIndex], Math.Max(minimum[e.ColumnIndex], maximum));
        };
        itemList.ColumnWidthChanged += (_, e) => FitContentColumns(e.ColumnIndex);
        itemList.ColumnClick += (_, e) => HandleContentHeaderClick(e.Column);
        itemList.SelectedIndexChanged += (_, _) => ShowSelected();
        itemList.DoubleClick += (_, _) => { if (SelectedListFolder is { } folder) { search.Clear(); SelectFolder(folder.Id); } else OpenLink(); };
        itemList.ItemDrag += (_, e) =>
        {
            if (e.Item is not ListViewItem row) return;
            var transfer = row.Tag switch
            {
                LibraryFolder folder => new LibraryTransfer(LibraryEntryType.Folder, folder.Id, true),
                StudyItem item => new LibraryTransfer(LibraryEntryType.Item, item.Id, true),
                _ => (LibraryTransfer?)null
            };
            if (transfer is { } value) itemList.DoDragDrop(value, DragDropEffects.Move);
        };
        itemList.AllowDrop = true;
        itemList.DragEnter += (_, e) => e.Effect = DragEffect(e, selectedFolderId);
        itemList.DragOver += (_, e) =>
        {
            var at = itemList.PointToClient(new Point(e.X, e.Y));
            e.Effect = DragEffect(e, DropFolderInList(at));
        };
        itemList.DragDrop += (_, e) =>
        {
            var at = itemList.PointToClient(new Point(e.X, e.Y));
            MoveDroppedEntry(e, DropFolderInList(at));
        };
        itemList.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            var row = itemList.GetItemAt(e.X, e.Y);
            if (row is null)
            {
                foreach (ListViewItem selected in itemList.SelectedItems) selected.Selected = false;
                locationMenu.Show(itemList, e.Location);
                return;
            }
            row.Selected = true;
            contextFolderId = (row.Tag as LibraryFolder)?.Id;
            (row.Tag is LibraryFolder ? folderMenu : itemMenu).Show(itemList, e.Location);
        };
        itemList.KeyDown += (_, e) =>
        {
            if (HandleTransferShortcut(e, SelectedListFolder?.Id, Selected?.Id)) e.SuppressKeyPress = true;
        };
        itemPanel.Controls.Add(itemList);
        itemPanel.Controls.Add(itemBar);

        details.Dock = DockStyle.Fill;
        details.ColumnCount = 1;
        details.RowCount = 6;
        details.Padding = new Padding(9, 0, 0, 0);
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 53));
        details.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        detailPanel.Controls.Add(details);
        title.Dock = DockStyle.Fill;
        title.Font = new Font(Font, FontStyle.Bold);
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.AutoEllipsis = true;
        url.Dock = DockStyle.Fill;
        url.TextAlign = ContentAlignment.MiddleLeft;
        url.LinkClicked += (_, _) => OpenLink();
        previewHeading.Text = "封面";
        previewHeading.Dock = DockStyle.Fill;
        previewHeading.TextAlign = ContentAlignment.MiddleLeft;
        preview.Dock = DockStyle.Fill;
        preview.BorderStyle = BorderStyle.FixedSingle;
        preview.SizeMode = PictureBoxSizeMode.Zoom;
        notesHeading.Text = "学习要点";
        notesHeading.Dock = DockStyle.Fill;
        notesHeading.TextAlign = ContentAlignment.MiddleLeft;
        notes.Dock = DockStyle.Fill;
        notes.ReadOnly = true;
        notes.Multiline = true;
        notes.ScrollBars = ScrollBars.Vertical;
        notes.BorderStyle = BorderStyle.FixedSingle;
        var titlePanel = new Panel { Dock = DockStyle.Fill };
        titlePanel.Controls.Add(title);
        var titleActions = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 122, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new Padding(0, 4, 0, 0) };
        deleteButton = AddButton(titleActions, "删除", DeleteItem, 53);
        editButton = AddButton(titleActions, "编辑", EditSelected, 53);
        titlePanel.Controls.Add(titleActions);
        details.Controls.Add(titlePanel, 0, 0);
        details.Controls.Add(url, 0, 1);
        var previewBar = new Panel { Dock = DockStyle.Fill };
        previewBar.Controls.Add(previewHeading);
        imageButton = new ChromeButton(() => data.Settings) { Text = "更换图片", Width = 86, Height = 27, Dock = DockStyle.Right };
        imageButton.Click += (_, _) => ChangeImage();
        previewBar.Controls.Add(imageButton);
        themedButtons.Add(imageButton);
        details.Controls.Add(previewBar, 0, 2);
        details.Controls.Add(preview, 0, 3);
        details.Controls.Add(notesHeading, 0, 4);
        details.Controls.Add(notes, 0, 5);
        preview.ContextMenuStrip = imageMenu;
        var newFolderMenu = folderMenu.Items.Add("新建子文件夹", null, (_, _) => AddFolder(contextFolderId));
        folderMenu.Items.Add("新建资料", null, (_, _) => AddItem(contextFolderId));
        folderMenu.Items.Add(new ToolStripSeparator());
        var copyFolderMenu = folderMenu.Items.Add("复制", null, (_, _) => StageFolder(contextFolderId, false));
        var cutFolderMenu = folderMenu.Items.Add("剪切", null, (_, _) => StageFolder(contextFolderId, true));
        var pasteFolderMenu = folderMenu.Items.Add("粘贴", null, (_, _) => PasteTo(contextFolderId));
        folderMenu.Items.Add(new ToolStripSeparator());
        var renameFolderMenu = folderMenu.Items.Add("编辑文件夹", null, (_, _) => EditFolder());
        var folderTagsMenu = folderMenu.Items.Add("编辑标签", null, (_, _) => EditFolderTags(contextFolderId));
        var imageFolderMenu = folderMenu.Items.Add("更换封面", null, (_, _) => ChangeImage());
        var appearanceFolderMenu = folderMenu.Items.Add("外观与图标颜色", null, (_, _) => EditFolderAppearance());
        var deleteFolderMenu = folderMenu.Items.Add("删除文件夹", null, (_, _) => DeleteFolder());
        folderMenu.Items.Add(new ToolStripSeparator());
        var folderPropertiesMenu = folderMenu.Items.Add("属性", null, (_, _) => ShowFolderProperties());
        folderMenu.Opening += (_, _) =>
        {
            newFolderMenu.Text = contextFolderId is null ? "新建文件夹" : "新建子文件夹";
            foreach (var entry in new[] { copyFolderMenu, cutFolderMenu, renameFolderMenu, folderTagsMenu, imageFolderMenu, appearanceFolderMenu, deleteFolderMenu, folderPropertiesMenu }) entry.Enabled = contextFolderId is not null;
            pasteFolderMenu.Enabled = pendingTransfer is not null;
        };
        itemMenu.Items.Add("打开链接", null, (_, _) => OpenLink());
        itemMenu.Items.Add("编辑资料", null, (_, _) => EditItem());
        itemMenu.Items.Add("编辑标签", null, (_, _) => EditItemTags(Selected?.Id));
        itemMenu.Items.Add("删除资料", null, (_, _) => DeleteItem());
        itemMenu.Items.Add(new ToolStripSeparator());
        itemMenu.Items.Add("复制", null, (_, _) => StageItem(Selected?.Id, false));
        itemMenu.Items.Add("剪切", null, (_, _) => StageItem(Selected?.Id, true));
        var pasteItemMenu = itemMenu.Items.Add("粘贴到当前文件夹", null, (_, _) => PasteTo(selectedFolderId));
        itemMenu.Items.Add(new ToolStripSeparator());
        itemMenu.Items.Add("属性", null, (_, _) => ShowItemProperties());
        itemMenu.Opening += (_, _) => pasteItemMenu.Enabled = pendingTransfer is not null;
        locationMenu.Items.Add("新建文件夹", null, (_, _) => AddFolder(selectedFolderId));
        locationMenu.Items.Add("新建资料", null, (_, _) => AddItem(selectedFolderId));
        var pasteLocationMenu = locationMenu.Items.Add("粘贴", null, (_, _) => PasteTo(selectedFolderId));
        locationMenu.Opening += (_, _) => pasteLocationMenu.Enabled = pendingTransfer is not null;
        imageMenu.Items.Add("更换图片", null, (_, _) => ChangeImage());
        imageMenu.Items.Add("重新获取网页封面", null, async (_, _) => await FetchSelectedCoverAsync());
        ConfigureContentMenus();
        statusText.Text = "资料保存在本机。请定期导出备份。";
    }

    Button AddButton(Control parent, string text, Action action, int width)
    {
        var button = new ChromeButton(() => data.Settings) { Text = text, Width = width, Height = 27, Margin = new Padding(0, 2, 7, 2) };
        button.Click += (_, _) => action();
        parent.Controls.Add(button);
        themedButtons.Add(button);
        return button;
    }

    void LayoutSearchField()
    {
        search.Left = 32;
        search.Top = Math.Max(1, (searchHost.ClientSize.Height - search.PreferredHeight) / 2);
        search.Width = Math.Max(30, searchHost.ClientSize.Width - 42);
    }

    void DrawSearchBox(Graphics graphics)
    {
        if (searchHost.Width < 12 || searchHost.Height < 12) return;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var shape = RoundedPath(new Rectangle(0, 0, searchHost.Width - 1, searchHost.Height - 1), ChromeStyle.CornerRadius);
        using var fill = new SolidBrush(ParseColor(data.Settings.PanelColor, Color.White));
        using var outline = new Pen(ParseColor(data.Settings.BorderColor, Color.Gray), 1);
        graphics.FillPath(fill, shape);
        graphics.DrawPath(outline, shape);
        using var icon = new Pen(ParseColor(data.Settings.TextColor, Color.FromArgb(70, 70, 70)), 1.6f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float y = searchHost.Height / 2f;
        graphics.DrawEllipse(icon, 10, y - 6, 9, 9);
        graphics.DrawLine(icon, 18, y + 2, 23, y + 7);
    }

    static void DrawFolderActionGlyph(Graphics graphics, Button button, bool plus)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color color = button.Enabled ? button.ForeColor : SystemColors.GrayText;
        float scale = Math.Max(1f, button.DeviceDpi / 96f);
        using var pen = new Pen(color, 1.7f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float x = (button.ClientSize.Width - 1) / 2f, y = (button.ClientSize.Height - 1) / 2f;
        float span = Math.Min(button.ClientSize.Width, button.ClientSize.Height) * 0.21f;
        graphics.DrawLine(pen, x, y - span, x, y + span);
        if (plus) graphics.DrawLine(pen, x - span, y, x + span, y);
        else
        {
            graphics.DrawLine(pen, x - span * 0.83f, y - span * 0.15f, x, y - span);
            graphics.DrawLine(pen, x, y - span, x + span * 0.83f, y - span * 0.15f);
        }
    }

    static void DrawSortGlyph(Graphics graphics, Button button)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(button.Enabled ? button.ForeColor : SystemColors.GrayText, 1.5f);
        float x = button.ClientSize.Width / 2f - 6, y = button.ClientSize.Height / 2f - 5;
        graphics.DrawLine(pen, x, y, x + 12, y);
        graphics.DrawLine(pen, x, y + 5, x + 9, y + 5);
        graphics.DrawLine(pen, x, y + 10, x + 6, y + 10);
    }

    void ConfigureContentMenus()
    {
        typeFilterMenu.Closing += KeepFilterOpenWhileSelecting;
        tagFilterMenu.Closing += KeepFilterOpenWhileSelecting;
        var folders = new ToolStripMenuItem("文件夹") { CheckOnClick = true, Checked = true };
        var items = new ToolStripMenuItem("资料") { CheckOnClick = true, Checked = true };
        folders.CheckedChanged += (_, _) => { showFolders = folders.Checked; UpdateContentHeaders(); RefreshItems(); };
        items.CheckedChanged += (_, _) => { showItems = items.Checked; UpdateContentHeaders(); RefreshItems(); };
        typeFilterMenu.Items.AddRange([folders, items]);

        (string Label, ContentSort Sort)[] options =
        [
            ("名称 A → Z", ContentSort.NameAscending),
            ("名称 Z → A", ContentSort.NameDescending),
            ("文件夹在前", ContentSort.FolderFirst),
            ("资料在前", ContentSort.ItemFirst),
            ("创建时间：新到旧", ContentSort.CreatedNewest),
            ("创建时间：旧到新", ContentSort.CreatedOldest),
            ("更新时间：新到旧", ContentSort.UpdatedNewest),
            ("更新时间：旧到新", ContentSort.UpdatedOldest)
        ];
        foreach (var option in options)
        {
            var menuItem = new ToolStripMenuItem(option.Label) { Tag = option.Sort };
            menuItem.Click += (_, _) => SetContentSort(option.Sort);
            sortMenu.Items.Add(menuItem);
        }
        sortMenu.Opening += (_, _) =>
        {
            foreach (ToolStripMenuItem option in sortMenu.Items) option.Checked = (ContentSort)option.Tag! == contentSort;
        };
        tagFilterMenu.Opening += (_, _) =>
        {
            tagFilterMenu.Items.Clear();
            var all = new ToolStripMenuItem("全部标签") { Checked = selectedTagFilters.Count == 0 };
            all.Click += (_, _) =>
            {
                updatingTagFilterMenu = true;
                selectedTagFilters.Clear();
                foreach (var option in tagFilterMenu.Items.OfType<ToolStripMenuItem>().Skip(1)) option.Checked = false;
                all.Checked = true;
                updatingTagFilterMenu = false;
                UpdateContentHeaders();
                RefreshItems();
            };
            tagFilterMenu.Items.Add(all);
            var allTags = data.Folders.SelectMany(x => x.Tags).Concat(data.Items.SelectMany(x => x.Tags))
                .Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();
            if (allTags.Count > 0) tagFilterMenu.Items.Add(new ToolStripSeparator());
            foreach (string tag in allTags)
            {
                var option = new ToolStripMenuItem(tag) { CheckOnClick = true, Checked = selectedTagFilters.Contains(tag) };
                option.CheckedChanged += (_, _) =>
                {
                    if (updatingTagFilterMenu) return;
                    if (option.Checked) selectedTagFilters.Add(tag);
                    else selectedTagFilters.Remove(tag);
                    all.Checked = selectedTagFilters.Count == 0;
                    UpdateContentHeaders();
                    RefreshItems();
                };
                tagFilterMenu.Items.Add(option);
            }
            ChromeStyle.Apply(tagFilterMenu, data.Settings, Font);
        };
        UpdateContentHeaders();
    }

    static void KeepFilterOpenWhileSelecting(object? sender, ToolStripDropDownClosingEventArgs e)
    {
        if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked) e.Cancel = true;
    }

    static IReadOnlyList<string> TagsForRow(ListViewItem row) => row.Tag switch
    {
        LibraryFolder folder => folder.Tags,
        StudyItem item => item.Tags,
        _ => []
    };

    void ClearTagHover()
    {
        tagHoverTimer.Stop();
        tagHoverTip.Hide(itemList);
        tagHoverRow = null;
    }

    void SetContentSort(ContentSort sort)
    {
        contentSort = sort;
        UpdateContentHeaders();
        RefreshItems();
    }

    void UpdateContentHeaders()
    {
        itemList.Columns[0].Text = contentSort == ContentSort.NameAscending ? "名称 ↑" : contentSort == ContentSort.NameDescending ? "名称 ↓" : "名称";
        itemList.Columns[1].Text = showFolders && showItems ? "类型 ▾" : "类型 •";
        itemList.Columns[2].Text = contentSort == ContentSort.UpdatedNewest ? "更新 ↓" : contentSort == ContentSort.UpdatedOldest ? "更新 ↑" : "更新";
        itemList.Columns[3].Text = selectedTagFilters.Count == 0 ? "标签 ▾" : "标签 •";
    }

    void HandleContentHeaderClick(int column)
    {
        if (column == 0) SetContentSort(contentSort == ContentSort.NameAscending ? ContentSort.NameDescending : ContentSort.NameAscending);
        else if (column == 2) SetContentSort(contentSort == ContentSort.UpdatedNewest ? ContentSort.UpdatedOldest : ContentSort.UpdatedNewest);
        else if (column is 1 or 3)
        {
            int left = 0;
            for (int i = 0; i < column; i++) left += itemList.Columns[i].Width;
            (column == 1 ? typeFilterMenu : tagFilterMenu).Show(itemList, new Point(left + 4, 24));
        }
    }

    void FitContentColumns(int changedColumn)
    {
        if (fittingColumns || itemList.Columns.Count != 4 || !itemList.IsHandleCreated) return;
        int available = itemList.ClientSize.Width - 2;
        if (available < 300) return;
        int[] widths = ContentView.FitColumnWidths(itemList.Columns.Cast<ColumnHeader>().Select(x => x.Width).ToArray(), available, changedColumn);
        fittingColumns = true;
        try
        {
            for (int i = 0; i < widths.Length; i++)
                if (itemList.Columns[i].Width != widths[i]) itemList.Columns[i].Width = widths[i];
        }
        finally { fittingColumns = false; }
    }

    void DrawContentSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.ColumnIndex == 0 && e.Item?.Tag is LibraryFolder folder && (folder.LabelTextColor is not null || folder.LabelFillColor is not null))
        {
            bool selectedFolder = e.Item.Selected;
            using var folderBackground = new SolidBrush(selectedFolder ? SystemColors.Highlight : itemList.BackColor);
            e.Graphics.FillRectangle(folderBackground, e.Bounds);
            if (e.Item.ImageIndex >= 0 && e.Item.ImageIndex < smallIcons.Images.Count)
                e.Graphics.DrawImage(smallIcons.Images[e.Item.ImageIndex], e.Bounds.Left + 2, e.Bounds.Top + (e.Bounds.Height - 16) / 2, 16, 16);
            var labelBounds = new Rectangle(e.Bounds.Left + 21, e.Bounds.Top + 1, Math.Max(1, e.Bounds.Width - 24), e.Bounds.Height - 2);
            DrawFolderLabel(e.Graphics, folder, labelBounds, selectedFolder, itemList.BackColor);
            return;
        }
        if (e.ColumnIndex != 3) { e.DrawDefault = true; return; }
        if (e.Item is null) return;
        bool selected = e.Item.Selected;
        using var background = new SolidBrush(selected ? SystemColors.Highlight : itemList.BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        IReadOnlyList<string> tags = TagsForRow(e.Item);
        int x = e.Bounds.Left + 4, height = Math.Min(19, e.Bounds.Height - 3);
        int top = e.Bounds.Top + (e.Bounds.Height - height) / 2;
        for (int i = 0; i < tags.Count; i++)
        {
            string tag = tags[i];
            int available = e.Bounds.Right - x - 3;
            if (available < 24) break;
            int desired = TextRenderer.MeasureText(tag, itemList.Font, Size.Empty, TextFormatFlags.NoPadding).Width + 12;
            int remaining = tags.Count - i - 1;
            int reserve = remaining > 0 ? 33 : 0;
            if (available < desired + reserve)
            {
                int counterWidth = Math.Min(available, 31);
                using var counter = RoundedPath(new Rectangle(x, top, counterWidth, height), 3);
                using var counterFill = new SolidBrush(selected ? Color.FromArgb(218, 230, 240) : Color.FromArgb(238, 238, 238));
                using var counterBorder = new Pen(Color.FromArgb(171, 171, 171));
                e.Graphics.FillPath(counterFill, counter);
                e.Graphics.DrawPath(counterBorder, counter);
                TextRenderer.DrawText(e.Graphics, "+" + (tags.Count - i), itemList.Font, new Rectangle(x + 3, top + 1, counterWidth - 5, height - 2), Color.FromArgb(50, 50, 50), TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);
                break;
            }
            int width = desired;
            using var chip = RoundedPath(new Rectangle(x, top, width, height), 3);
            using var fill = new SolidBrush(selected ? Color.FromArgb(218, 230, 240) : Color.FromArgb(238, 238, 238));
            using var border = new Pen(Color.FromArgb(171, 171, 171));
            e.Graphics.FillPath(fill, chip);
            e.Graphics.DrawPath(border, chip);
            TextRenderer.DrawText(e.Graphics, tag, itemList.Font, new Rectangle(x + 6, top + 1, width - 10, height - 2), Color.FromArgb(50, 50, 50), TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            x += width + 4;
        }
    }

    void DrawFolderNode(object? sender, DrawTreeNodeEventArgs e)
    {
        if (e.Node?.Tag is not Guid id) { e.DrawDefault = true; return; }
        var folder = data.Folders.FirstOrDefault(x => x.Id == id);
        if (folder is null || folder.LabelTextColor is null && folder.LabelFillColor is null) { e.DrawDefault = true; return; }
        bool selected = e.Node.IsSelected;
        using var background = new SolidBrush(selected ? SystemColors.Highlight : categoryTree.BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        DrawFolderLabel(e.Graphics, folder, e.Bounds, selected, categoryTree.BackColor);
    }

    void DrawFolderLabel(Graphics graphics, LibraryFolder folder, Rectangle bounds, bool selected, Color normalBackground)
    {
        if (!selected && folder.LabelFillColor is not null)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var color = FolderAppearance.ColorOr(folder.LabelFillColor, normalBackground);
            using var path = FolderAppearance.RoundedRectangle(bounds, folder.LabelCornerRadius);
            using var fill = new SolidBrush(Color.FromArgb((int)Math.Round(folder.LabelFillOpacity * 255d / 100), color));
            graphics.FillPath(fill, path);
        }
        var textColor = selected ? SystemColors.HighlightText : FolderAppearance.ColorOr(folder.LabelTextColor, itemList.ForeColor);
        TextRenderer.DrawText(graphics, folder.Name, itemList.Font, new Rectangle(bounds.Left + 3, bounds.Top, Math.Max(1, bounds.Width - 5), bounds.Height), textColor, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    static Bitmap CreateSmallIcon(bool folder, Color? folderColor = null)
    {
        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var line = new Pen(Color.FromArgb(100, 100, 100), 1);
        if (folder)
        {
            using var fill = new SolidBrush(folderColor ?? Color.FromArgb(210, 205, 190));
            g.FillRectangle(fill, 1, 5, 14, 9);
            g.FillRectangle(fill, 2, 3, 6, 4);
            g.DrawRectangle(line, 1, 5, 13, 8);
            g.DrawLine(line, 2, 3, 8, 3);
        }
        else
        {
            using var fill = new SolidBrush(Color.FromArgb(250, 250, 248));
            g.FillRectangle(fill, 3, 1, 10, 14);
            g.DrawRectangle(line, 3, 1, 9, 13);
            g.DrawLine(line, 5, 6, 11, 6);
            g.DrawLine(line, 5, 9, 11, 9);
        }
        return bitmap;
    }

    void DrawDecoration(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var state = g.Save();
        float cx = decoration.ClientSize.Width / 2f, cy = decoration.ClientSize.Height / 2f;
        double t = decorationTimer.Enabled ? decorationClock.Elapsed.TotalMilliseconds : 400;
        (float sx, float sy) = DecorationScale(t);
        g.TranslateTransform(cx, cy);
        g.ScaleTransform(sx, sy);
        g.TranslateTransform(-cx, -cy);
        if (decorationImage is not null)
        {
            int size = Math.Min(Math.Clamp(data.Settings.DecorationSize, 20, 80), Math.Min(decoration.Width - 4, decoration.Height - 2));
            float fit = Math.Min(size / (float)decorationImage.Width, size / (float)decorationImage.Height);
            float width = decorationImage.Width * fit, height = decorationImage.Height * fit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(decorationImage, cx - width / 2, cy - height / 2, width, height);
            g.Restore(state);
            return;
        }
        using var brush = new SolidBrush(ParseColor(data.Settings.AccentColor, Color.DarkOliveGreen));
        using var pen = new Pen(brush, 1.3f);
        float x = cx, y = cy;
        g.FillEllipse(brush, x - 10, y - 12, 9, 9);
        g.FillEllipse(brush, x + 1, y - 12, 9, 9);
        g.FillEllipse(brush, x - 10, y - 1, 9, 9);
        g.FillEllipse(brush, x + 1, y - 1, 9, 9);
        g.DrawLine(pen, x, y + 5, x - 4, y + 15);
        g.Restore(state);
    }

    internal static (float X, float Y) DecorationScale(double elapsedMilliseconds)
    {
        (double Time, float X, float Y)[] frames = [(0, 1, 1), (85, 1.035f, .89f), (175, .985f, 1.04f), (265, 1.008f, .985f), (400, 1, 1)];
        if (elapsedMilliseconds <= 0 || elapsedMilliseconds >= 400) return (1, 1);
        for (int i = 1; i < frames.Length; i++)
        {
            if (elapsedMilliseconds > frames[i].Time) continue;
            double progress = (elapsedMilliseconds - frames[i - 1].Time) / (frames[i].Time - frames[i - 1].Time);
            float eased = (float)(progress * progress * (3 - 2 * progress));
            return (frames[i - 1].X + (frames[i].X - frames[i - 1].X) * eased,
                frames[i - 1].Y + (frames[i].Y - frames[i - 1].Y) * eased);
        }
        return (1, 1);
    }

    void RefreshCategories(Guid? selectFolder = null)
    {
        smallIcons.Images.Clear();
        smallIcons.Images.Add(CreateSmallIcon(true));
        smallIcons.Images.Add(CreateSmallIcon(false));
        folderIconIndices.Clear();
        foreach (var folder in data.Folders)
        {
            if (folder.IconColor is null) continue;
            folderIconIndices[folder.Id] = smallIcons.Images.Count;
            smallIcons.Images.Add(CreateSmallIcon(true, FolderAppearance.ColorOr(folder.IconColor, Color.FromArgb(210, 205, 190))));
        }
        rebuilding = true;
        Guid? wanted = selectFolder ?? selectedFolderId;
        categoryTree.BeginUpdate();
        categoryTree.Nodes.Clear();
        AddChildren(categoryTree.Nodes, null);
        categoryTree.SelectedNode = wanted is Guid id ? FindNode(categoryTree.Nodes, id) : null;
        categoryTree.SelectedNode?.EnsureVisible();
        categoryTree.EndUpdate();
        rebuilding = false;
        RefreshItems();
    }

    void AddChildren(TreeNodeCollection nodes, Guid? parentId)
    {
        foreach (var folder in data.Folders.Where(x => x.ParentId == parentId).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            int imageIndex = folderIconIndices.GetValueOrDefault(folder.Id, 0);
            var node = new TreeNode(folder.Name) { Tag = folder.Id, ImageIndex = imageIndex, SelectedImageIndex = imageIndex };
            nodes.Add(node);
            AddChildren(node.Nodes, folder.Id);
        }
    }

    static TreeNode? FindNode(TreeNodeCollection nodes, Guid id)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is Guid found && found == id) return node;
            var child = FindNode(node.Nodes, id);
            if (child is not null) return child;
        }
        return null;
    }

    void RefreshItems(Guid? select = null)
    {
        if (rebuilding) return;
        ClearTagHover();
        Guid? wanted = select ?? selectedId;
        selectedFolderId = categoryTree.SelectedNode?.Tag as Guid?;
        upButton.Enabled = selectedFolderId is not null;
        var visible = ContentView.Build(data, selectedFolderId, search.Text, showFolders, showItems, selectedTagFilters, contentSort);
        itemList.BeginUpdate();
        itemList.Items.Clear();
        foreach (var entry in visible)
        {
            int imageIndex = entry.Folder is { } folder ? folderIconIndices.GetValueOrDefault(folder.Id, 0) : 1;
            var row = new ListViewItem(entry.Name, imageIndex) { Tag = (object?)entry.Folder ?? entry.Item };
            row.SubItems.Add(entry.IsFolder ? "文件夹" : "资料");
            row.SubItems.Add(entry.UpdatedAt?.LocalDateTime.ToString("yyyy-MM-dd") ?? "");
            row.SubItems.Add(string.Join("  ", entry.Tags));
            itemList.Items.Add(row);
            if (entry.Item?.Id == wanted) row.Selected = true;
        }
        itemList.EndUpdate();
        if (itemList.SelectedItems.Count == 0) { selectedId = null; ShowSelected(); }
        statusText.Text = $"当前显示 {visible.Count(x => x.IsFolder)} 个文件夹、{visible.Count(x => !x.IsFolder)} 条资料。";
    }

    StudyItem? Selected => itemList.SelectedItems.Count == 0 ? null : itemList.SelectedItems[0].Tag as StudyItem;
    LibraryFolder? SelectedListFolder => itemList.SelectedItems.Count == 0 ? null : itemList.SelectedItems[0].Tag as LibraryFolder;
    LibraryFolder? CurrentFolder => selectedFolderId is Guid id ? data.Folders.FirstOrDefault(x => x.Id == id) : null;

    void ShowSelected()
    {
        var item = Selected;
        var folder = SelectedListFolder ?? (item is null ? CurrentFolder : null);
        selectedId = item?.Id;
        title.Text = item?.Title ?? folder?.Name ?? "";
        url.Text = item is null ? "" : string.IsNullOrWhiteSpace(item.Url) ? "" : item.Url;
        url.LinkColor = ParseColor(data.Settings.AccentColor, Color.DarkGreen);
        notes.Text = item?.Notes ?? folder?.Description ?? "";
        notesHeading.Text = item is not null ? "学习要点" : folder is not null ? "说明" : "";
        SetPreviewImage(store.ImagePath(item?.ImageFile ?? folder?.ImageFile) ?? store.AssetPath(data.Settings.WallpaperFile));
        editButton.Enabled = item is not null || folder is not null;
        deleteButton.Enabled = item is not null;
        imageButton.Enabled = item is not null || folder is not null;
        imageMenu.Items[1].Enabled = item is not null && DataStore.IsWebUrl(item.Url);
    }

    void SetPreviewImage(string? path)
    {
        var previous = preview.Image;
        preview.Image = null;
        previous?.Dispose();
        if (path is null) return;
        try
        {
            using var image = Image.FromFile(path);
            preview.Image = new Bitmap(image);
        }
        catch { /* Missing or unsupported image should never hide the record. */ }
    }

    void AddItem(Guid? targetFolderId)
    {
        using var editor = new ItemEditor(data, null, targetFolderId);
        if (editor.ShowDialog(this) != DialogResult.OK || editor.Result is null) return;
        SaveEdited(editor.Result, editor.ChosenImagePath, null);
        if (editor.AutoFetchCover && editor.ChosenImagePath is null && editor.Result.Url.Length > 0) _ = FetchForItemAsync(editor.Result.Id, false);
    }

    void EditItem()
    {
        var original = Selected;
        if (original is null) return;
        using var editor = new ItemEditor(data, original);
        if (editor.ShowDialog(this) != DialogResult.OK || editor.Result is null) return;
        SaveEdited(editor.Result, editor.ChosenImagePath, original);
        if (editor.AutoFetchCover && editor.ChosenImagePath is null && editor.Result.Url.Length > 0) _ = FetchForItemAsync(editor.Result.Id, false);
    }

    void EditSelected()
    {
        if (Selected is not null) EditItem();
        else EditFolder();
    }

    void EditItemTags(Guid? id)
    {
        var item = data.Items.FirstOrDefault(x => x.Id == id);
        if (item is null) return;
        using var editor = new TagEditor(data.Settings, item.Tags);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        var previous = item.Tags;
        var previousUpdated = item.UpdatedAt;
        item.Tags = editor.Result;
        item.UpdatedAt = DateTimeOffset.Now;
        try { store.Save(data); selectedId = item.Id; RefreshItems(); statusText.Text = "标签已保存。"; }
        catch (Exception error) { item.Tags = previous; item.UpdatedAt = previousUpdated; ShowOperationError("保存标签", error); }
    }

    void EditFolderTags(Guid? id)
    {
        var folder = data.Folders.FirstOrDefault(x => x.Id == id);
        if (folder is null) return;
        using var editor = new TagEditor(data.Settings, folder.Tags);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        var previous = folder.Tags;
        var previousUpdated = folder.UpdatedAt;
        folder.Tags = editor.Result;
        folder.UpdatedAt = DateTimeOffset.Now;
        try { store.Save(data); RefreshItems(); statusText.Text = "标签已保存。"; }
        catch (Exception error) { folder.Tags = previous; folder.UpdatedAt = previousUpdated; ShowOperationError("保存标签", error); }
    }

    void SaveEdited(StudyItem item, string? imagePath, StudyItem? original)
    {
        try
        {
            if (imagePath is not null) item.ImageFile = store.SaveImage(imagePath);
            int index = data.Items.FindIndex(x => x.Id == item.Id);
            if (index >= 0) data.Items[index] = item;
            else data.Items.Add(item);
            try { store.Save(data); }
            catch
            {
                if (index >= 0 && original is not null) data.Items[index] = original;
                else data.Items.Remove(item);
                throw;
            }
            selectedId = item.Id;
            RefreshCategories(item.FolderId);
            statusText.Text = "资料已保存。";
        }
        catch (Exception error) { ShowOperationError("保存资料", error); }
    }

    void DeleteItem()
    {
        var item = Selected;
        if (item is null) return;
        if (MessageBox.Show(this, $"确定删除“{item.Title}”吗？\n建议先导出备份。", "删除资料", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        int index = data.Items.IndexOf(item);
        data.Items.RemoveAt(index);
        try { store.Save(data); selectedId = null; RefreshCategories(); statusText.Text = "资料已删除。"; }
        catch (Exception error) { data.Items.Insert(index, item); ShowOperationError("删除资料", error); }
    }

    void OpenLink()
    {
        var item = Selected;
        if (item is null || !DataStore.IsWebUrl(item.Url)) return;
        try { Process.Start(new ProcessStartInfo(item.Url) { UseShellExecute = true }); }
        catch (Exception error) { ShowOperationError("打开链接", error); }
    }

    void ChangeImage()
    {
        var item = Selected;
        var folder = SelectedListFolder ?? (item is null ? CurrentFolder : null);
        if (item is null && folder is null) return;
        using var dialog = new OpenFileDialog { Title = "选择配图", Filter = "图片|*.png;*.jpg;*.jpeg;*.gif;*.bmp|所有文件|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            string? previous = item?.ImageFile ?? folder?.ImageFile;
            var previousFolderUpdated = folder?.UpdatedAt;
            string image = store.SaveImage(dialog.FileName);
            if (item is not null) { item.ImageFile = image; item.UpdatedAt = DateTimeOffset.Now; }
            else { folder!.ImageFile = image; folder.UpdatedAt = DateTimeOffset.Now; }
            try { store.Save(data); }
            catch { if (item is not null) item.ImageFile = previous; else { folder!.ImageFile = previous; folder.UpdatedAt = previousFolderUpdated; } throw; }
            ShowSelected();
            statusText.Text = "图片已更换。";
        }
        catch (Exception error) { ShowOperationError("更换图片", error); }
    }

    async Task FetchSelectedCoverAsync()
    {
        var item = Selected;
        if (item is not null) await FetchForItemAsync(item.Id, true);
    }

    async Task FetchForItemAsync(Guid id, bool showFailure)
    {
        var item = data.Items.FirstOrDefault(x => x.Id == id);
        if (item is null || !DataStore.IsWebUrl(item.Url)) return;
        statusText.Text = "正在寻找网页封面…";
        try
        {
            byte[]? bytes = await ThumbnailService.FindCoverAsync(item.Url, closing.Token);
            if (bytes is null)
            {
                statusText.Text = "未找到可用封面；可以点击“更换图片”手动选择。";
                if (showFailure) MessageBox.Show(this, "这个网页没有提供可读取的封面，或网站限制了自动访问。\n可以使用“更换图片”选择本机图片。", "未找到封面", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string? previous = item.ImageFile;
            item.ImageFile = store.SaveImage(bytes);
            item.UpdatedAt = DateTimeOffset.Now;
            try { store.Save(data); }
            catch { item.ImageFile = previous; throw; }
            if (Selected?.Id == id) ShowSelected();
            statusText.Text = "已保存网页封面。";
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            statusText.Text = "自动寻找封面失败；可以手动选择图片。";
            if (showFailure) ShowOperationError("寻找封面", error);
        }
        finally { imageMenu.Items[1].Enabled = Selected is { Url.Length: > 0 }; }
    }

    void SelectFolder(Guid? folderId)
    {
        var node = folderId is Guid id ? FindNode(categoryTree.Nodes, id) : null;
        if (folderId is not null && node is null) return;
        node?.EnsureVisible();
        categoryTree.SelectedNode = node;
        selectedFolderId = folderId;
        RefreshItems();
    }

    void GoUp()
    {
        var parentId = CurrentFolder?.ParentId;
        SelectFolder(parentId);
    }

    bool HandleTransferShortcut(KeyEventArgs e, Guid? folderId, Guid? itemId)
    {
        if (e.Modifiers != Keys.Control) return false;
        if (e.KeyCode == Keys.V) { PasteTo(folderId ?? selectedFolderId); return true; }
        if (e.KeyCode != Keys.C && e.KeyCode != Keys.X) return false;
        bool cut = e.KeyCode == Keys.X;
        if (folderId is Guid folder) StageFolder(folder, cut);
        else if (itemId is Guid item) StageItem(item, cut);
        else return false;
        return true;
    }

    void StageFolder(Guid? folderId, bool cut)
    {
        var folder = data.Folders.FirstOrDefault(x => x.Id == folderId);
        if (folder is null) return;
        pendingTransfer = new LibraryTransfer(LibraryEntryType.Folder, folder.Id, cut);
        statusText.Text = cut ? $"已剪切文件夹“{folder.Name}”，请选择目标位置并粘贴。" : $"已复制文件夹“{folder.Name}”，请选择目标位置并粘贴。";
    }

    void StageItem(Guid? itemId, bool cut)
    {
        var item = data.Items.FirstOrDefault(x => x.Id == itemId);
        if (item is null) return;
        pendingTransfer = new LibraryTransfer(LibraryEntryType.Item, item.Id, cut);
        statusText.Text = cut ? $"已剪切资料“{item.Title}”，请选择目标位置并粘贴。" : $"已复制资料“{item.Title}”，请选择目标位置并粘贴。";
    }

    void PasteTo(Guid? destination)
    {
        if (pendingTransfer is not LibraryTransfer transfer) return;
        try
        {
            var result = LibraryOrganizer.Paste(data, transfer, destination);
            if (!result.Changed)
            {
                statusText.Text = "内容已经在这个位置。";
                return;
            }
            store.Save(result.Data);
            data = result.Data;
            if (transfer.Cut) pendingTransfer = null;
            selectedFolderId = transfer.Type == LibraryEntryType.Folder ? result.ResultId : destination;
            selectedId = transfer.Type == LibraryEntryType.Item ? result.ResultId : null;
            RefreshCategories();
            statusText.Text = transfer.Cut ? "已移动到目标位置。" : "已复制到目标位置。";
        }
        catch (InvalidOperationException error)
        {
            MessageBox.Show(this, error.Message, "无法粘贴", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception error) { ShowOperationError("粘贴", error); }
    }

    void AddFolder(Guid? parentId = null)
    {
        using var editor = new FolderEditor(data.Settings, null);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        if (data.Folders.Any(x => x.ParentId == parentId && string.Equals(x.Name, editor.FolderName, StringComparison.CurrentCultureIgnoreCase)))
        { MessageBox.Show(this, "这个位置已有同名文件夹。", "名称重复"); return; }
        var now = DateTimeOffset.Now;
        var folder = new LibraryFolder { ParentId = parentId, Name = editor.FolderName, Description = editor.Description, CreatedAt = now, UpdatedAt = now };
        data.Folders.Add(folder);
        try { store.Save(data); RefreshCategories(folder.Id); statusText.Text = "文件夹已创建。"; }
        catch (Exception error) { data.Folders.Remove(folder); ShowOperationError("新建文件夹", error); }
    }

    void EditFolder()
    {
        var folder = SelectedListFolder ?? CurrentFolder;
        if (folder is null) return;
        using var editor = new FolderEditor(data.Settings, folder);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        if (editor.FolderName == folder.Name && editor.Description == folder.Description) return;
        if (data.Folders.Any(x => x.Id != folder.Id && x.ParentId == folder.ParentId && string.Equals(x.Name, editor.FolderName, StringComparison.CurrentCultureIgnoreCase)))
        { MessageBox.Show(this, "这个位置已有同名文件夹。", "名称重复"); return; }
        string previousName = folder.Name, previousDescription = folder.Description;
        var previousUpdated = folder.UpdatedAt;
        folder.Name = editor.FolderName;
        folder.Description = editor.Description;
        folder.UpdatedAt = DateTimeOffset.Now;
        try { store.Save(data); RefreshCategories(folder.Id); statusText.Text = "文件夹已保存。"; }
        catch (Exception error) { folder.Name = previousName; folder.Description = previousDescription; folder.UpdatedAt = previousUpdated; ShowOperationError("编辑文件夹", error); }
    }

    void EditFolderAppearance()
    {
        var folder = data.Folders.FirstOrDefault(x => x.Id == contextFolderId);
        if (folder is null) return;
        using var editor = new FolderAppearanceEditor(data.Settings, folder);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        var previous = (folder.LabelTextColor, folder.LabelFillColor, folder.LabelFillOpacity, folder.LabelCornerRadius, folder.IconColor, folder.UpdatedAt);
        folder.LabelTextColor = editor.TextColor;
        folder.LabelFillColor = editor.FillColor;
        folder.LabelFillOpacity = editor.FillOpacity;
        folder.LabelCornerRadius = editor.CornerRadius;
        folder.IconColor = editor.IconColor;
        folder.UpdatedAt = DateTimeOffset.Now;
        try { store.Save(data); RefreshCategories(selectedFolderId); statusText.Text = "文件夹外观已保存。"; }
        catch (Exception error)
        {
            (folder.LabelTextColor, folder.LabelFillColor, folder.LabelFillOpacity, folder.LabelCornerRadius, folder.IconColor, folder.UpdatedAt) = previous;
            ShowOperationError("保存文件夹外观", error);
        }
    }

    void ShowFolderProperties()
    {
        var folder = data.Folders.FirstOrDefault(x => x.Id == contextFolderId);
        if (folder is null) return;
        using var dialog = new EntryPropertiesForm(data, folder, store);
        dialog.ShowDialog(this);
    }

    void ShowItemProperties()
    {
        if (Selected is not { } item) return;
        using var dialog = new EntryPropertiesForm(data, item, store);
        dialog.ShowDialog(this);
    }

    void DeleteFolder()
    {
        var folder = SelectedListFolder ?? CurrentFolder;
        if (folder is null) return;
        var ids = new HashSet<Guid> { folder.Id };
        bool changed;
        do
        {
            int count = ids.Count;
            foreach (var child in data.Folders.Where(x => x.ParentId is Guid parent && ids.Contains(parent))) ids.Add(child.Id);
            changed = ids.Count != count;
        } while (changed);
        int itemCount = data.Items.Count(x => x.FolderId is Guid id && ids.Contains(id));
        string message = itemCount == 0 ? $"删除文件夹“{folder.Name}”及其中的子文件夹？" : $"文件夹“{folder.Name}”包含 {itemCount} 条资料。删除后这些资料也会被删除。确定继续吗？";
        if (MessageBox.Show(this, message, "删除文件夹", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var oldFolders = data.Folders.ToList();
        var oldItems = data.Items.ToList();
        data.Folders.RemoveAll(x => ids.Contains(x.Id));
        data.Items.RemoveAll(x => x.FolderId is Guid id && ids.Contains(id));
        try { store.Save(data); selectedId = null; RefreshCategories(folder.ParentId); statusText.Text = "文件夹已删除。"; }
        catch (Exception error) { data.Folders = oldFolders; data.Items = oldItems; ShowOperationError("删除文件夹", error); }
    }

    Guid? DropFolderInList(Point location) => itemList.GetItemAt(location.X, location.Y)?.Tag is LibraryFolder folder ? folder.Id : selectedFolderId;

    DragDropEffects DragEffect(DragEventArgs e, Guid? destination) =>
        e.Data?.GetData(typeof(LibraryTransfer)) is LibraryTransfer transfer && LibraryOrganizer.CanMove(data, transfer, destination)
            ? DragDropEffects.Move : DragDropEffects.None;

    void MoveDroppedEntry(DragEventArgs e, Guid? destination)
    {
        if (e.Data?.GetData(typeof(LibraryTransfer)) is not LibraryTransfer transfer || !LibraryOrganizer.CanMove(data, transfer, destination)) return;
        try
        {
            var result = LibraryOrganizer.Paste(data, transfer, destination);
            store.Save(result.Data);
            data = result.Data;
            selectedId = transfer.Type == LibraryEntryType.Item ? result.ResultId : null;
            selectedFolderId = destination;
            RefreshCategories(destination);
            statusText.Text = transfer.Type == LibraryEntryType.Folder ? "文件夹已移动。" : "资料已移动。";
        }
        catch (InvalidOperationException error) { MessageBox.Show(this, error.Message, "无法移动", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        catch (Exception error) { ShowOperationError("移动内容", error); }
    }

    void ExportBackup()
    {
        using var dialog = new SaveFileDialog { Title = "导出完整备份", Filter = "RustAji 备份|*.tlb", FileName = "RustAji-" + DateTime.Now.ToString("yyyy-MM-dd") + ".tlb", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { store.ExportBackup(data, dialog.FileName); statusText.Text = "完整备份已导出。"; }
        catch (Exception error) { ShowOperationError("导出备份", error); }
    }

    void ImportBackup()
    {
        using var dialog = new OpenFileDialog { Title = "导入备份", Filter = "备份文件|*.tlb;*.json|新版完整备份|*.tlb|旧版网页 JSON 备份|*.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (MessageBox.Show(this, "导入会替换当前资料和设置。建议先导出当前备份。\n确定继续吗？", "导入备份", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            var imported = store.ImportBackup(dialog.FileName);
            data = imported;
            pendingTransfer = null;
            selectedId = null;
            selectedFolderId = null;
            selectedTagFilters.Clear();
            showFolders = showItems = true;
            contentSort = ContentSort.NameAscending;
            UpdateContentHeaders();
            search.Clear();
            ApplyTheme();
            RefreshCategories();
            statusText.Text = $"已导入 {data.Items.Count} 条资料。";
        }
        catch (Exception error) { ShowOperationError("导入备份", error); }
    }

    void CleanupMedia()
    {
        try
        {
            var removed = store.CleanupUnusedMedia(data);
            statusText.Text = $"已整理图片：移除 {removed.Images} 张资料配图、{removed.Assets} 张装饰或壁纸。恢复副本仍需要的图片会保留。";
        }
        catch (Exception error) { ShowOperationError("整理图片", error); }
    }

    void OpenSettings()
    {
        using var dialog = new SettingsForm(data.Settings, store);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var old = data.Settings;
        data.Settings = dialog.Result;
        try { store.Save(data); ApplyTheme(); RefreshCategories(); statusText.Text = "设置已保存。"; }
        catch (Exception error) { data.Settings = old; ShowOperationError("保存设置", error); }
    }

    void OpenDataLocation()
    {
        using var dialog = new DataLocationForm(store.Folder);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Destination is not { } destination) return;
        try
        {
            if (dialog.Choice == DataLocationChoice.Migrate)
                destination = DataLocation.Migrate(store, data, destination);
            else if (dialog.Choice == DataLocationChoice.OpenExisting)
            {
                if (!File.Exists(Path.Combine(destination, "library.json")))
                    throw new InvalidDataException("所选文件夹里没有 library.json。请选择完整的腐味资料目录。 ");
            }
            else return;
            var replacement = new DataStore(destination);
            var loaded = replacement.Load();
            DataLocation.Save(replacement.Folder);
            store = replacement;
            data = loaded;
            pendingTransfer = null;
            selectedId = null;
            selectedFolderId = null;
            contextFolderId = null;
            selectedTagFilters.Clear();
            showFolders = showItems = true;
            contentSort = ContentSort.NameAscending;
            search.Clear();
            UpdateContentHeaders();
            ApplyTheme();
            RefreshCategories();
            statusText.Text = "资料位置已切换：" + store.Folder;
        }
        catch (Exception error) { ShowOperationError("切换资料位置", error); }
    }

    void ApplyTheme()
    {
        var settings = data.Settings;
        Color window = ParseColor(settings.WindowColor, Color.FromArgb(236, 236, 236));
        Color panel = ParseColor(settings.PanelColor, Color.White);
        Color text = ParseColor(settings.TextColor, Color.FromArgb(32, 32, 32));
        Color accent = ParseColor(settings.AccentColor, Color.DarkOliveGreen);
        Color button = ParseColor(settings.ButtonColor, Color.FromArgb(232, 232, 232));
        Color border = ParseColor(settings.BorderColor, Color.Gray);
        Font font;
        try { font = new Font(settings.FontFamily, Math.Clamp(settings.FontSize, 8, 24)); }
        catch { font = new Font("Microsoft YaHei UI", 9); }
        Font = font;
        foreach (Control control in new Control[] { menu, header, headerRow, searchHost, status, details, categoryBar, folderActions, itemBar, sortActions, categoryHeading, itemHeading, notesHeading, previewHeading, decoration })
        {
            control.BackColor = window;
            control.ForeColor = text;
        }
        foreach (var band in new[] { header, categoryBar, itemBar }) ChromeStyle.SetQuietBackground(band);
        foreach (Control control in new Control[] { categoryTree, itemList, notes, search, preview })
        {
            control.BackColor = panel;
            control.ForeColor = text;
            control.Font = font;
        }
        outer.BackColor = window;
        mainLayout.BackColor = window;
        categoryPanel.BackColor = window;
        itemPanel.BackColor = window;
        detailPanel.BackColor = window;
        title.ForeColor = text;
        title.Font = new Font(font, FontStyle.Bold);
        url.Font = font;
        url.LinkColor = accent;
        statusText.ForeColor = text;
        menu.Font = font;
        menu.Renderer = new HeaderMenuRenderer(() => data.Settings);
        int menuWidth = 0;
        foreach (ToolStripItem item in menu.Items)
        {
            item.Font = font;
            item.ForeColor = text;
            item.BackColor = panel;
            int textWidth = TextRenderer.MeasureText(item.Text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
            item.Size = new Size(Math.Max(item == menu.Items[0] ? 106 : 43, textWidth + 20), ChromeStyle.HeaderHeight);
            menuWidth += item.Width + ChromeStyle.ControlGap;
            if (item is ToolStripMenuItem top && top.HasDropDownItems)
                ChromeStyle.Apply(top.DropDown, settings, font);
        }
        headerRow.ColumnStyles[0].Width = menuWidth;
        foreach (var context in new[] { folderMenu, itemMenu, locationMenu, imageMenu, sortMenu, typeFilterMenu, tagFilterMenu })
            ChromeStyle.Apply(context, settings, font);
        foreach (var control in themedButtons)
        {
            if (control.Parent is not null) control.Parent.BackColor = window;
            control.BackColor = button;
            control.ForeColor = text;
            control.FlatStyle = FlatStyle.Flat;
            control.FlatAppearance.BorderColor = border;
            control.FlatAppearance.MouseOverBackColor = ControlPaint.Light(button);
            control.Font = font;
            control.Invalidate();
        }
        menu.Invalidate();
        var oldWallpaper = outer.BackgroundImage;
        outer.BackgroundImage = LoadImage(store.AssetPath(settings.WallpaperFile));
        outer.BackgroundImageLayout = ImageLayout.Stretch;
        oldWallpaper?.Dispose();
        var oldDecoration = decorationImage;
        decorationImage = LoadImage(store.AssetPath(settings.DecorationFile));
        decoration.Width = Math.Clamp(settings.DecorationSize, 20, 80) + 10;
        headerRow.ColumnStyles[3].Width = decoration.Width;
        oldDecoration?.Dispose();
        decoration.Invalidate();
        LayoutSearchField();
        searchHost.Invalidate();
        itemList.Invalidate();
        ShowSelected();
    }

    static Image? LoadImage(string? path)
    {
        if (path is null) return null;
        try { using var original = Image.FromFile(path); return new Bitmap(original); }
        catch { return null; }
    }

    static Color ParseColor(string value, Color fallback)
    {
        try { return ColorTranslator.FromHtml(value); }
        catch { return fallback; }
    }

    void ShowOperationError(string operation, Exception error)
    {
        statusText.Text = operation + "失败：" + error.Message;
        MessageBox.Show(this, statusText.Text, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
