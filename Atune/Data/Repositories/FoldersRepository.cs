using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Atune.Data.Repositories
{
    public class FoldersRepository : IFoldersRepository
    {
        private static readonly string[] SupportedExtensions = { "mp3", "mp4", "m4a", "aac", "ogg", "wav" };
        private const string NoMedia = ".nomedia";

        public List<FileSystemInfo> GetMediaFiles(DirectoryInfo dir, bool acceptDirs)
        {
            var result = new List<FileSystemInfo>();
            // Добавляем родительскую директорию, если есть
            if (dir.Parent != null)
            {
                result.Add(dir.Parent);
            }
            if (dir.Exists)
            {
                var children = dir.GetFileSystemInfos()
                    .Where(f =>
                    {
                        if (f is FileInfo file)
                        {
                            return file.Name != NoMedia && CheckFileExt(file.Name);
                        }
                        else if (f is DirectoryInfo subDir)
                        {
                            return acceptDirs && CheckDir(subDir);
                        }
                        return false;
                    })
                    .OrderBy(f => f is DirectoryInfo ? 0 : 1)
                    .ThenBy(f => f.Name)
                    .ToList();

                result.AddRange(children);
            }
            return result;
        }

        private bool CheckDir(DirectoryInfo dir)
        {
            try
            {
                var files = dir.GetFileSystemInfos().Where(f =>
                {
                    if (f.Name == "." || f.Name == "..")
                        return false;
                    if (f is FileInfo file)
                        return CheckFileExt(file.Name);
                    if (f is DirectoryInfo)
                        return true;
                    return false;
                });
                return dir.Exists && files.Any();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private bool CheckFileExt(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            int p = name.LastIndexOf('.') + 1;
            if (p < 1)
                return false;
            var ext = name.Substring(p).ToLowerInvariant();
            return SupportedExtensions.Contains(ext);
        }
    }
} 
