using GrepWinUi.Core;

namespace GrepWinUi.Core.Tests;

public class AppInfoTests
{
    [Fact]
    public void MainWindowTitle_ShowsProductNameAndThreePartVersion()
    {
        string title = AppInfo.MainWindowTitle(new Version(1, 2, 3, 4));

        Assert.Equal("grepWinUi 1.2.3", title);
    }
}
