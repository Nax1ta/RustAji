namespace TutorialLibrary;

public enum LibraryEntryType { Folder, Item }

public readonly record struct LibraryTransfer(LibraryEntryType Type, Guid Id, bool Cut);

public readonly record struct LibraryPasteResult(LibraryData Data, Guid ResultId, bool Changed);

public static class LibraryOrganizer
{
    public static bool CanMove(LibraryData data, LibraryTransfer transfer, Guid? destination)
    {
        if (destination is Guid target && data.Folders.All(x => x.Id != target)) return false;
        if (transfer.Type == LibraryEntryType.Item)
            return data.Items.FirstOrDefault(x => x.Id == transfer.Id) is { } item && item.FolderId != destination;
        var folder = data.Folders.FirstOrDefault(x => x.Id == transfer.Id);
        if (folder is null || folder.ParentId == destination) return false;
        for (Guid? current = destination; current is Guid id;)
        {
            if (id == folder.Id) return false;
            current = data.Folders.First(x => x.Id == id).ParentId;
        }
        return true;
    }

    public static LibraryPasteResult Paste(LibraryData source, LibraryTransfer transfer, Guid? destination)
    {
        if (destination is Guid target && source.Folders.All(x => x.Id != target))
            throw new InvalidOperationException("目标文件夹已不存在。");

        return transfer.Type == LibraryEntryType.Folder
            ? PasteFolder(source, transfer, destination)
            : PasteItem(source, transfer, destination);
    }

    static LibraryPasteResult PasteFolder(LibraryData source, LibraryTransfer transfer, Guid? destination)
    {
        var original = source.Folders.FirstOrDefault(x => x.Id == transfer.Id)
            ?? throw new InvalidOperationException("要粘贴的文件夹已不存在。");
        for (Guid? current = destination; current is Guid id;)
        {
            if (id == original.Id) throw new InvalidOperationException("不能把文件夹放进自身或它的子文件夹。");
            current = source.Folders.First(x => x.Id == id).ParentId;
        }
        if (transfer.Cut && original.ParentId == destination)
            return new LibraryPasteResult(source, original.Id, false);

        var result = Clone(source);
        string name = UniqueName(result.Folders.Where(x => x.ParentId == destination && (!transfer.Cut || x.Id != original.Id)).Select(x => x.Name), original.Name, 100, !transfer.Cut);
        if (transfer.Cut)
        {
            var moved = result.Folders.First(x => x.Id == original.Id);
            moved.ParentId = destination;
            moved.Name = name;
            moved.UpdatedAt = DateTimeOffset.Now;
            return new LibraryPasteResult(result, moved.Id, true);
        }

        var subtree = new List<LibraryFolder> { original };
        for (int i = 0; i < subtree.Count; i++)
            subtree.AddRange(source.Folders.Where(x => x.ParentId == subtree[i].Id));
        var map = new Dictionary<Guid, Guid>();
        foreach (var folder in subtree)
        {
            Guid newId = Guid.NewGuid();
            map[folder.Id] = newId;
            result.Folders.Add(new LibraryFolder
            {
                Id = newId,
                ParentId = folder.Id == original.Id ? destination : map[folder.ParentId!.Value],
                Name = folder.Id == original.Id ? name : folder.Name,
                Description = folder.Description,
                Tags = [.. folder.Tags],
                ImageFile = folder.ImageFile,
                LabelTextColor = folder.LabelTextColor,
                LabelFillColor = folder.LabelFillColor,
                LabelFillOpacity = folder.LabelFillOpacity,
                LabelCornerRadius = folder.LabelCornerRadius,
                IconColor = folder.IconColor,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now
            });
        }
        foreach (var item in source.Items)
            if (item.FolderId is Guid oldFolder && map.TryGetValue(oldFolder, out Guid newFolder))
                result.Items.Add(CloneItem(item, Guid.NewGuid(), newFolder));
        return new LibraryPasteResult(result, map[original.Id], true);
    }

    static LibraryPasteResult PasteItem(LibraryData source, LibraryTransfer transfer, Guid? destination)
    {
        var original = source.Items.FirstOrDefault(x => x.Id == transfer.Id)
            ?? throw new InvalidOperationException("要粘贴的资料已不存在。");
        if (transfer.Cut && original.FolderId == destination)
            return new LibraryPasteResult(source, original.Id, false);

        var result = Clone(source);
        string name = UniqueName(result.Items.Where(x => x.FolderId == destination && (!transfer.Cut || x.Id != original.Id)).Select(x => x.Title), original.Title, 150, !transfer.Cut);
        if (transfer.Cut)
        {
            var moved = result.Items.First(x => x.Id == original.Id);
            moved.FolderId = destination;
            moved.Title = name;
            moved.UpdatedAt = DateTimeOffset.Now;
            return new LibraryPasteResult(result, moved.Id, true);
        }
        var copy = CloneItem(original, Guid.NewGuid(), destination);
        copy.Title = name;
        copy.CreatedAt = DateTimeOffset.Now;
        copy.UpdatedAt = copy.CreatedAt;
        result.Items.Add(copy);
        return new LibraryPasteResult(result, copy.Id, true);
    }

    static LibraryData Clone(LibraryData source) => new()
    {
        Version = source.Version,
        Settings = source.Settings,
        Folders = source.Folders.Select(x => new LibraryFolder { Id = x.Id, ParentId = x.ParentId, Name = x.Name, Description = x.Description, Tags = [.. x.Tags], ImageFile = x.ImageFile, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt, LabelTextColor = x.LabelTextColor, LabelFillColor = x.LabelFillColor, LabelFillOpacity = x.LabelFillOpacity, LabelCornerRadius = x.LabelCornerRadius, IconColor = x.IconColor }).ToList(),
        Items = source.Items.Select(x => CloneItem(x, x.Id, x.FolderId)).ToList()
    };

    static StudyItem CloneItem(StudyItem source, Guid id, Guid? folderId) => new()
    {
        Id = id,
        FolderId = folderId,
        Categories = [.. source.Categories],
        Title = source.Title,
        Url = source.Url,
        Notes = source.Notes,
        Tags = [.. source.Tags],
        ImageFile = source.ImageFile,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt
    };

    static string UniqueName(IEnumerable<string> occupied, string requested, int maxLength, bool copy)
    {
        var used = new HashSet<string>(occupied, StringComparer.CurrentCultureIgnoreCase);
        if (!used.Contains(requested)) return requested;
        for (int index = 1; ; index++)
        {
            string suffix = copy ? index == 1 ? " - 副本" : $" - 副本 {index}" : $" - {index + 1}";
            string candidate = requested[..Math.Min(requested.Length, maxLength - suffix.Length)] + suffix;
            if (!used.Contains(candidate)) return candidate;
        }
    }
}
