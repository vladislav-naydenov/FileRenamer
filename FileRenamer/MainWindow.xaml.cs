using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace FileRenamer
{
    [SuppressMessage("ReSharper", "RedundantExtendsListEntry")]
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<string> fileNames;
        private const string templateRegex = "S(\\d+){0}(\\d+)";
        private const string renameReport = "RenameReport.txt";

        public string CurrentPath { get; set; }

        public MainWindow()
        {
            fileNames = new ObservableCollection<string>();
            InitializeComponent();
            this.DataContext = this;
            this.RegexString.Text = "E";
        }

        private void OnDirectoryPathChanged(object sender, TextChangedEventArgs e)
        {
            fileNames.Clear();

            var newPath = ((TextBox)e.Source).Text;
            var oldPath = ((TextBox)e.OriginalSource).Text;

            if (string.IsNullOrEmpty(newPath))
            {
                return;
            }

            PopulateFileNames(newPath);
            this.Rename.IsEnabled = true;
            this.CurrentPath = newPath;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public ObservableCollection<string> FileNames => this.fileNames;

        private void OnRename(object sender, RoutedEventArgs e)
        {
            var directoryInfo = new DirectoryInfo(DirectoryPath.Text);
            var fileInfos = directoryInfo.GetFiles();
            var subtitles = new List<FileInfo>();
            var videos = new List<FileInfo>();
            
            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.Extension.Equals(".srt", StringComparison.Ordinal))
                {
                    subtitles.Add(fileInfo);
                }
                else
                {
                    videos.Add(fileInfo);
                }
            }

            var regex = new Regex(string.Format(templateRegex, RegexString.Text), RegexOptions.IgnoreCase);
            RenameFiles(subtitles, regex, true);
            RenameFiles(videos, regex, false);

            fileNames.Clear();
            PopulateFileNames(DirectoryPath.Text);

            this.Rename.Content = "Done";
            this.Rename.IsEnabled = false;
        }

        private void RenameFiles(IEnumerable<FileInfo> fileInfos, Regex seasonEpisodeRegex, bool isSubtitles)
        {
            var subtitlesLang = isSubtitles ? ".bg" : "";

            foreach (var file in fileInfos)
            {
                var match = seasonEpisodeRegex.Match(file.Name);

                if (!match.Success)
                {
                    continue;
                }

                var season = int.Parse(match.Groups[match.Groups.Count - 2].Value);
                var episode = match.Groups[match.Groups.Count - 1].Value;
                var newFileFullName =
                    file.FullName.Replace(file.Name, $"{NewName.Text} {season}{episode}{subtitlesLang}{file.Extension}");
                var newFileName = newFileFullName.Split(new[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();
                try
                {
                    File.Move(file.FullName, newFileFullName);
                }
                catch (IOException e)
                {
                    MessageBox.Show($"Rename of file {file.Name} failed due to an error: {e.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                WriteToReportFile(file.DirectoryName, file.Name, newFileName);
            }
        }

        private void WriteToReportFile(string path, string oldFileName, string newFilename)
        {
            File.AppendAllText($"{path}\\{renameReport}", $@"{oldFileName} -> {newFilename}{Environment.NewLine}");
        }

        private void RollbackNames(object sender, RoutedEventArgs e)
        {
            var renameReportPath = $"{this.CurrentPath}\\{renameReport}";

            if (!File.Exists(renameReportPath))
            {
                MessageBox.Show($"Couldn't find file {renameReportPath}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            var renameInfo = File.ReadAllLines(renameReportPath);

            foreach (var line in renameInfo)
            {
                var renameData = line.Split(new [] { "->" }, StringSplitOptions.None);
                
                var originalName = renameData[0].Trim();
                var currentName = renameData[1].Trim();

                File.Move($"{this.CurrentPath}\\{currentName}", $"{this.CurrentPath}\\{originalName}");
            }
        }

        private void PopulateFileNames(string path)
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                fileNames.Add(file);
            }
        }
    }
}
