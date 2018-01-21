using Make_FSELF_GUI;
using Make_FSELF_GUI.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Make_FSELF {
    public partial class Make_FSELF_GUI : Form {
        /// <summary>
        /// Path to make_fself.py script from flat_z.
        /// </summary>
        private string make_fself = string.Empty;

        /// <summary>
        /// Path to the Authentication DataBase file.
        /// </summary>
        public string authInfo = string.Empty;

        /// <summary>
        /// The path to use for opening a path.
        /// </summary>
        private string usePath = string.Empty;

        /// <summary>
        /// The path to use for opening the database file.
        /// </summary>
        private string dbPath = string.Empty;

        /// <summary>
        /// The selected File of the TreeView.
        /// </summary>
        private string selectedFile = string.Empty;

        /// <summary>
        /// A Boolian to indicate which view is visible.
        /// </summary>
        private static bool tree = true;

        /// <summary>
        /// A counter for the batch process to determine how much elfs would be successfully converted.
        /// </summary>
        private static int batchCounter = 0;

        /// <summary>
        /// Stores the output of make_fself.py script.
        /// </summary>
        private static string errorString;

        /// <summary>
        /// Stores the fws from the authInfo database.
        /// </summary>
        private List<string> Fws;

        /// <summary>
        /// Stores the Names of all the apps from the authInfo database.
        /// </summary>
        private string[][] Apps;

        /// <summary>
        /// Strores all the authentication infos from the db.
        /// </summary>
        private string[][] Auths;

        /// <summary>
        /// Stores the selected node.
        /// </summary>
        private TreeNode selectedNode;

        /// <summary>
        /// A ClipboardWatcehr Instance to check for DataChanged.
        /// </summary>
        private ClipboardWatcher clipboard;

        /// <summary>
        /// Determine StringComparison level.
        /// </summary>
        private StringComparison ignore = StringComparison.InvariantCultureIgnoreCase;

        /// <summary>
        /// GUI Entry Point
        /// </summary>
        public Make_FSELF_GUI() { InitializeComponent(); }

        /// <summary>
        /// On Load of Form do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Make_FSELF_GUI_Load(object sender, EventArgs e) {
            // Check for python.
            if (!SwissKnife.CheckRegKey("hklm", @"SOFTWARE\CLASSES\Applications\python.exe")) MessagBox.Warning("Looks like Python is not installed on this System !");

            // Tell the MessagBox Class to center the Buttons.
            MessagBox.ButtonPosition = ButtonPosition.Center;

            // Set the Icon List for the TreeView.
            treeView.ImageList = TreeViewExtension.ImageList;

            //Initialize a new instance of the ClipboardWatcher class. By default it is set to watch for text.
            clipboard = new ClipboardWatcher(DataFormats.Text, false);
            clipboard.ContentPresent += Clipboard_ContentPresent;

            // Enable Drag Drop Events and KeyPress Event for the batch richTextBox.
            richTextBox.AllowDrop = true;
            richTextBox.DragEnter += Make_FSELF_GUI_DragEnter;
            richTextBox.DragDrop += Make_FSELF_GUI_DragDrop;
            richTextBox.KeyPress += RtbBatch_KeyPress;

            // Load settings.
            Settings sett = new Settings();
            if (sett.Mode == 12) batchNormalToolStrip.PerformClick();
            else if (sett.Mode == 21) normalAdvancedToolStrip.PerformClick();
            else if (sett.Mode == 22) batchAdvancedToolStrip.PerformClick();
            else normalNormalToolStrip.PerformClick();
            usePath = sett.LastPath;
            dbPath = sett.DbPath;
            make_fself = sett.MfselfPath;
            hexifyAuthInfoToolStrip.Checked = sett.Hexify;
            if (hexifyAuthInfoToolStrip.Checked) {
                if ((HexAllign)sett.HexAllign == HexAllign.x4) { x4ToolStrip.Checked = true; x4ToolStrip.Enabled = false; }
                else if ((HexAllign)sett.HexAllign == HexAllign.x8) { x8ToolStrip.Checked = true; x8ToolStrip.Enabled = false; }
                else if ((HexAllign)sett.HexAllign == HexAllign.x16) { x16ToolStrip.Checked = true; x16ToolStrip.Enabled = false; }

                if ((ByteAllign)sett.ByteAllign == ByteAllign.b1) { byte1ToolStrip.Checked = true; byte1ToolStrip.Enabled = false; }
                else if ((ByteAllign)sett.ByteAllign == ByteAllign.b2) { byte2ToolStrip.Checked = true; byte2ToolStrip.Enabled = false; } 
                else if ((ByteAllign)sett.ByteAllign == ByteAllign.b4) { byte4ToolStrip.Checked = true; byte4ToolStrip.Enabled = false; } 
                else if ((ByteAllign)sett.ByteAllign == ByteAllign.b8) { byte8ToolStrip.Checked = true; byte8ToolStrip.Enabled = false; } 
                else if ((ByteAllign)sett.ByteAllign == ByteAllign.b16) { byte16ToolStrip.Checked = true; byte16ToolStrip.Enabled = false; }
            }
            if (!hexifyAuthInfoToolStrip.Checked) hexifyToolStrip.Enabled = hexifyContextMenu.Enabled = false;

            // In case settings path are empty.
            if (dbPath == string.Empty) dbPath = Directory.GetCurrentDirectory() + @"\authinfo.txt";
            if (make_fself == string.Empty) make_fself = Directory.GetCurrentDirectory() + @"\make_fself.py";

            // Fill Combo Box and set default.
            comboType.Items.AddRange(new string[] { "  fake", "  npdrm_exec", "  npdrm_dynlib", "  system_exec", "  system_dynlib", "  host_kernel", "  secure_module", "  secure_kernel" });
            comboType.SelectedItem = "  fake";

            // Load Authentication DataBase.
            if (!File.Exists(dbPath)) MessagBox.Info("Can not access the authentication information database \nPlease place 'authinfo.txt' into the same directory from where this app runs from.");
            else {                
                Fws = new List<string>();
                List<string[]> _Apps = new List<string[]>();
                List<string[]> _Auths = new List<string[]>();
                List<string> _apps = new List<string>();
                List<string> _auths = new List<string>();
                bool app, auth, fw;
                app = auth = fw = false;
                foreach (string line in dbPath.ReadAllLines()) {
                    if (!string.IsNullOrEmpty(line)) {
                        if (line.Contains("[FW=", ignore)) {
                            if (Fws.Count > 0 && app) { MessagBox.Error("DataBase Inconsistent !"); break; }

                            string[] tag = line.Split(']');
                            Fws.Add(tag[0].XReplace(@"[[]FW=?", string.Empty, RegexOptions.IgnoreCase));
                            fw = true;
                            if (Fws.Count > 1) {
                                _Apps.Add(_apps.ToArray());
                                _Auths.Add(_auths.ToArray());
                                _apps = new List<string>();
                                _auths = new List<string>();
                            }

                        } else if (line.Contains("[Name=", ignore)) {
                            if (!fw) {
                                if (!auth) { MessagBox.Error("DataBase Inconsistent !"); break; }
                            }

                            string[] tag = line.Split(']');
                            _apps.Add(tag[0].XReplace(@"[[]Name=?\s?", string.Empty, RegexOptions.IgnoreCase));
                            MessagBox.Debug(tag[0].XReplace(@"[[]Name=?\s?", string.Empty, RegexOptions.IgnoreCase));
                            auth = fw = false;
                            app = true;
                        } else if (line.Contains("[Auth=", ignore)) {
                            if (!app) {
                                if (fw) { MessagBox.Error("DataBase Inconsistent !"); break; }
                            }

                            string[] tag = line.Split(']');
                            _auths.Add(tag[0].XReplace(@"[[]Auth=?", string.Empty, RegexOptions.IgnoreCase));
                            app = false;
                            auth = true;
                        }
                    }
                }

                _Apps.Add(_apps.ToArray());
                _Auths.Add(_auths.ToArray());

                if (Fws.Count != _Apps.Count || Fws.Count != _Auths.Count) {
                    MessagBox.Error("DataBase Inconsistent !");
                    Fws.Clear();
                } else {
                    comboFW.Items.AddRange(Fws.ToArray());
                    Apps = _Apps.ToArray();
                    Auths = _Auths.ToArray();
                }
            }
        }

        /// <summary>
        /// Draw GUI in advanced mode.
        /// </summary>
        private void GuiAdvanced() {
            groupAdvanced.Visible = groupVersion.Visible = textBoxPaid.Visible = comboType.Visible = labelPaid.Visible = labelType.Visible = true;
            groupAdvanced.Text = "Advanced Options";
            ClientSize = new Size(618, 713);
        }

        /// <summary>
        /// Draw GUI in normal mode.
        /// </summary>
        private void GuiNormal() {
            groupAdvanced.Visible = true;
            groupVersion.Visible = textBoxPaid.Visible = comboType.Visible = labelPaid.Visible = labelType.Visible = false;
            groupAdvanced.Text = "Normal Options";
            ClientSize = new Size(618, 686);
        }

        /// <summary>
        /// Draw Gui in Only Fake Sign mode.
        /// </summary>
        private void GuiFake() {
            groupAdvanced.Visible = false;
            ClientSize = new Size(618, 504);
        }

        /// <summary>
        /// Deactivate all Tool and Context menu strips.
        /// </summary>
        private void DeactivateToolAndContext() {
            batchFakeToolStrip.Checked = batchFakeContextMenu.Checked = normalAdvancedToolStrip.Checked = normalNormalToolStrip.Checked = batchAdvancedToolStrip.Checked = batchNormalToolStrip.Checked = false;
            normalFakeToolStrip.Checked = normalFakeContextMenu.Checked = normalNContextMenu.Checked = normalAContextMenu.Checked = batchNContextMenu.Checked = batchAContextMenu.Checked = false;
            batchFakeToolStrip.Enabled = batchFakeContextMenu.Enabled = normalAdvancedToolStrip.Enabled = normalNormalToolStrip.Enabled = batchAdvancedToolStrip.Enabled = batchNormalToolStrip.Enabled = true;
            normalFakeToolStrip.Enabled = normalFakeContextMenu.Enabled = normalNContextMenu.Enabled = normalAContextMenu.Enabled = batchNContextMenu.Enabled = batchAContextMenu.Enabled = true;
        }

        /// <summary>
        /// Change the File Window to TreeView.
        /// </summary>
        private void TreeView() {
            treeView.Visible = tree = true;
            richTextBox.Visible = false;
            DeactivateToolAndContext();
        }

        /// <summary>
        /// Change the File Window to Batch View.
        /// </summary>
        private void BatchView() {
            treeView.Visible = tree = false;
            richTextBox.Visible = true;
            DeactivateToolAndContext();
        }

        /// <summary>
        /// Gets all elf and bin files, including sub dirs, from the overloaded path and returns them.
        /// </summary>
        /// <param name="path">The path to look up.</param>
        /// <returns>All founded files and subdirs.</returns>
        private string[] GetFiles(string path) {
            string[] elfs = path.GetFilesNDirsRecursive("*.elf");
            string[] bins = path.GetFilesNDirsRecursive("*.bin");
            string[] found = new string[elfs.Length + bins.Length];
            Array.Copy(elfs, found, elfs.Length);
            Array.Copy(bins, 0, found, elfs.Length, bins.Length);
            return found;
        }

        /// <summary>
        /// Reset the Hex Allign Tool Strip Menu Item.
        /// </summary>
        private void ResetHexAllignStrip() {
            x8ToolStrip.Checked = x4ToolStrip.Checked = x16ToolStrip.Checked = x4ContextMenu.Checked = x8ContextMenu.Checked = x16ContextMenu.Checked = false;
            x8ToolStrip.Enabled = x4ToolStrip.Enabled = x16ToolStrip.Enabled = x4ContextMenu.Enabled = x8ContextMenu.Enabled = x16ContextMenu.Enabled = true;
        }

        /// <summary>
        /// Reset the Byte Allign Tool Strip Menu Item.
        /// </summary>
        private void ResetByteAllignStrip() {
            byte1ToolStrip.Checked = byte2ToolStrip.Checked = byte4ToolStrip.Checked = byte8ToolStrip.Checked = byte16ToolStrip.Checked = false;
            b1ContextMenu.Checked = b2ContextMenu.Checked = b4ContextMenu.Checked = b8ContextMenu.Checked = b16ContextMenu.Checked = false;
            byte1ToolStrip.Enabled = byte2ToolStrip.Enabled = byte4ToolStrip.Enabled = byte8ToolStrip.Enabled = byte16ToolStrip.Enabled = true;
            b1ContextMenu.Enabled = b2ContextMenu.Enabled = b4ContextMenu.Enabled = b8ContextMenu.Enabled = b16ContextMenu.Enabled = false;
        }
        
        /// <summary>
        /// Get the User Choosen Byte Allign.
        /// </summary>
        /// <returns>The User Choosen Byte Allign.</returns>
        private ByteAllign GetByteAllign() {
            if (byte1ToolStrip.Checked) return ByteAllign.b1;
            else if (byte2ToolStrip.Checked) return ByteAllign.b2;
            else if (byte4ToolStrip.Checked) return ByteAllign.b4;
            else if (byte8ToolStrip.Checked) return ByteAllign.b8;
            else return ByteAllign.b16;
        }

        /// <summary>
        /// Get the User Choosen Hex Allign.
        /// </summary>
        /// <returns>The User Choosen Hex Allign.</returns>
        private HexAllign GetHexAllign() {
            if (x4ToolStrip.Checked) return HexAllign.x4;
            else if (x8ToolStrip.Checked) return HexAllign.x8;
            else return HexAllign.x16;
        }

        /// <summary>
        /// Dehexify the Auth Info.
        /// </summary>
        /// <param name="source">The source stirng to use.</param>
        /// <returns>The formated string.</returns>
        private string BuildAuthInfo(string[] source) {
            string builded = string.Empty;
            foreach (string line in source) builded += line;
            return builded;
        }

        /// <summary>
        /// On Clipboard ContentChanged do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        public void Clipboard_ContentPresent(object sender, AppEventArgs e) {
            if (e.DataPresent) rtbAuthInfo.ContextMenu.MenuItems[1].Enabled = true;
            else rtbAuthInfo.ContextMenu.MenuItems[1].Enabled = false;
        }

        /// <summary>
        /// On Drag Enter do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Make_FSELF_GUI_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false)) e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        /// <summary>
        /// On Drag Drop do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Make_FSELF_GUI_DragDrop(object sender, DragEventArgs e) {
            string[] data = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            
            if (data[0].Contains("make_fself.py")) make_fself = data[0];
            else if (data[0].Contains("authinfo.txt")) dbPath = data[0];

            if (treeView.Visible) {
                if (data[0].IsFolder()) treeView.PopulateTreeView(this, GetFiles(data[0]), '\\', data[0]);
                else treeView.PopulateTreeView(this, GetFiles(data[0].GetPath()), '\\', data[0]);
            } else {
                foreach (string path in data) {
                    if (path.Contains(".elf", StringComparison.InvariantCultureIgnoreCase) || path.Contains(".bin", StringComparison.InvariantCultureIgnoreCase)) {
                        if (!richTextBox.Text.Contains(path)) richTextBox.Text += path + "\n";
                    }
                }
            }
        }

        /// <summary>
        /// After a TreeView Node selection do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e) {
            selectedFile = e.Node.FullPath;
            selectedNode = e.Node;
        }

        /// <summary>
        /// After a TreeView Node Expand do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void TreeView_AfterExpand(object sender, TreeViewEventArgs e) {
            e.Node.ImageKey = "Folder Open";
            e.Node.SelectedImageKey = "Folder Open";
        }

        /// <summary>
        /// After a TreeView Node Collapse do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void TreeView_AfterCollapse(object sender, TreeViewEventArgs e) {
            e.Node.ImageKey = "Folder";
            e.Node.SelectedImageKey = "Folder";
        }
        
        /// <summary>
        /// On RichTextBox KeyBoard Key Press do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void RtbAuthInfo_KeyPress(object sender, KeyPressEventArgs e) {
            if (e.KeyChar == 22) {
                IDataObject iData = Clipboard.GetDataObject();
                if (!iData.GetDataPresent(DataFormats.Text)) {
                    e.Handled = true;
                    rtbAuthInfo.Undo();
                }
            }
        }

        /// <summary>
        /// On RichTextBox Batch View KeyBoard Key Press do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void RtbBatch_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
            rtbAuthInfo.Undo();
        }

        /// <summary>
        /// On RichTextBox Text Changed do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void RtbAuthInfo_TextChanged(object sender, EventArgs e) {
            if (rtbAuthInfo.Text != string.Empty) clearContextMenu.Enabled = copyContextMenu.Enabled = true;
            else clearContextMenu.Enabled = copyContextMenu.Enabled = false;

            if (rtbAuthInfo.Text != string.Empty) {
                clearContextMenu.Enabled = true;

                if (!rtbAuthInfo.Text.IsHex()) {
                    if (!rtbAuthInfo.Text.IsHexifyed()) {
                        rtbAuthInfo.Undo();
                        return;
                    }
                }

                if (hexifyAuthInfoToolStrip.Checked && rtbAuthInfo.Text != string.Empty) {
                    if (!rtbAuthInfo.Text.IsHexifyed()) {
                        List<string> hexed = new List<string>();
                        foreach (string line in rtbAuthInfo.Text.Hexify(GetByteAllign(), GetHexAllign())) hexed.Add(line);
                        rtbAuthInfo.Text = "";
                        rtbAuthInfo.Lines = hexed.ToArray();
                    }
                }
            } else clearContextMenu.Enabled = false;
        }

        /// <summary>
        /// On richTextBox AuthInfo selcetion changed do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void RrtbAuthInfo_SelectionChanged(object sender, EventArgs e) {
            if (rtbAuthInfo.SelectionLength > 0) pasteContextMenu.Enabled = true;
            else pasteContextMenu.Enabled = false;
        }

        /// <summary>
        /// On comboBox FW SelectedIndex changed do..
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ComboFW_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboFW.SelectedIndex >= 0) {
                comboApp.Items.Clear();
                comboApp.Items.AddRange(Apps[comboFW.SelectedIndex]);
            }
        }

        /// <summary>
        /// On comboBox App SelectedIndex changed do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ComboApp_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboApp.SelectedIndex >= 0) {
                string authi = Auths[comboFW.SelectedIndex][comboApp.SelectedIndex];
                textBoxPaid.Text = rtbAuthInfo.Text = string.Empty;
                if (authi != string.Empty) {
                    if (authi.Length == 0x88 * 2) {
                        textBoxPaid.Text = "0x" + authi.Substring(0, 16);
                        rtbAuthInfo.Text = authi;
                    } else MessagBox.Error("Authentication Information for this Application is too short !\nShould be in Length of 272 Characters.");
                } else MessagBox.Error("Authentication Information within DataBase for this Application is Empty !");
            }
        }

        /// <summary>
        /// Output Event Handler for the make_fself.py Process.
        /// </summary>
        /// <param name="sendingProcess">The Process which triggered this Event.</param>
        /// <param name="outLine">The Received Data Event Arguments.</param>
        private static void MFSELF_OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
            if (!String.IsNullOrEmpty(outLine.Data)) {
                if (outLine.Data.Contains("done")) {
                    if (tree) MessagBox.Show(outLine.Data + " !");
                    else batchCounter++;
                }
            }
        }

        /// <summary>
        /// Error Event Handler for the make_fself.py Process.
        /// </summary>
        /// <param name="sendingProcess">The Process which triggered this Event.</param>
        /// <param name="outLine">The Received Data Event Arguments.</param>
        private static void MFSELF_ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine) { errorString += outLine.Data; }

        /// <summary>
        /// On Menu Strip Open File click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void OpenFileToolStrip_Click(object sender, EventArgs e) {
            string[] files;
            if (richTextBox.Visible) files = MessagBox.ShowOpenFile("Select File", "PS4 Executeables (*.elf; *.bin)|*.elf;*.bin|All files (*.*)|*.*", usePath, true);
            else files = MessagBox.ShowOpenFile("Select File", "PS4 Executeables (*.elf; *.bin)|*.elf;*.bin|All files (*.*)|*.*", usePath, false);
            if (files.Length > 0) {
                usePath = files[0];
                if (richTextBox.Visible) {
                    foreach (string file in files) {
                        if (file.Contains(".elf", StringComparison.InvariantCultureIgnoreCase) || file.Contains(".bin", StringComparison.InvariantCultureIgnoreCase)) {
                            if (!richTextBox.Text.Contains(file)) richTextBox.Text += file + "\n";
                        }
                    }
                } else treeView.PopulateTreeView(this, GetFiles(files[0].GetPath()), '\\', files[0]);
            }
        }

        /// <summary>
        /// On Menu Strip Open Folder click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void OpenFolderToolStrip_Click(object sender, EventArgs e) {
            string folder = MessagBox.ShowFolderBrowse("Choose a Folder with one or more ELF", usePath);
            if (folder != string.Empty) {
                usePath = folder;
                string[] found = GetFiles(folder);
                if (found.Length > 0) {
                    if (richTextBox.Visible) {
                        foreach (string file in found) { if (!richTextBox.Text.Contains(file)) richTextBox.Text += file + "\n"; }
                    } else treeView.PopulateTreeView(this, found, '\\', folder);
                }
            }
        }

        /// <summary>
        /// On Menu Strip Close Click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>     
        private void CloseToolStrip_Click(object sender, EventArgs e) { Close(); }

        /// <summary>
        /// On Menu Strip Normal > Batch click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchNormalToolStrip_Click(object sender, EventArgs e) {
            BatchView();
            batchNormalToolStrip.Checked = batchNContextMenu.Checked = true;
            batchNormalToolStrip.Enabled = batchNContextMenu.Enabled = false;
            if (!normalNormalToolStrip.Checked || !normalNContextMenu.Checked) GuiNormal();
        }

        /// <summary>
        /// On Menu Strip Advanced > Batch click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchAdvancedToolStrip_Click(object sender, EventArgs e) {
            BatchView();
            batchAdvancedToolStrip.Checked = batchAContextMenu.Checked = true;
            batchAdvancedToolStrip.Enabled = batchAContextMenu.Enabled = false;
            if (!normalAdvancedToolStrip.Checked || !normalAContextMenu.Checked) GuiAdvanced();
        }

        /// <summary>
        /// On Menu Strip Normal > Normal click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalNormalToolStrip_Click(object sender, EventArgs e) {
            TreeView();
            normalNormalToolStrip.Checked = normalNContextMenu.Checked = true;
            normalNormalToolStrip.Enabled = normalNContextMenu.Enabled = false;
            if (!batchNormalToolStrip.Checked || !batchNContextMenu.Checked) GuiNormal();
        }

        /// <summary>
        /// On Menu Strip Advanced > Normal click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalAdvancedToolStrip_Click(object sender, EventArgs e) {
            TreeView();
            normalAdvancedToolStrip.Checked = normalAContextMenu.Checked = true;
            normalAdvancedToolStrip.Enabled = normalAContextMenu.Enabled = false;
            if (!batchAdvancedToolStrip.Checked || !batchAContextMenu.Checked) GuiAdvanced();
        }

        /// <summary>
        /// On Menu Strip Only Fake Sign > Batch click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchFakeToolStrip_Click(object sender, EventArgs e) {
            BatchView();
            batchFakeToolStrip.Checked = batchFakeContextMenu.Checked = true;
            batchFakeToolStrip.Enabled = batchFakeContextMenu.Enabled = false;
            if (!normalFakeToolStrip.Checked || !normalFakeContextMenu.Checked) GuiFake();
        }

        /// <summary>
        /// On Menu Strip Only Fake Sign > Normal click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalFakeToolStrip_Click(object sender, EventArgs e) {
            TreeView();
            normalFakeToolStrip.Checked = normalFakeContextMenu.Checked = true;
            normalFakeToolStrip.Enabled = normalFakeContextMenu.Enabled = false;
            if (!batchFakeToolStrip.Checked || !batchFakeContextMenu.Checked) GuiFake();
        }
        
        /// <summary>
        /// Set a path to make_fself.py script.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void SetMFSELFToolStrip_Click(object sender, EventArgs e) {
            string pyPath = MessagBox.ShowOpenFile("Select make_fself.py", "Python Script (*.py)|*.py", make_fself);
            if (pyPath != string.Empty) {
                if (pyPath.Contains("make_fself.py")) make_fself = pyPath;
                else MessagBox.Error("That doesn't look like flat_z make_fself.py script !");
            }
        }

        /// <summary>
        /// Show saved path to the make_fself.py script.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ShowMFSELFToolStrip_Click(object sender, EventArgs e) { MessagBox.Info(make_fself); }

        /// <summary>
        /// Set a path to the authinfo database.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void SetAuthInfoToolStrip_Click(object sender, EventArgs e) {
            string _dbPath = MessagBox.ShowOpenFile("Select Auth Info DB", "Text File (*.txt)|*.txt", dbPath);
            if (_dbPath != string.Empty) {
                if (_dbPath.Contains("authinfo.txt")) dbPath = _dbPath;
                else MessagBox.Error("That doesn't look like the authinfo.txt file !");
            }
        }

        /// <summary>
        /// Show saved path to the authinfo database.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ShowAuthInfoToolStrip_Click(object sender, EventArgs e) { MessagBox.Info(dbPath); }

        /// <summary>
        /// On Tool Strip Menu Hex x4 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param> 
        private void X4ToolStrip_Click(object sender, EventArgs e) {
            if (!x4ToolStrip.Checked) {
                if (!byte1ToolStrip.Checked || !byte2ToolStrip.Checked || !byte4ToolStrip.Checked ||
                    !b1ContextMenu.Checked || !b2ContextMenu.Checked || !b4ContextMenu.Checked) {
                    ResetByteAllignStrip();
                    byte4ToolStrip.Checked = b4ContextMenu.Checked = true;
                    byte4ToolStrip.Enabled = b4ContextMenu.Enabled = false;
                }
                ResetHexAllignStrip();
                x4ToolStrip.Checked = x4ContextMenu.Checked = true;
                x4ToolStrip.Enabled = x4ContextMenu.Enabled = false;
            }
        }

        /// <summary>
        /// On Tool Strip Menu Hex x8 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void X8ToolStrip_Click(object sender, EventArgs e) {
            if (!x8ToolStrip.Checked) {
                if (byte16ToolStrip.Checked) {
                    byte8ToolStrip.Checked = b8ContextMenu.Checked = true;
                    byte8ToolStrip.Enabled = b8ContextMenu.Enabled = b16ContextMenu.Checked = b16ContextMenu.Enabled = byte16ToolStrip.Checked = byte16ToolStrip.Enabled = false;
                }
                ResetHexAllignStrip();
                x8ToolStrip.Checked = x8ContextMenu.Checked = true;
                x8ToolStrip.Enabled = x8ContextMenu.Enabled = false;
            }
        }

        /// <summary>
        /// On Tool Strip Menu Hex x16 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void X16ToolStrip_Click(object sender, EventArgs e) {
            if (!x16ToolStrip.Checked) {                
                ResetHexAllignStrip();
                x16ToolStrip.Checked = x16ContextMenu.Checked = true;
                x16ToolStrip.Enabled = x16ContextMenu.Enabled = false;
            }
        }

        /// <summary>
        /// On Tool Strip Menu Byte 1 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Byte1ToolStrip_Click(object sender, EventArgs e) {
            ResetByteAllignStrip();
            byte1ToolStrip.Checked = b1ContextMenu.Checked = true;
            byte1ToolStrip.Enabled = b1ContextMenu.Enabled = false;
        }

        /// <summary>
        /// On Tool Strip Menu Byte 2 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Byte2ToolStrip_Click(object sender, EventArgs e) {
            ResetByteAllignStrip();
            byte2ToolStrip.Checked = b2ContextMenu.Checked = true;
            byte2ToolStrip.Enabled = b2ContextMenu.Enabled = false;
        }

        /// <summary>
        /// On Tool Strip Menu Byte 4 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Byte4ToolStrip_Click(object sender, EventArgs e) {
            ResetByteAllignStrip();
            byte4ToolStrip.Checked = b4ContextMenu.Checked = true;
            byte4ToolStrip.Enabled = b4ContextMenu.Enabled = false;
        }

        /// <summary>
        /// On Tool Strip Menu Byte 8 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Byte8ToolStrip_Click(object sender, EventArgs e) {
            ResetByteAllignStrip();
            byte8ToolStrip.Checked = b8ContextMenu.Checked = true;
            byte8ToolStrip.Enabled = b8ContextMenu.Enabled = false;
        }

        /// <summary>
        /// On Tool Strip Menu Byte 16 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Byte16ToolStrip_Click(object sender, EventArgs e) {
            ResetByteAllignStrip();
            byte16ToolStrip.Checked = b16ContextMenu.Checked = true;
            byte16ToolStrip.Enabled = b16ContextMenu.Enabled = false;
        }

        /// <summary>
        /// On ToolStrip Hexify Auth Info click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void HexifyAuthInfoToolStrip_Click(object sender, EventArgs e) {
            if (hexifyAuthInfoToolStrip.Checked || hexifyContextMenu.Checked)
                hexifyAuthInfoToolStrip.Checked = hexifyToolStrip.Enabled = hexifyContextMenu.Enabled = hexifyAuthInfoContextMenu.Checked = false;
            else hexifyAuthInfoToolStrip.Checked = hexifyAuthInfoContextMenu.Checked = hexifyToolStrip.Enabled = hexifyContextMenu.Enabled = true;
        }

        /// <summary>
        /// On Menu Strip About Click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>     
        private void AboutToolStrip_Click(object sender, EventArgs e) {
            MessagBox.Info("About", "This is a grafical Interface\nfor the python make_fself.py script of flat_z\nto Fake Sign PS4 ELFs with custom Authentication Informations.\nv1.0");
        }

        /// <summary>
        /// On Context Menu Open Folder click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void FolderContextMenu_Click(object sender, EventArgs e) { openFolderToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Open File click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void FileContextMenu_Click(object sender, EventArgs e) { openFileToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Normal Normal click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalNContextMenu_Click(object sender, EventArgs e) { normalNormalToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Normal Batch click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchNContextMenu_Click(object sender, EventArgs e) { batchNormalToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Advanced Normal click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalAContextMenu_Click(object sender, EventArgs e) { normalAdvancedToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Advanced Batch click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchAContextMenu_Click(object sender, EventArgs e) { batchAdvancedToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Only Fake sign Normal click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void NormalFakeContextMenu_Click(object sender, EventArgs e) { normalFakeToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Modus Only Fake sign Batch click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void BatchFakeContextMenu_Click(object sender, EventArgs e) { batchFakeToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Set make_fself.py click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void SetmfselfContextMenu_Click(object sender, EventArgs e) { setMFSELFToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Show make_fself.py click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ShowmfselfContextMenu_Click(object sender, EventArgs e) { showMFSELFToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Set authinfo.txt click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void SetaiContextMenu_Click(object sender, EventArgs e) { setAuthInfoToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Show authinfo.txt click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ShowaiContextMenu_Click(object sender, EventArgs e) { showAuthInfoToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Byte Allign Byte 1 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void B1ContextMenu_Click(object sender, EventArgs e) { byte1ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Byte Allign Byte 2 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void B2ContextMenu_Click(object sender, EventArgs e) { byte2ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Byte Allign Byte 4 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void B4ContextMenu_Click(object sender, EventArgs e) { byte4ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Byte Allign Byte 8 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void B8ContextMenu_Click(object sender, EventArgs e) { byte8ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Byte Allign Byte 16 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void B16ContextMenu_Click(object sender, EventArgs e) { byte16ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Hex Allign Hex 4 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void X4ContextMenu_Click(object sender, EventArgs e) { x4ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Hex Allign Hex 8 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void X8ContextMenu_Click(object sender, EventArgs e) { x8ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Hex Allign Hex 16 click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void X16ContextMenu_Click(object sender, EventArgs e) { x16ToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Hexify Authentication informations click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void HexifyAuthInfoContextMenu_Click(object sender, EventArgs e) { hexifyAuthInfoToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu About click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void AboutContextMenu_Click(object sender, EventArgs e) { aboutToolStrip.PerformClick(); }

        /// <summary>
        /// On Context Menu Close click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void CloseContextMenu_Click(object sender, EventArgs e) { closeToolStrip.PerformClick(); }

        /// <summary>
        /// On ContextMenu Copy click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void CopyToolStripMenuItem_Click(object sender, EventArgs e) { rtbAuthInfo.Copy(); }

        /// <summary>
        /// On ContextMenu Paste click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void PasteContextMenu_Click(object sender, EventArgs e) { rtbAuthInfo.Paste(); }

        /// <summary>
        /// On ContextMenu Clear click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void ClearContextMenu_Click(object sender, EventArgs e) { rtbAuthInfo.Clear(); }

        /// <summary>
        /// On Tool Strip Python Call Script click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void PythonCallToolStrip_Click(object sender, EventArgs e) {
            if (pythonCallToolStrip.Checked) pythonCallToolStrip.Checked = pythonCallContextMenu.Checked = false;
            else pythonCallToolStrip.Checked = pythonCallContextMenu.Checked = true;
        }

        /// <summary>
        /// On ContextMenu Python Call Script click do.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void PythonCallContextMenu_Click(object sender, EventArgs e) { pythonCallToolStrip.PerformClick(); }

        /// <summary>
        /// On Only Fake Sign Menu Context click do.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnlyFakeSignContextMenu_Click(object sender, EventArgs e) { onlyFakeSignToolStrip.PerformClick(); }

        /// <summary>
        /// On Closing of Form.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>
        private void Make_FSELF_GUI_FormClosing(object sender, FormClosingEventArgs e) {
            Settings sett = new Settings();
            int mode = 0;
            if (normalNormalToolStrip.Checked) mode = 11;
            else if (batchNormalToolStrip.Checked) mode = 12;
            else if (normalAdvancedToolStrip.Checked) mode = 21;
            else if (batchAdvancedToolStrip.Checked) mode = 22;
            sett.Mode = mode;
            sett.LastPath = usePath;
            sett.DbPath = dbPath;
            sett.MfselfPath = make_fself;
            sett.Hexify = hexifyAuthInfoToolStrip.Checked;
            sett.ByteAllign = (int)GetByteAllign();
            sett.HexAllign = (int)GetHexAllign();
            sett.Save();
        }

        /// <summary>
        /// On Button Fake Sign ELF Click.
        /// </summary>
        /// <param name="sender">The Sender.</param>
        /// <param name="e">The Event Arguments.</param>     
        private void ButtonFSELF_Click(object sender, EventArgs e) {
            // Check for make_fself.py script.            
            if (!File.Exists(make_fself)) {
                MessagBox.Error("Can not find make_fself.py script !\nPlease place the script into the same Folder then this App runs from.\nOr define a path to the script.");
                return;
            }

            // Getting files.
            if (rtbAuthInfo.Text == string.Empty) { MessagBox.Error("No Authentication Information Provided !"); return; }
            List<string> toFakeSign = new List<string>();
            if (treeView.Visible) {
                if (selectedNode.IsSelected) toFakeSign.Add(selectedFile);
                else { MessagBox.Error("No File selected !"); return; }
            } else {
                toFakeSign.AddRange(richTextBox.Lines);
                if (toFakeSign.Count == 0) { MessagBox.Error("No File found in the Batch Window !"); return; }
            }            

            // Prepare for Script call.
            batchCounter = 0;
            string args = string.Empty;
            if (!normalFakeToolStrip.Checked || !batchFakeToolStrip.Checked) {
                if (normalAdvancedToolStrip.Checked || batchAdvancedToolStrip.Checked) {
                    if (textBoxPaid.Text != string.Empty) args += "--paid " + textBoxPaid.Text + " ";
                    else MessagBox.Error("Program Authentication ID is empty !");
                    if (comboType.SelectedIndex > -1) args += "--ptype " + (comboType.SelectedItem.ToString().Replace(" ", "")) + " ";
                    else MessagBox.Error("No Program Type selected !");
                    if (textBoxAppVersion.Text != string.Empty) args += "--app-version " + textBoxAppVersion.Text + " ";
                    else MessagBox.Error("Application version is empty !");
                    if (textBoxFWVersion.Text != string.Empty) args += "--fw-version " + textBoxFWVersion.Text + " ";
                    else MessagBox.Error("Firmware version is empty !");
                    if (rtbAuthInfo.Text != string.Empty) {
                        if (hexifyAuthInfoToolStrip.Checked) args += "--auth-info " + rtbAuthInfo.Text.ReplaceLineBreak().Dehexify() + " ";
                        else args += "--auth-info " + rtbAuthInfo.Text + " ";
                    } else MessagBox.Error("Program Authentication Information is empty !");
                } else {
                    if (textBoxPaid.Text != string.Empty) args += "--paid " + textBoxPaid.Text + " ";
                    else MessagBox.Error("Program Authentication ID is empty !");
                    if (rtbAuthInfo.Text != string.Empty) {
                        if (hexifyAuthInfoToolStrip.Checked) args += "--auth-info " + BuildAuthInfo(rtbAuthInfo.Text.Dehexify()).ReplaceLineBreak() + " ";
                        else args += "--auth-info " + rtbAuthInfo.Text + " ";
                    } else MessagBox.Error("Program Authentication Information is empty !");
                }
            }

            // Do call.
            ProcessStartInfo run = new ProcessStartInfo();
            Process python = new Process();
            python.OutputDataReceived += MFSELF_OutputHandler;
            python.ErrorDataReceived += MFSELF_ErrorHandler;

            if (pythonCallToolStrip.Checked) run.FileName = make_fself;
            else run.FileName = "python.exe";

            run.UseShellExecute = false;
            run.RedirectStandardOutput = run.CreateNoWindow = run.RedirectStandardError = true;            

            foreach (string elf in toFakeSign) {
                if (pythonCallToolStrip.Checked) run.Arguments = args + elf + " " + elf.Replace(".elf", ".self");
                else run.Arguments = make_fself + " " + args + elf + " " + elf.Replace(".elf", ".self");
                python.StartInfo = run;
                errorString = string.Empty;

                try { python.Start(); }
                catch (Exception) { MessagBox.Error("Can't run the python script.\nIf you have python installed, make sure to have it set in your system environment 'PATH' variable.\nIn whorst case just deinstall it and reinstall. Make sure to check the box 'set python as PATH variable' or some stuff like that."); return; }

                python.BeginOutputReadLine();
                python.BeginErrorReadLine();
                python.WaitForExit();

                if (errorString != string.Empty) MessagBox.Error(errorString);
            }
            if (!tree && batchCounter > 0) MessagBox.Show(batchCounter.ToString() + " elfs successfully fake signed !");
        }
    }
}