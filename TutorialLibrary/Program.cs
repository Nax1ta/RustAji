namespace TutorialLibrary;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => ShowError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception) ShowError(exception);
        };
        try
        {
            string? dataFolder = Environment.GetEnvironmentVariable("TUTORIAL_LIBRARY_DATA_DIR");
            DataStore store = string.IsNullOrWhiteSpace(dataFolder)
                ? OpenConfiguredStore() ?? throw new OperationCanceledException("已取消打开资料目录。")
                : new DataStore(dataFolder);
            bool firstLaunch = WelcomeLibrary.Ensure(store);
            Application.Run(new MainForm(store, firstLaunch));
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { ShowError(exception); }
    }

    static DataStore? OpenConfiguredStore()
    {
        string previousVersion = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "腐味-创意版");
        while (true)
        {
            try { return new DataStore(DataLocation.Read(), previousVersion); }
            catch (DirectoryNotFoundException error)
            {
                if (MessageBox.Show(error.Message + "\n\n连接原磁盘后重试，或选择已有资料目录。", "资料目录暂时不可用", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) != DialogResult.Retry)
                    return null;
                using var picker = new FolderBrowserDialog { Description = "选择含有 library.json 的腐味资料目录", UseDescriptionForTitle = true };
                if (picker.ShowDialog() != DialogResult.OK) return null;
                try { DataLocation.Save(picker.SelectedPath); }
                catch (Exception invalid) { MessageBox.Show(invalid.Message, "无法打开目录", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
        }
    }

    static void ShowError(Exception exception)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "腐味");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "errors.log"), $"[{DateTimeOffset.Now:O}] {exception}\n");
        }
        catch { /* Logging must never hide the original error. */ }
        MessageBox.Show($"程序遇到错误：{exception.Message}\n\n数据文件未自动切换或清空。", "腐味", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
