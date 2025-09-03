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
            try
            {
                string[] files = Directory.GetFiles(path);
                foreach(string file in files)
                {
                    ListViewItem item = new ListViewItem(Path.GetFileName(file));
                    item.Tag = file;
                    item.SubItems.Add(new FileInfo(file).Length.ToString() + " bytes");
                    listView.Items.Add(item);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("There is an Issue with " + ex.Message.ToString());
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
    }
}
