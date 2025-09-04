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

namespace DF_Projekat
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            treeView.BeforeExpand += TreeView_BeforeExpand;
            treeView.AfterSelect += TreeView_AfterSelect;
            listView.MouseDoubleClick += ListView_MouseDoubleClick;
            listView.View = View.List;
            StoreRadio.Checked = true;
        }
        //Nakon sto je dva puta kliknut file u listView
        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(listView.SelectedItems.Count>0)
            {
                string filepath = listView.SelectedItems[0].Tag.ToString();
                try
                {
                    System.Diagnostics.Process.Start(filepath);
                }
                catch(Exception ex)
                {
                    MessageBox.Show("There is an Issue with " + ex.Message.ToString());
                }
            }
        }

        //Nakon sto je odabran folder u treeView
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = e.Node.Tag.ToString();
            listView.Items.Clear();
            if(StoreRadio.Checked)
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

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if(StoreRadio.Checked)
            {
                // Check if something is selected in TreeView
                if (treeView.SelectedNode == null)
                {
                    StatusLabel.Text = "No folder selected!";
                    return;
                }

                // Get selected folder path
                string selectedPath = treeView.SelectedNode.Tag.ToString();

                // Define root.txt path
                string rootFilePath = Path.Combine(selectedPath, "root.txt");

                try
                {
                    if (File.Exists(rootFilePath))
                    {
                        // File already exists
                        StatusLabel.Text = "Exists";
                    }
                    else
                    {
                        // Create the file
                        using (FileStream fs = File.Create(rootFilePath))
                        {
                            // Just creating an empty file is enough
                        }

                        StatusLabel.Text = "Created Root";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }
    }
}
