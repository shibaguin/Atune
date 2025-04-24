using System.Collections.Generic;
using System.IO;

namespace Atune.Data.Interfaces
{
    public interface IFoldersRepository
    {
        List<FileSystemInfo> GetMediaFiles(DirectoryInfo dir, bool acceptDirs);
    }
}
