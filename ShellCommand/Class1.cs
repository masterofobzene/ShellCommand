using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SharpShell;
using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System.Runtime.InteropServices;

namespace ShellCommandContextMenu
{
    // Support both file/folder and directory background invocations.
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.AllFilesAndFolders)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    public class ShellCommandContextMenu : SharpContextMenu
    {
        private Size _currentIconSize;
        // Allowed script file extensions for commands (".py" has been removed).
        private static readonly HashSet<string> AllowedScriptExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1", ".bat", ".cmd"
        };

        // This field holds the active extension folder that matches the current selection.
        private string _activeExtensionFolder = null;

        /// <summary>
        /// Determines if the context menu should be shown.
        /// It distinguishes between two cases:
        ///   1. Background context: when no items are selected (empty right-click space);
        ///      uses the special allowed keyword "background".
        ///   2. Normal context: files and/or folders have been selected,
        ///      with files evaluated by file extension and folders by the keyword "folder".
        /// For either case, exactly one extension group (folder) must match.
        /// </summary>
        protected override bool CanShowMenu()
        {
            var shellCommandPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ShellCommand"
            );

            if (!Directory.Exists(shellCommandPath))
                return false;

            var matchingExtensionFolders = new List<string>();

            // Retrieve the selected items.
            var selectedItems = SelectedItemPaths;
            if (selectedItems == null || !selectedItems.Any())
            {
                // Background context: no items selected.
                // Look only for extension groups that allow the keyword "background".
                foreach (var extFolder in Directory.GetDirectories(shellCommandPath))
                {
                    var folderName = Path.GetFileName(extFolder);
                    if (folderName.StartsWith("[") && folderName.EndsWith("]"))
                    {
                        // Parse the allowed keywords (extensions).
                        var innerText = folderName.Substring(1, folderName.Length - 2);
                        var allowedKeys = innerText
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToHashSet();

                        if (allowedKeys.Contains("background"))
                            matchingExtensionFolders.Add(extFolder);
                    }
                }
            }
            else
            {
                // Normal context: iterate over each candidate extension folder and ensure
                // every selected item's "extension" keyword is contained in that group.
                foreach (var extFolder in Directory.GetDirectories(shellCommandPath))
                {
                    var folderName = Path.GetFileName(extFolder);
                    if (folderName.StartsWith("[") && folderName.EndsWith("]"))
                    {
                        var innerText = folderName.Substring(1, folderName.Length - 2);
                        var allowedKeys = innerText
                            .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToHashSet();

                        bool allMatch = true;
                        foreach (var path in selectedItems)
                        {
                            string key = string.Empty;
                            if (File.Exists(path))
                            {
                                // For files, use the extension without the dot.
                                key = Path.GetExtension(path).TrimStart('.').ToLower();
                            }
                            else if (Directory.Exists(path))
                            {
                                // For folders, use the keyword "folder".
                                key = "folder";
                            }
                            else
                            {
                                continue;
                            }

                            if (!allowedKeys.Contains(key))
                            {
                                allMatch = false;
                                break;
                            }
                        }

                        if (allMatch)
                            matchingExtensionFolders.Add(extFolder);
                    }
                }
            }

            // Only show the menu if exactly one matching extension group qualifies.
            if (matchingExtensionFolders.Count == 1)
            {
                _activeExtensionFolder = matchingExtensionFolders.First();
                return true;
            }
            else
            {
                _activeExtensionFolder = null;
                return false;
            }
        }

        /// <summary>
        /// Builds the context menu using the contents of the active extension folder.
        /// </summary>
        protected override ContextMenuStrip CreateMenu()
        {
            CalculateIconSize();
            var menu = new ContextMenuStrip();
            menu.ImageScalingSize = _currentIconSize;

            if (!string.IsNullOrEmpty(_activeExtensionFolder) && Directory.Exists(_activeExtensionFolder))
            {
                AddDirectoryItems(menu.Items, _activeExtensionFolder);
            }
            return menu;
        }

        /// <summary>
        /// Calculates the icon size based on the current DPI.
        /// </summary>
        private void CalculateIconSize()
        {
            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiScale = g.DpiX / 96f;
                int baseSize = 32;
                int scaledSize = (int)(baseSize * dpiScale);
                _currentIconSize = new Size(
                    Math.Min(scaledSize, 32),
                    Math.Min(scaledSize, 32)
                );
            }
        }

        /// <summary>
        /// Recursively adds subdirectories and script files (excluding .py)
        /// from the specified path into the parent menu.
        /// </summary>
        private void AddDirectoryItems(ToolStripItemCollection parentItems, string currentPath)
        {
            try
            {
                // Process subdirectories.
                foreach (var directory in Directory.GetDirectories(currentPath))
                {
                    var directoryName = Path.GetFileName(directory);
                    var directoryItem = new ToolStripMenuItem(directoryName)
                    {
                        ImageScaling = ToolStripItemImageScaling.SizeToFit
                    };
                    SetMenuItemIcon(directoryItem, GetIconPath(directory, true));
                    AddDirectoryItems(directoryItem.DropDownItems, directory);
                    parentItems.Add(directoryItem);
                }

                // Process script files.
                foreach (var file in Directory.GetFiles(currentPath))
                {
                    var extension = Path.GetExtension(file);
                    if (!AllowedScriptExtensions.Contains(extension) ||
                        extension.Equals(".ico", StringComparison.OrdinalIgnoreCase))
                        continue;
                    // Use the file name without its extension.
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileItem = new ToolStripMenuItem(fileName)
                    {
                        ImageScaling = ToolStripItemImageScaling.SizeToFit
                    };
                    SetMenuItemIcon(fileItem, GetIconPath(file, false));
                    fileItem.Click += (sender, e) => ExecuteFile(file);
                    parentItems.Add(fileItem);
                }
            }
            catch
            {
                // Optionally add error logging or handling.
            }
        }

        /// <summary>
        /// Gets the icon path for a given file or directory.
        /// </summary>
        private string GetIconPath(string path, bool isDirectory)
        {
            var baseName = isDirectory
                ? Path.GetFileName(path)
                : Path.GetFileNameWithoutExtension(path);
            return Path.Combine(
                Path.GetDirectoryName(path),
                $"{baseName}.ico"
            );
        }

        /// <summary>
        /// Sets the icon for a menu item if the corresponding .ico file exists.
        /// </summary>
        private void SetMenuItemIcon(ToolStripMenuItem item, string iconPath)
        {
            try
            {
                if (File.Exists(iconPath))
                {
                    using (var icon = new Icon(iconPath, _currentIconSize))
                    {
                        item.Image = new Bitmap(icon.ToBitmap(), _currentIconSize);
                    }
                }
            }
            catch
            {
                // Handle missing or invalid icons gracefully.
            }
        }

        /// <summary>
        /// Executes the selected script file, passing all selected paths as arguments.
        /// The script's window will be shown.
        /// </summary>
        private void ExecuteFile(string scriptPath)
        {
            try
            {
                var argsBuilder = new StringBuilder();
                foreach (var path in SelectedItemPaths)
                {
                    if (argsBuilder.Length > 0)
                        argsBuilder.Append(' ');
                    argsBuilder.Append($"\"{path}\"");
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = argsBuilder.ToString(),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    CreateNoWindow = false
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing script: {ex.Message}",
                    "Script Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
