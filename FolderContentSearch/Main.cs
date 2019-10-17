using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace FolderContentSearch
{
    public partial class FrmMain : Form
    {
        #region ::  Variable & Properties  ::

        private readonly ToolStripMenuItem _mnNotepadOpen;
        private readonly ToolStripMenuItem _mnExpand;
        private readonly ToolStripMenuItem _mnCollapse;
        private readonly ToolStripMenuItem _mnExpandAll;
        private readonly ToolStripMenuItem _mnFolderOpen;

        private string textFileExtensions = "*.txt, *.config, ";
        private string codeFileExtensions = "*.cs";

        private bool _forceClose = false;

        private bool isInProgress = false;

        #endregion

        #region ::  Constructor  ::

        public FrmMain()
        {
            InitializeComponent();

            _mnNotepadOpen = new ToolStripMenuItem
            {
                Name = "mnNotepadOpen",
                Size = new System.Drawing.Size(181, 22),
                Text = "Open in Notepad++"
            };

            _mnExpand = new ToolStripMenuItem
            {
                Name = "mnExpand",
                Size = new System.Drawing.Size(180, 22),
                Text = "Expand children"
            };

            _mnExpandAll = new ToolStripMenuItem
            {
                Name = "mnExpandAll",
                Size = new System.Drawing.Size(180, 22),
                Text = "Expand All"
            };

            _mnCollapse = new ToolStripMenuItem
            {
                Name = "mnCollapse",
                Size = new System.Drawing.Size(180, 22),
                Text = "Collapse children"
            };

            _mnFolderOpen = new ToolStripMenuItem
            {
                Name = "mnFolderOpen",
                Size = new System.Drawing.Size(180, 22),
                Text = "Explore"
            };

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _mnFolderOpen,
                _mnExpandAll,
                _mnExpand,
                _mnCollapse,
                _mnNotepadOpen
            });
        }

        #endregion

        #region ::  Methods   ::

        public void StartSearch(string dirPath)
        {
            var selectedFileFilter = "";
            cmbFileTypeFilter.PerformSafely(() => selectedFileFilter = cmbFileTypeFilter.Text);

            string[] files;

            if (selectedFileFilter == "Only CS Files")
                files = Directory.GetFiles(dirPath, codeFileExtensions, SearchOption.AllDirectories);
            else if (selectedFileFilter == "Only Text Files")
                files = Directory.GetFiles(dirPath, textFileExtensions, SearchOption.AllDirectories);
            else
                files = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);

            files = files.Where(file => Path.GetExtension(file) != ".dll").Select(f => f).ToArray();

            progressBar.PerformSafely(() =>
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = files.Length;
                progressBar.Value = 0;
            });

            for (var i = 0; i < files.Length; i++)
            {
                if (_forceClose || !isInProgress)
                {
                    Thread.CurrentThread.Abort();
                }

                try
                {
                    if (Path.GetExtension(files[i]) == ".dll")
                        continue;

                    UpdateProgress(i);

                    string fileContent = File.ReadAllText(files[i]);
                    if (fileContent.ToLower().Contains(txtTextToSearch.Text.ToLower()))
                    {
                        PopulateTreeView(listFiles, files[i], '\\', contextMenu);
                    }
                }
                catch (IOException)
                {
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void UpdateProgress(int value)
        {
            progressBar.PerformSafely(() => progressBar.Value = value);
        }

        private void ChangeControlState(bool state)
        {
            isInProgress = !state;
            btnStartSearch.PerformSafely(() => btnStartSearch.Text = !state ? "Stop" : "Start");
            progressBar.PerformSafely(() => progressBar.Visible = !state);
        }

        private void PopulateTreeView(TreeView treeView, string path, char pathSeparator, ContextMenuStrip contextMenu)
        {
            TreeNode lastNode = null;
            var subPathAgg = string.Empty;

            foreach (string subPath in path.Split(pathSeparator))
            {
                subPathAgg += subPath + pathSeparator;
                TreeNode[] nodes = null;
                treeView.PerformSafely(() => nodes = treeView.Nodes.Find(subPathAgg, true));
                if (nodes.Length == 0)
                {
                    if (lastNode == null)
                        treeView.PerformSafely(() => lastNode = treeView.Nodes.Add(subPathAgg, subPath));
                    else
                        treeView.PerformSafely(() => lastNode = lastNode.Nodes.Add(subPathAgg, subPath));
                }
                else
                    lastNode = nodes[0];
            }
        }

        #endregion

        #region ::  Control Events   ::

        private void btnStartSearch_Click(object sender, EventArgs e)
        {
            if (isInProgress)
            {
                ChangeControlState(true);
                return;
            }

            // Validations
            string selectedPath = txtFolderPath.Text;

            if (!Directory.Exists(selectedPath))
            {
                MessageBox.Show("Invalid folder path");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTextToSearch.Text))
            {
                MessageBox.Show("Invalid search text");
                return;
            }

            var threadStart = new ThreadStart(delegate
            {
                try
                {
                    ChangeControlState(false);
                    StartSearch(selectedPath);
                }
                finally
                {
                    ChangeControlState(true);
                }
            });

            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            string copiedPath = Clipboard.GetText();

            if (Directory.Exists(copiedPath))
                txtFolderPath.Text = copiedPath;
        }

        private void contextMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Name == "mnNotepadOpen")
            {
                Process.Start("notepad++", listFiles.SelectedNode.FullPath);
            }
            else if (e.ClickedItem.Name == "mnExpand")
            {
                listFiles.SelectedNode.Expand();
            }
            else if (e.ClickedItem.Name == "mnExpandAll")
            {
                listFiles.SelectedNode.ExpandAll();
            }
            else if (e.ClickedItem.Name == "mnCollapse")
            {
                listFiles.SelectedNode.Collapse();
            }
            else if (e.ClickedItem.Name == "mnFolderOpen")
            {
                Process.Start("explorer.exe", listFiles.SelectedNode.FullPath);
            }
        }

        private void listFiles_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
            }
        }

        private void listFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Select the clicked node
                listFiles.SelectedNode = listFiles.GetNodeAt(e.X, e.Y);

                if (listFiles.SelectedNode != null)
                {
                    _mnNotepadOpen.Visible = listFiles.SelectedNode.GetNodeCount(true) == 0;
                    _mnFolderOpen.Visible = listFiles.SelectedNode.GetNodeCount(true) != 0;

                    _mnExpand.Visible = listFiles.SelectedNode.GetNodeCount(true) != 0 &&
                                        !listFiles.SelectedNode.IsExpanded;
                    _mnExpandAll.Visible = listFiles.SelectedNode.GetNodeCount(true) != 0 &&
                                           !listFiles.SelectedNode.IsExpanded;
                    _mnCollapse.Visible = listFiles.SelectedNode.GetNodeCount(true) != 0 &&
                                          listFiles.SelectedNode.IsExpanded;
                    _mnExpand.Visible = listFiles.SelectedNode.GetNodeCount(true) != 0 &&
                                        !listFiles.SelectedNode.IsExpanded;

                    contextMenu.Show(listFiles, e.Location);
                }
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _forceClose = true;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            listFiles.Nodes.Clear();
        }

        #endregion
    }
}