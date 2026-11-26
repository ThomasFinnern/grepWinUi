namespace GrepWinUi.Core.Search;

/// <summary>
/// The "Folder name" text as a list of folders (as in the old program): separated by blanks, a name with
/// blanks in double quotes. A text that is one existing folder as a whole is that folder, also without
/// quotes (e.g. <c>d:\My Folder</c>).
/// </summary>
public static class FolderList
{
    /// <summary>Folders of <paramref name="text"/>, in input order, without duplicates (case ignored).</summary>
    /// <param name="text">Folder name text of the search query.</param>
    /// <param name="folderExists">Folder check; null = <see cref="Directory.Exists(string)"/>.</param>
    public static IReadOnlyList<string> Split(string text, Func<string, bool>? folderExists = null)
    {
        folderExists ??= Directory.Exists;
        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return [];
        }

        string whole = trimmed.Trim('"').Trim();
        if (!whole.Contains('"') && whole.Contains(' ') && folderExists(whole))
        {
            return [whole];
        }

        List<string> folders = [];
        int index = 0;
        while (index < trimmed.Length)
        {
            if (char.IsWhiteSpace(trimmed[index]))
            {
                index++;
                continue;
            }

            int end;
            string folder;
            if (trimmed[index] == '"')
            {
                end = trimmed.IndexOf('"', index + 1);
                if (end < 0)
                {
                    end = trimmed.Length;
                }
                folder = trimmed[(index + 1)..end].Trim();
                end++;
            }
            else
            {
                end = index;
                while (end < trimmed.Length && !char.IsWhiteSpace(trimmed[end]) && trimmed[end] != '"')
                {
                    end++;
                }
                folder = trimmed[index..end];
            }

            if (folder.Length > 0 && !folders.Contains(folder, StringComparer.OrdinalIgnoreCase))
            {
                folders.Add(folder);
            }
            index = end;
        }
        return folders;
    }
}
