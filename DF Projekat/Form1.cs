using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading.Tasks;


namespace DF_Projekat
{
    public partial class Form1 : Form
    {

        // Add these P/Invoke declarations to your form class
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetFileSize(IntPtr hFile, out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstStreamW(string lpFileName, int InfoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool FindNextStreamW(IntPtr hFindStream, out WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindStream);

        // Add this struct to your form class
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
            public string cStreamName;
        }

        // Constants
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint CREATE_ALWAYS = 2;
        const uint OPEN_EXISTING = 3;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private static readonly IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);


        public Form1()
        {
            InitializeComponent();
            treeView.BeforeExpand += TreeView_BeforeExpand;
            treeView.AfterSelect += TreeView_AfterSelect;
            listView.MouseDoubleClick += ListView_MouseDoubleClick;
            listView.View = View.List;
            StoreRadio.Checked = true;
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView.SelectedItems.Count > 0)
            {
                string filepath = listView.SelectedItems[0].Tag.ToString();

                try
                {
                    // Check if this is an ADS file (contains colon indicating ADS path)
                    if (filepath.Contains(":") && filepath.Split(':').Length >= 3)
                    {
                        // This is an ADS file - extract and open temporarily
                        OpenADSFile(filepath);
                    }
                    else
                    {
                        // Regular file - open directly
                        System.Diagnostics.Process.Start(filepath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There is an Issue with " + ex.Message.ToString());
                }
            }
        }

        // Function to open ADS files by creating temporary files
        private void OpenADSFile(string adsPath)
        {
            try
            {
                Console.WriteLine("Opening ADS file: " + adsPath);

                // Read the ADS data
                byte[] adsData = ReadDataFromADS(adsPath);

                if (adsData == null || adsData.Length == 0)
                {
                    MessageBox.Show("Failed to read ADS data or file is empty!");
                    return;
                }

                // Get the original filename from metadata
                string originalFilename = GetOriginalFilenameFromADS(adsPath);

                // Create temporary file path
                string tempDir = Path.GetTempPath();
                string tempFilePath = Path.Combine(tempDir, originalFilename);

                // If temp file already exists, create a unique name
                if (File.Exists(tempFilePath))
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilename);
                    string extension = Path.GetExtension(originalFilename);
                    int counter = 1;

                    do
                    {
                        tempFilePath = Path.Combine(tempDir, $"{fileNameWithoutExt}_{counter}{extension}");
                        counter++;
                    }
                    while (File.Exists(tempFilePath));
                }

                // Write ADS data to temporary file
                File.WriteAllBytes(tempFilePath, adsData);

                Console.WriteLine("Created temporary file: " + tempFilePath);
                Console.WriteLine("File size: " + adsData.Length + " bytes");

                // Open the temporary file with default application
                System.Diagnostics.Process.Start(tempFilePath);

                // Optional: Clean up temp file after a delay (uncomment if desired)
                // ScheduleTempFileCleanup(tempFilePath);

                Console.WriteLine("Successfully opened ADS file: " + originalFilename);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening ADS file: " + ex.Message);
                Console.WriteLine("Error in OpenADSFile: " + ex.Message);
            }
        }

        // Helper function to get original filename from ADS metadata
        private string GetOriginalFilenameFromADS(string adsPath)
        {
            try
            {
                // Parse the ADS path to get the stream name
                string[] parts = adsPath.Split(':');
                if (parts.Length < 3) return "unknown_file";

                string hostFile = parts[0] + ":" + parts[1]; // Reconstruct path with drive letter
                string streamName = parts[2];

                // Try to read filename metadata
                string metadataAdsPath = hostFile + ":" + streamName + "_filename";
                byte[] filenameData = ReadDataFromADS(metadataAdsPath);

                if (filenameData != null && filenameData.Length > 0)
                {
                    return Encoding.UTF8.GetString(filenameData);
                }
                else
                {
                    // Fallback to stream name if no metadata
                    return streamName;
                }
            }
            catch
            {
                return "unknown_file";
            }
        }

