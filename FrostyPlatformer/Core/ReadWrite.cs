#nullable enable
using System;
using System.IO;
using System.Text.Json;

namespace FrostyPlatformer.Core
{
    public class ReadWrite
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented          = true,
            PropertyNameCaseInsensitive = true,
        };

        private string Root { get; set; }
        public string GetRoot { get { return Root; } }

        private bool EnableWriteToLog { get; set; }


        public ReadWrite(bool enableWriteToLog = false)
        {
            EnableWriteToLog = enableWriteToLog;
            Root = GetCorrectPath();
        }

        private string GetCorrectPath()
        {
            Root = System.IO.Path.Combine(Environment.CurrentDirectory);
            var BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
           
            return BaseDirectory;
        }


        public T? ReadJson<T>(string FilePath, string FileName, string FileExtension, bool CreateFile = true)
        {
            try
            {
                var FullPath = CreateIfNotExists(FilePath, FileName, FileExtension, CreateFile);
                string json = File.ReadAllText(FullPath);
                if (!string.IsNullOrEmpty(json))
                {
                    return JsonSerializer.Deserialize<T>(json, _jsonOptions);
                }
                else
                {
                    WriteToLog(String.Format("ReadJson - Read All Text Is Null Or Empty. Path: {0}. Filename: {1}. Extension: {2}.", FilePath, FileName, FileExtension));
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                WriteToLog(ex.ToString());
                return default(T);
            }
        }
        public bool WriteJson<T>(string FilePath, string FileName, string FileExtension, T obj)
        {
            try
            {
                var FullPath = CreateIfNotExists(FilePath, FileName, FileExtension);
                string json = JsonSerializer.Serialize(obj, _jsonOptions);

                System.IO.File.WriteAllText(FullPath, json);
            }
            catch (Exception ex)
            {
                WriteToLog(ex.ToString());
                return false;
            }
            return true;
        }

        public void WriteToLog(string Msg)
        {
            if (EnableWriteToLog)
            {
                var fullDirectory = CreateIfNotExists("\\Log", "\\log", ".txt");
                string[] lines = {
                    "--------------------------------"+DateTime.Now+"--------------------------------",
                    Msg,
                    "END"
                };

                using (StreamWriter writer = new StreamWriter(fullDirectory, true))
                {
                    foreach (var line in lines)
                        writer.WriteLine(line);
                }
            }
        }

        public string CreateIfNotExists(string FilePath, string FileName, string FileExtension, bool CreateFile = true)
        {
            string PathLocation = NormalizeSeparators(Root + FilePath);
            string FullPath     = NormalizeSeparators(PathLocation + FileName + FileExtension);

            if (!string.IsNullOrEmpty(FilePath) && !System.IO.Directory.Exists(PathLocation))
            {
                var info = System.IO.Directory.CreateDirectory(PathLocation);
            }

            if (!string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(FileName) && !string.IsNullOrEmpty(FileExtension) && !File.Exists(FullPath))
            {
                if (CreateFile)
                {
                    using (StreamWriter writer = new StreamWriter(FullPath)) { };
                }
                else
                {
                    return string.Empty;
                }
            }

            return FullPath;
        }

        /// <summary>
        /// Normaliserar sökvägsseparatorer till plattformens egen — '\' på Windows,
        /// '/' på Linux (t.ex. Raspberry Pi).
        /// </summary>
        /// <remarks>
        /// MÖNSTER: Normaliserings-boundary (Anti-Corruption Layer för filsystemet).
        ///
        /// MOTIVERING:
        /// Kodbasens sökvägskonstanter (PathSprites, DataFile.Settings m.fl.) är skrivna
        /// med Windows-backslash. På Linux är '\' ett giltigt filnamnstecken — INTE en
        /// mappavgränsare — så utan detta blir "root/\Resources\hero.png" ETT enda knasigt
        /// filnamn och all asset-laddning brister. Genom att normalisera vid fil-I/O-gränsen
        /// (denna klass) behöver resten av koden aldrig känna till plattformen. På Windows
        /// är resultatet oförändrat (backslash → backslash), så beteendet där är intakt.
        ///
        /// ANVÄNDNING:
        /// Anropas i CreateIfNotExists på de sammansatta sökvägarna innan de rör disken.
        /// </remarks>
        internal static string NormalizeSeparators(string path)
            => path.Replace('\\', System.IO.Path.DirectorySeparatorChar)
                   .Replace('/',  System.IO.Path.DirectorySeparatorChar);


    }

}
