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
            //Fuckar up och ger tillbaka min path på andras maskiner.. wtf
            Root = System.IO.Path.Combine(Environment.CurrentDirectory);
            
            //
            var BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
           
            return BaseDirectory;
        }


        public T ReadJson<T>(string FilePath, string FileName, string FileExtension, bool CreateFile = true)
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
            string PathLocation = Root + FilePath;

            // Check if dir Exists
            if (!string.IsNullOrEmpty(FilePath) && !System.IO.Directory.Exists(PathLocation))
            {
                // create dir 
                var info = System.IO.Directory.CreateDirectory(PathLocation);
            }

            // Check if file Exists
            if (!string.IsNullOrEmpty(FilePath) && !string.IsNullOrEmpty(FileName) && !string.IsNullOrEmpty(FileExtension) && !File.Exists(PathLocation + FileName + FileExtension))
            {
                if (CreateFile)
                {
                    // create file 
                    using (StreamWriter writer = new StreamWriter(PathLocation + FileName + FileExtension)) { };
                }
                else
                {
                    return string.Empty;
                }
            }

            // Return full Directory. 
            return PathLocation + FileName + FileExtension;
        }


    }

}
