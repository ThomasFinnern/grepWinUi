using System.Text.Json;
using GrepWinUi.Core.AppData.Config;
using SaveAndLoad;

namespace GrepWinUi.Core.Tests;

public sealed class AppConfigTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GrepWinUi.Core.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void NewConfig_HasDocumentedDefaults()
    {
        AppConfig config = new();

        Assert.False(config.OpenSearchFormOnStart);
        Assert.False(config.RepeatLastSearchOnStart);
        Assert.False(config.ReopenLastSearchesOnStart);
        Assert.False(config.ShowLastResultOnStart);
        Assert.True(config.ShowFileNamesWhileSearching);
        Assert.False(config.ShowProgressInPercent);
        Assert.Equal(new ResultViewOptions(), config.ResultView);
        Assert.Equal(0, config.ResultView.KeepLinesBefore);
        Assert.True(config.ResultView.ShowLineNumbers);
        Assert.False(config.ResultView.ShowDoubleBlanks);
    }

    [Fact]
    public void NewConfig_SearchStringTokens_StartValues()
    {
        TokenListOptions tokens = new AppConfig().SearchStringTokens;

        Assert.Equal(7, tokens.PreFixedCount);
        Assert.Equal(3, tokens.FirstFixedCount);
        Assert.Equal(30, tokens.MaxCount);
        Assert.Equal(15, tokens.MaxFixedCount);
    }

    [Fact]
    public void NewConfig_FileNameTokens_SameStartValues()
    {
        Assert.Equal(new TokenListOptions(), new AppConfig().FileNameTokens);
        Assert.NotSame(new AppConfig().SearchStringTokens, new AppConfig().FileNameTokens);
    }

    [Fact]
    public void NewConfig_FolderNameTokens_40StandardEntries()
    {
        TokenListOptions folders = new AppConfig().FolderNameTokens;

        Assert.Equal(7, folders.PreFixedCount);
        Assert.Equal(3, folders.FirstFixedCount);
        Assert.Equal(40, folders.MaxCount);
        Assert.Equal(15, folders.MaxFixedCount);
    }

    [Fact]
    public void OldConfigFileWithoutTokenSection_GetsDefaults()
    {
        AppConfig config = JsonSerializer.Deserialize<AppConfig>("{ \"ShowProgressInPercent\": true }")!;

        Assert.True(config.ShowProgressInPercent);
        Assert.Equal(new TokenListOptions(), config.SearchStringTokens);
        Assert.Equal(40, config.FolderNameTokens.MaxCount);
    }

    [Fact]
    public void DefaultsFile_IsReadAndMatchesBuiltInDefaults()
    {
        StorageFolders folders = new(
            Path.Combine(_root, "User"),
            Path.Combine(AppContext.BaseDirectory, "Defaults"));
        ObjectStore store = new(folders);

        LoadResult<AppConfig> result = store.Load<AppConfig>();

        Assert.Equal(LoadSource.DefaultsFile, result.Source);
        Assert.Null(result.Problem);
        Assert.Equal(JsonSerializer.Serialize(new AppConfig()), JsonSerializer.Serialize(result.Value));
    }

    [Fact]
    public void SaveThenLoad_KeepsAllValuesIncludingResultView()
    {
        ObjectStore store = new(new StorageFolders(Path.Combine(_root, "User"), Path.Combine(_root, "NoDefaults")));
        AppConfig saved = new()
        {
            OpenSearchFormOnStart = true,
            ShowProgressInPercent = true,
            ResultView = new ResultViewOptions { KeepLinesBefore = 3, ShowLinesAfter = 2, FixedFont = false },
        };

        store.Save(saved);
        AppConfig loaded = store.Load<AppConfig>().Value;

        Assert.True(loaded.OpenSearchFormOnStart);
        Assert.True(loaded.ShowProgressInPercent);
        Assert.Equal(saved.ResultView, loaded.ResultView);
    }

    [Fact]
    public void Copy_IsDeepAndIndependent()
    {
        AppConfig original = new() { ShowProgressInPercent = true };
        original.ResultView.KeepLinesAfter = 4;

        AppConfig copy = original.Copy();
        copy.ShowProgressInPercent = false;
        copy.ResultView.KeepLinesAfter = 9;

        Assert.True(original.ShowProgressInPercent);
        Assert.Equal(4, original.ResultView.KeepLinesAfter);
        Assert.Equal(9, copy.ResultView.KeepLinesAfter);
        Assert.NotSame(original.ResultView, copy.ResultView);
    }

    [Fact]
    public void ResultViewCopy_IsIndependentOfConfig()
    {
        AppConfig config = new();

        ResultViewOptions formView = config.ResultView with { };
        formView.ShowLinesBefore = 5;
        formView.FixedFont = false;

        Assert.Equal(0, config.ResultView.ShowLinesBefore);
        Assert.True(config.ResultView.FixedFont);
    }
}