        // Helper to check if file is locked/in use
        private bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                return true; // File is locked
            }
            catch
            {
                return false;
            }
            return false;
        }

        //Nakon sto je odabran folder u treeView
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if(StoreRadio.Checked)
            {
                string path = e.Node.Tag.ToString();
                listView.Items.Clear();
                if (StoreRadio.Checked)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(path);
                        foreach (string file in files)
                        {
                            ListViewItem item = new ListViewItem(Path.GetFileName(file));
                            item.Tag = file;
                            item.SubItems.Add(new FileInfo(file).Length.ToString() + " bytes");
                            listView.Items.Add(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("There is an Issue with " + ex.Message.ToString());
                    }
                }
            }
        }

        private void TreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            TreeNode node = e.Node;
            node.Nodes.Clear();
            try
            {
                string[] directories = Directory.GetDirectories(node.Tag.ToString());
                foreach (string directory in directories)
                {
                    TreeNode subNode = new TreeNode(Path.GetFileName(directory));
                    subNode.Tag = directory;
                    subNode.Nodes.Add("Loading...");
                    node.Nodes.Add(subNode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There is an Issue with " + ex.Message.ToString());
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach(var drive in Directory.GetLogicalDrives())
            {
                TreeNode node = new TreeNode(drive);
                node.Tag = drive;
                node.Nodes.Add("Loading...");
                treeView.Nodes.Add(node);
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void ReadRadio_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void treeView_AfterSelect_1(object sender, TreeViewEventArgs e)
        {

        }

        async private void SaveButton_Click(object sender, EventArgs e)
        {
            // Function to copy selected file to ADS with indexed keys
                try
                {
                    // Check if StoreRadio is selected
                    if (!StoreRadio.Checked)
                    {
                        Console.WriteLine("ERROR: StoreRadio must be selected to store files in ADS!");
                        MessageBox.Show("Please select 'Store' mode to save files to ADS.");
                        return;
                    }

                    // Check if ListView has a selected item
                    if (listView.SelectedItems.Count == 0)
                    {
                        Console.WriteLine("ERROR: No file/folder selected in ListView!");
                        return;
                    }

                    // Get selected path from ListView
                    string selectedPath = listView.SelectedItems[0].Tag.ToString();

                    // Check if selected item is a file (not a directory)
                    if (!File.Exists(selectedPath))
                    {
                        Console.WriteLine("ERROR: Selected item is not a file or doesn't exist!");
                        return;
                    }

                    // Get base ADS name from the text input
                    string baseAdsName = StreamKeyValue.Text.Trim();
                    if (string.IsNullOrWhiteSpace(baseAdsName))
                    {
                        Console.WriteLine("ERROR: No ADS name provided!");
                        return;
                    }

                    // Get the directory where the selected file is located
                    string fileDirectory = Path.GetDirectoryName(selectedPath);
                    string hostFile = Path.Combine(fileDirectory, "root.txt");
                    string originalFileName = Path.GetFileName(selectedPath);

                    Console.WriteLine("Starting file copy to ADS...");
                    StatusLabel.Text = "Starting file copy to ADS...";
                    await Task.Delay(300);
                    Console.WriteLine("Selected file: " + selectedPath);
                    Console.WriteLine("Original filename: " + originalFileName);
                    Console.WriteLine("Host file: " + hostFile);
                    Console.WriteLine("Base ADS name: " + baseAdsName);

                    // Check if host file exists, create if not
                    if (!File.Exists(hostFile))
                    {
                        File.WriteAllText(hostFile, "");
                        Console.WriteLine("Created host file: " + hostFile);
                        StatusLabel.Text = "Created host file: " + hostFile;
                        await Task.Delay(1000);
                    }

                    // Find next available indexed key
                    string finalAdsName = FindNextAvailableKey(hostFile, baseAdsName);
                    Console.WriteLine("Using ADS key: " + finalAdsName);

                    // Read the selected file into byte array
                    byte[] fileData = File.ReadAllBytes(selectedPath);
                    Console.WriteLine("Read " + fileData.Length + " bytes from source file");

                    // Save the actual file data to the main ADS
                    string adsPath = hostFile + ":" + finalAdsName;
                    if (!SaveDataToADS(adsPath, fileData))
                    {
                        return;
                    }

                    // Save the original filename as metadata in a separate ADS
                    string metadataAdsPath = hostFile + ":" + finalAdsName + "_filename";
                    byte[] filenameData = Encoding.UTF8.GetBytes(originalFileName);
                    if (!SaveDataToADS(metadataAdsPath, filenameData))
                    {
                        Console.WriteLine("WARNING: Could not save filename metadata");
                    }

                    Console.WriteLine("File successfully copied to ADS with filename metadata!");
                    Console.WriteLine("SUCCESS: " + originalFileName + " stored with key: " + finalAdsName);
                
                StatusLabel.Text = "SUCCESS: " + originalFileName;
                await Task.Delay(1000);
                StatusLabel.Text = "Stored with key: " + finalAdsName;
                await Task.Delay(1000);
                StatusLabel.Text = fileData.Length + " bytes";
                await Task.Delay(1000);
                StatusLabel.Text = "READY";


                // Delete original file if deleteCheckBox is checked
                if (deleteCheckBox.Checked)
                    {
                        try
                        {
                            File.Delete(selectedPath);
                            Console.WriteLine("Original file deleted: " + selectedPath);

                            // Refresh the ListView to reflect the deletion
                            if (StoreRadio.Checked && treeView.SelectedNode != null)
                            {
                                TreeView_AfterSelect(treeView, new TreeViewEventArgs(treeView.SelectedNode, TreeViewAction.Unknown));
                            }
                        }
                        catch (Exception deleteEx)
                        {
                            Console.WriteLine("ERROR: Could not delete original file: " + deleteEx.Message);
                            MessageBox.Show("File stored in ADS successfully, but could not delete original file: " + deleteEx.Message);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                }
            }

        private string FindNextAvailableKey(string hostFile, string baseKey)
        {
            // First try the base key without index
            if (!ADSExists(hostFile, baseKey))
            {
                return baseKey;
            }

            // If base key exists, try indexed versions
            int index = 1;
            string indexedKey;

            do
            {
                indexedKey = baseKey + "_" + index;
                index++;
            }
            while (ADSExists(hostFile, indexedKey));

            return indexedKey;
        }

        private bool SaveDataToADS(string adsPath, byte[] data)
        {
            IntPtr hFile = CreateFile(adsPath, GENERIC_WRITE, 0, IntPtr.Zero, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (hFile == INVALID_HANDLE_VALUE)
            {
                int error = Marshal.GetLastWin32Error();
                Console.WriteLine("ERROR: CreateFile failed with error code: " + error);
                StatusLabel.Text = "Failed to create ADS. Error: " + error;
                return false;
            }

            try
            {
                uint bytesWritten;
                bool writeResult = WriteFile(hFile, data, (uint)data.Length, out bytesWritten, IntPtr.Zero);

                if (!writeResult || bytesWritten != data.Length)
                {
                    Console.WriteLine("ERROR: WriteFile failed");
                    StatusLabel.Text = "Failed to write data to ADS";
                    return false;
                }

                return true;
            }
            finally
            {
                CloseHandle(hFile);
            }
        }

        private byte[] ReadDataFromADS(string adsPath)
        {
            IntPtr hFile = CreateFile(adsPath, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (hFile == INVALID_HANDLE_VALUE)
            {
                return null;
            }

            try
            {
                uint fileSizeHigh;
                uint fileSize = GetFileSize(hFile, out fileSizeHigh);

                if (fileSize == 0)
                {
                    return new byte[0];
                }

                byte[] buffer = new byte[fileSize];
                uint bytesRead;

                bool readResult = ReadFile(hFile, buffer, fileSize, out bytesRead, IntPtr.Zero);
                if (!readResult)
                {
                    return null;
                }

                return buffer;
            }
            finally
            {
                CloseHandle(hFile);
            }
        }

        private void ShowADSFiles()
        {
            try
            {
                // Check if TreeView has a selected directory
                if (treeView.SelectedNode == null)
                {
                    Console.WriteLine("ERROR: No directory selected!");
                    return;
                }

                string selectedPath = treeView.SelectedNode.Tag.ToString();
                listView.Items.Clear();

                Console.WriteLine("Searching for ADS files in directory: " + selectedPath);

                // Look for root.txt in the selected directory
                string rootFile = Path.Combine(selectedPath, "root.txt");

                if (!File.Exists(rootFile))
                {
                    Console.WriteLine("ERROR: No root.txt found in selected directory");
                    return;
                }

                // Get all ADS streams from root.txt
                List<string> adsStreams = GetAllADSStreams(rootFile);

                if (adsStreams.Count == 0)
                {
                    Console.WriteLine("No ADS streams found in root.txt");
                    return;
                }

                // Get filter key from text input (optional)
                string filterKey = StreamKeyValue.Text.Trim();
                bool useFilter = !string.IsNullOrWhiteSpace(filterKey);

                Console.WriteLine("Found " + adsStreams.Count + " ADS streams");
                if (useFilter)
                {
                    Console.WriteLine("Filtering by base key: " + filterKey);
                }

                int foundCount = 0;
                foreach (string streamName in adsStreams)
                {
                    // Skip filename metadata streams
                    if (streamName.EndsWith("_filename"))
                        continue;

                    // Apply filter if specified (check both exact match and base key match)
                    if (useFilter)
                    {
                        bool matches = streamName.Equals(filterKey, StringComparison.OrdinalIgnoreCase) ||
                                      streamName.StartsWith(filterKey + "_", StringComparison.OrdinalIgnoreCase);
                        if (!matches)
                            continue;
                    }

                    // Get file size
                    long adsSize = GetADSSize(rootFile, streamName);

                    // Try to get the original filename from metadata
                    string originalFilename = streamName; // Default to stream name
                    string metadataAdsPath = rootFile + ":" + streamName + "_filename";
                    byte[] filenameData = ReadDataFromADS(metadataAdsPath);

                    if (filenameData != null && filenameData.Length > 0)
                    {
                        originalFilename = Encoding.UTF8.GetString(filenameData);
                        Console.WriteLine("Stream '" + streamName + "' -> Original file: " + originalFilename);
                    }
                    else
                    {
                        Console.WriteLine("Stream '" + streamName + "' -> No filename metadata, using stream name");
                    }

                    // Create ListView item with original filename
                    ListViewItem item = new ListViewItem(originalFilename);
                    item.Tag = rootFile + ":" + streamName;  // Store full ADS path

                    if (adsSize > 0)
                    {
                        item.SubItems.Add(adsSize.ToString() + " bytes");
                    }
                    else
                    {
                        item.SubItems.Add("Unknown size");
                    }

                    // Show ADS key for identification
                    item.SubItems.Add("Key: " + streamName);

                    listView.Items.Add(item);
                    foundCount++;
                }

                Console.WriteLine("SUCCESS: Displayed " + foundCount + " ADS files");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ShowADSFiles: " + ex.Message);
            }
        }

        private void ReadButton_Click(object sender, EventArgs e)
        {
            if (OpenRadio.Checked)
            {
                ShowADSFiles();
            }
        }

        private bool ADSExists(string hostFilePath, string adsName)
        {
            try
            {
                string adsPath = hostFilePath + ":" + adsName;
                IntPtr hFile = CreateFile(adsPath, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                if (hFile == INVALID_HANDLE_VALUE)
                {
                    return false;
                }

                CloseHandle(hFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GetAllADSStreams(string filePath)
        {
            List<string> streams = new List<string>();

            try
            {
                WIN32_FIND_STREAM_DATA findData;
                IntPtr findHandle = FindFirstStreamW(filePath, 0, out findData, 0);

                if (findHandle == IntPtr.Zero || findHandle.ToInt64() == -1)
                {
                    Console.WriteLine("No streams found or error occurred");
                    return streams;
                }

                try
                {
                    do
                    {
                        string streamName = findData.cStreamName;
                        Console.WriteLine("Raw stream found: " + streamName);

                        if (streamName.StartsWith(":") && streamName.EndsWith(":$DATA"))
                        {
                            // Remove the leading : and trailing :$DATA
                            streamName = streamName.Substring(1, streamName.Length - 7);
                            if (streamName != "") // Skip the main data stream
                            {
                                streams.Add(streamName);
                                Console.WriteLine("Added stream: " + streamName);
                            }
                        }
                    }
                    while (FindNextStreamW(findHandle, out findData));
                }
                finally
                {
                    FindClose(findHandle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enumerating streams: " + ex.Message);
            }

            return streams;
        }
        private long GetADSSize(string hostFilePath, string adsName)
        {
            try
            {
                string adsPath = hostFilePath + ":" + adsName;
                IntPtr hFile = CreateFile(adsPath, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

                if (hFile == INVALID_HANDLE_VALUE)
                {
                    return -1;
                }

                try
                {
                    uint fileSizeHigh;
                    uint fileSize = GetFileSize(hFile, out fileSizeHigh);
                    return (long)fileSize + ((long)fileSizeHigh << 32);
                }
                finally
                {
                    CloseHandle(hFile);
                }
            }
            catch
            {
                return -1;
            }
        }

        async private void deleteButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if OpenRadio is selected (ADS viewing mode)
                if (!OpenRadio.Checked)
                {
                    Console.WriteLine("ERROR: OpenRadio must be selected to delete ADS files!");
                    MessageBox.Show("Please select 'Open' mode to delete ADS files.");
                    return;
                }

                // Check if ListView has a selected item
                if (listView.SelectedItems.Count == 0)
                {
                    Console.WriteLine("ERROR: No ADS file selected in ListView!");
                    MessageBox.Show("Please select an ADS file to delete.");
                    return;
                }

                // Get the selected ADS path from ListView
                string adsPath = listView.SelectedItems[0].Tag.ToString();

                // Verify this is an ADS path (should contain colons)
                if (!adsPath.Contains(":") || adsPath.Split(':').Length < 3)
                {
                    Console.WriteLine("ERROR: Selected item is not an ADS file!");
                    MessageBox.Show("Selected item is not an ADS file.");
                    return;
                }

                // Get the original filename for confirmation dialog
                string originalFilename = GetOriginalFilenameFromADS(adsPath);

                // Ask for confirmation before deleting
                DialogResult confirmResult = MessageBox.Show(
                    $"Are you sure you want to delete the ADS file:\n\n{originalFilename}\n\nThis action cannot be undone.",
                    "Confirm ADS File Deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (confirmResult != DialogResult.Yes)
                {
                    Console.WriteLine("ADS file deletion cancelled by user");
                    return;
                }

                Console.WriteLine("Starting ADS file deletion...");
                Console.WriteLine("ADS path: " + adsPath);
                Console.WriteLine("Original filename: " + originalFilename);

                // Parse the ADS path to get components
                string[] parts = adsPath.Split(':');
                string hostFile = parts[0] + ":" + parts[1]; // Reconstruct path with drive letter
                string streamName = parts[2];

                // Delete the main ADS stream
                bool mainDeleted = DeleteADSStream(hostFile, streamName);

                // Delete the filename metadata stream
                bool metadataDeleted = DeleteADSStream(hostFile, streamName + "_filename");

                if (mainDeleted)
                {
                    Console.WriteLine("SUCCESS: ADS file deleted - " + originalFilename);
                    StatusLabel.Text = "Deleted";
                    await Task.Delay(1000);
                    StatusLabel.Text = "READY";
                    MessageBox.Show($"ADS file '{originalFilename}' has been successfully deleted.");

                    // Refresh the ListView to show the deletion
                    ShowADSFiles();
                }
                else
                {
                    Console.WriteLine("ERROR: Failed to delete main ADS stream");
                    MessageBox.Show("Failed to delete the ADS file. It may be in use or access was denied.");
                }

                // Log metadata deletion result
                if (!metadataDeleted)
                {
                    Console.WriteLine("WARNING: Could not delete filename metadata stream");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR in DeleteSelectedADSFile: " + ex.Message);
                MessageBox.Show("Error deleting ADS file: " + ex.Message);
            }
        }

        // Helper function to delete an ADS stream
        private bool DeleteADSStream(string hostFile, string streamName)
        {
            try
            {
                string adsPath = hostFile + ":" + streamName;
                Console.WriteLine("Attempting to delete ADS stream: " + adsPath);

                // Open the ADS stream and truncate it to 0 bytes (effectively deleting it)
                IntPtr hFile = CreateFile(
                    adsPath,
                    GENERIC_WRITE,
                    0,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hFile == INVALID_HANDLE_VALUE)
                {
                    int error = Marshal.GetLastWin32Error();
                    Console.WriteLine("ERROR: Could not open ADS stream for deletion. Error code: " + error);
                    return false;
                }

                try
                {
                    // Set file pointer to beginning and set end of file (truncate to 0)
                    if (SetEndOfFile(hFile))
                    {
                        Console.WriteLine("Successfully deleted ADS stream: " + streamName);
                        return true;
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        Console.WriteLine("ERROR: SetEndOfFile failed. Error code: " + error);
                        return false;
                    }
                }
                finally
                {
                    CloseHandle(hFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in DeleteADSStream: " + ex.Message);
                return false;
            }
        }

        // Add this P/Invoke declaration to your existing P/Invoke section
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetEndOfFile(IntPtr hFile);

        private void restoreButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Proveri da li je u "Open" modu
                if (!OpenRadio.Checked)
                {
                    MessageBox.Show("Please select 'Open' mode to restore ADS files.");
                    return;
                }

                // Proveri da li je odabran fajl u ListView
                if (listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select an ADS file to restore.");
                    return;
                }

                string adsPath = listView.SelectedItems[0].Tag.ToString();

                // Proveri da li je ovo uopšte ADS path
                if (!adsPath.Contains(":") || adsPath.Split(':').Length < 3)
                {
                    MessageBox.Show("Selected item is not an ADS file.");
                    return;
                }

                // Učitaj ADS podatke
                byte[] adsData = ReadDataFromADS(adsPath);
                if (adsData == null || adsData.Length == 0)
                {
                    MessageBox.Show("Failed to read ADS data or stream is empty!");
                    return;
                }

                // Odredi originalni filename iz metadata ADS-a
                string originalFilename = GetOriginalFilenameFromADS(adsPath);
                if (string.IsNullOrWhiteSpace(originalFilename))
                    originalFilename = "restored_file";

                // Odredi folder u kom se nalazi host fajl (root.txt)
                string[] parts = adsPath.Split(':');
                string hostFile = parts[0] + ":" + parts[1]; // putanja do root.txt
                string hostDir = Path.GetDirectoryName(hostFile);

                // Konačna putanja gde će biti vraćen fajl
                string restorePath = Path.Combine(hostDir, originalFilename);

                // Ako već postoji fajl sa tim imenom, napravi unikatan naziv
                int counter = 1;
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFilename);
                string extension = Path.GetExtension(originalFilename);
                while (File.Exists(restorePath))
                {
                    restorePath = Path.Combine(hostDir, $"{fileNameWithoutExt}_{counter}{extension}");
                    counter++;
                }

                // Zapiši fajl na disk
                File.WriteAllBytes(restorePath, adsData);

                MessageBox.Show($"File restored successfully:\n{restorePath}");
                Console.WriteLine("Restored ADS file to: " + restorePath);

                // Opciono: osveži prikaz foldera
                if (treeView.SelectedNode != null)
                {
                    TreeView_AfterSelect(treeView, new TreeViewEventArgs(treeView.SelectedNode, TreeViewAction.Unknown));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error restoring file: " + ex.Message);
                Console.WriteLine("Error in restoreButton_Click: " + ex.Message);
            }
        }
    }
}
