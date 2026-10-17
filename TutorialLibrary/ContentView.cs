namespace TutorialLibrary;

public enum ContentSort
{
    NameAscending,
    NameDescending,
    FolderFirst,
    ItemFirst,
    CreatedNewest,
    CreatedOldest,
    UpdatedNewest,
    UpdatedOldest
}

public sealed record ContentEntry(LibraryFolder? Folder, StudyItem? Item, DateTimeOffset? FolderLatestUpdate = null)
{
    public string Name => Folder?.Name ?? Item!.Title;
    public IReadOnlyList<string> Tags => Folder?.Tags ?? Item!.Tags;
    public DateTimeOffset? CreatedAt => Folder?.CreatedAt ?? Item?.CreatedAt;
    public DateTimeOffset? UpdatedAt => Folder is null ? Item?.UpdatedAt : FolderLatestUpdate ?? Folder.UpdatedAt;
    public bool IsFolder => Folder is not null;
}

public static class ContentView
{
    public static IReadOnlyDictionary<Guid, DateTimeOffset?> LatestFolderUpdates(LibraryData data)
    {
        var folders = data.Folders.ToDictionary(x => x.Id);
        var latest = data.Folders.ToDictionary(x => x.Id, x => x.UpdatedAt);
        var remainingChildren = data.Folders.ToDictionary(x => x.Id, _ => 0);
        foreach (var folder in data.Folders)
            if (folder.ParentId is { } parent && remainingChildren.ContainsKey(parent)) remainingChildren[parent]++;
        foreach (var item in data.Items)
            if (item.FolderId is { } id && latest.ContainsKey(id)) latest[id] = Max(latest[id], item.UpdatedAt);

        var ready = new Queue<Guid>(remainingChildren.Where(x => x.Value == 0).Select(x => x.Key));
        while (ready.TryDequeue(out var id))
        {
            if (folders[id].ParentId is not { } parent || !folders.ContainsKey(parent)) continue;
            latest[parent] = Max(latest[parent], latest[id]);
            if (--remainingChildren[parent] == 0) ready.Enqueue(parent);
        }
        return latest;
    }

    static DateTimeOffset? Max(DateTimeOffset? left, DateTimeOffset? right) =>
        left is null ? right : right is null || left >= right ? left : right;

    public static int[] FitColumnWidths(IReadOnlyList<int> current, int available, int changedColumn)
    {
        if (current.Count != 4 || available < 4) throw new ArgumentOutOfRangeException(nameof(available));
        int[] minimum = [120, 64, 96, 90];
        if (available < minimum.Sum())
        {
            int fixedWidth = 0;
            for (int i = 0; i < 3; i++) { minimum[i] = Math.Max(1, minimum[i] * available / 370); fixedWidth += minimum[i]; }
            minimum[3] = available - fixedWidth;
        }
        int[] widths = current.Select((value, i) => Math.Max(minimum[i], value)).ToArray();
        int excess = widths.Sum() - available;
        if (excess > 0)
        {
            foreach (int i in new[] { 3, 0, 2, 1 })
            {
                if (i == changedColumn) continue;
                int shrink = Math.Min(excess, widths[i] - minimum[i]);
                widths[i] -= shrink;
                excess -= shrink;
                if (excess == 0) break;
            }
            if (excess > 0 && changedColumn >= 0)
                widths[changedColumn] -= Math.Min(excess, widths[changedColumn] - minimum[changedColumn]);
        }
        else if (excess < 0) widths[changedColumn == 0 ? 3 : 0] -= excess;
        return widths;
    }

    public static List<ContentEntry> Build(LibraryData data, Guid? folderId, string query, bool showFolders, bool showItems, IReadOnlyCollection<string> selectedTags, ContentSort sort)
    {
        var latestFolderUpdates = LatestFolderUpdates(data);
        query = query.Trim();
        bool searchEverywhere = query.Length > 0;
        IEnumerable<ContentEntry> entries =
            data.Folders.Where(x => showFolders && (searchEverywhere || x.ParentId == folderId)).Select(x => new ContentEntry(x, null, latestFolderUpdates[x.Id]))
            .Concat(data.Items.Where(x => showItems && (searchEverywhere || x.FolderId == folderId)).Select(x => new ContentEntry(null, x)));

        if (searchEverywhere)
        {
            string term = query.StartsWith('#') ? query[1..].Trim() : query;
            bool tagOnly = query.StartsWith('#');
            entries = entries.Where(x =>
                x.Tags.Any(t => t.Contains(term, StringComparison.CurrentCultureIgnoreCase)) ||
                !tagOnly && (x.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    (x.Folder?.Description ?? x.Item?.Notes ?? "").Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    (x.Item?.Url ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
        if (selectedTags.Count > 0)
        {
            var matchingFolders = MatchingFoldersForTags(data, selectedTags);
            entries = entries.Where(x => x.IsFolder
                ? matchingFolders.Contains(x.Folder!.Id)
                : x.Tags.Any(t => selectedTags.Contains(t, StringComparer.CurrentCultureIgnoreCase)));
        }

        return sort switch
        {
            ContentSort.NameDescending => entries.OrderBy(x => x.IsFolder ? 0 : 1).ThenByDescending(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.FolderFirst => entries.OrderBy(x => x.IsFolder ? 0 : 1).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.ItemFirst => entries.OrderBy(x => x.IsFolder ? 1 : 0).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.CreatedNewest => entries.OrderByDescending(x => x.CreatedAt ?? DateTimeOffset.MinValue).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.CreatedOldest => entries.OrderBy(x => x.CreatedAt ?? DateTimeOffset.MinValue).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.UpdatedNewest => entries.OrderByDescending(x => x.UpdatedAt ?? DateTimeOffset.MinValue).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            ContentSort.UpdatedOldest => entries.OrderBy(x => x.UpdatedAt ?? DateTimeOffset.MinValue).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList(),
            _ => entries.OrderBy(x => x.IsFolder ? 0 : 1).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList()
        };
    }

    public static HashSet<Guid> MatchingFoldersForTags(LibraryData data, IReadOnlyCollection<string> selectedTags)
    {
        var folders = data.Folders.ToDictionary(x => x.Id);
        var matching = new HashSet<Guid>();
        void IncludeAncestors(Guid? folderId)
        {
            while (folderId is Guid id && folders.TryGetValue(id, out var folder) && matching.Add(id)) folderId = folder.ParentId;
        }
        foreach (var folder in data.Folders)
            if (folder.Tags.Any(t => selectedTags.Contains(t, StringComparer.CurrentCultureIgnoreCase))) IncludeAncestors(folder.Id);
        foreach (var item in data.Items)
            if (item.Tags.Any(t => selectedTags.Contains(t, StringComparer.CurrentCultureIgnoreCase))) IncludeAncestors(item.FolderId);
        return matching;
    }
}
