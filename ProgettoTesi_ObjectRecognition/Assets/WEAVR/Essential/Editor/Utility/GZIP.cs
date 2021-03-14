using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TXT.WEAVR.Editor
{
    public class GZIP
    {
        private class Archive : IDisposable
        {
            public TarArchive tarArchive;
            private Stream outStream;
            private Stream gzoStream;

            public Archive(string filename, bool subsequentZip = false)
            {
                outStream = File.Create(filename);
                if (subsequentZip)
                {
                    gzoStream = new GZipOutputStream(outStream);
                    tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);
                }
                else
                {
                    tarArchive = TarArchive.CreateOutputTarArchive(outStream);
                }
            }

            public void Dispose()
            {
                try
                {
                    tarArchive?.Close();
                    outStream?.Close();
                    gzoStream?.Close();
                }
                finally
                {
                    tarArchive?.Dispose();
                    gzoStream?.Dispose();
                    outStream?.Dispose();
                }
            }
        }

        public static void CreateTarGZFromFiles(string tgzFilename, params string[] files)
        {
            using (Archive archive = new Archive(tgzFilename))
            {
                var tarArchive = archive.tarArchive;
                AddFilesToTarArchive(files, tarArchive);

                tarArchive.Close();
            }
        }

        public static async Task CreateTarGZFromFilesAsync(string tgzFilename, params string[] files)
        {
            using (Archive archive = new Archive(tgzFilename))
            {
                var tarArchive = archive.tarArchive;
                await Task.Run(() => AddFilesToTarArchive(files, tarArchive));

                tarArchive.Close();
            }
        }

        private static void AddFilesToTarArchive(string[] files, TarArchive tarArchive)
        {
            // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
            // and must not end with a slash, otherwise cuts off first char of filename
            // This is scheduled for fix in next release
            foreach (var file in files)
            {
                tarArchive.RootPath = file.Replace('\\', '/');
                if (tarArchive.RootPath.EndsWith("/"))
                {
                    tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);
                }
                AddFileToTar(tarArchive, file);
            }
        }

        private static TarArchive CreateTarArchive(string tgzFilename)
        {
            Stream outStream = File.Create(tgzFilename);
            Stream gzoStream = new GZipOutputStream(outStream);
            TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);
            return tarArchive;
        }

        public static void CreateTarGZ(string tgzFilename, string sourceDirectory)
        {
            using (Archive archive = new Archive(tgzFilename))
            {
                // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                // and must not end with a slash, otherwise cuts off first char of filename
                // This is scheduled for fix in next release
                var tarArchive = archive.tarArchive;
                tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
                if (tarArchive.RootPath.EndsWith("/"))
                {
                    tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);
                }
                AddDirectoryFilesToTar(tarArchive, sourceDirectory, true, true);
            }
        }

        public static async Task CreateTarGZAsync(string tgzFilename, string sourceDirectory)
        {
            using (Archive archive = new Archive(tgzFilename))
            {

                // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                // and must not end with a slash, otherwise cuts off first char of filename
                // This is scheduled for fix in next release
                var tarArchive = archive.tarArchive;
                tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
                if (tarArchive.RootPath.EndsWith("/"))
                    tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

                await Task.Run(() => AddDirectoryFilesToTar(tarArchive, sourceDirectory, true, true));
            }
        }

        private static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse, bool isRoot)
        {

            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            //
            TarEntry tarEntry;

            if (!isRoot)
            {
                tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
                tarArchive.WriteEntry(tarEntry, false);
            }

            // Write each file to the tar.
            //
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                AddFileToTar(tarArchive, filename);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                {
                    AddDirectoryFilesToTar(tarArchive, directory, recurse, false);
                }
            }
        }

        private static TarEntry AddFileToTar(TarArchive tarArchive, string filename)
        {
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(filename);
            tarArchive.WriteEntry(tarEntry, true);
            return tarEntry;
        }
    }
}