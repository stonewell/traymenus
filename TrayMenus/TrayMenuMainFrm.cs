using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Reflection;

namespace TrayMenus
{
    public partial class TrayMenuMainFrm : Form
    {
        private bool modified_ = false;
        private string menu_file_name_ = null;

        private bool update_ui_ = false;

        private bool setting_ = false;
        private bool hidesetting_ = false;
        private bool hideloadmenu_ = false;
        private bool hideexit_ = false;

        private string notify_icon_file_ = null;
        private string notify_tooltip_ = null;

        private static readonly XmlSerializer xs_ =
            new XmlSerializer(typeof(TrayMenu));

        public TrayMenuMainFrm()
        {
            InitializeComponent();
        }

        ContextMenuStrip cmenu = null;

        public TrayMenuMainFrm(string file_name, bool setting, 
            bool hidesetting, bool hideloadmenu, bool hideexit,
            string notify_icon_file, string notify_tooltip)
        {
            setting_ = setting;
            menu_file_name_ = file_name;
            hidesetting_ = hidesetting;
            hideloadmenu_ = hideloadmenu;
            hideexit_ = hideexit;
            notify_icon_file_ = notify_icon_file;
            notify_tooltip_ = notify_tooltip;

            InitializeComponent();

            cmenu = new ContextMenuStrip(components);

			notifyIcon1.ContextMenuStrip = cmenu;

            resetNotifyIcon();
            
            cmenu.Opening += HandleOpening;
			cmenu.ItemClicked += HandleItemClicked;
			cmenu.Closed += delegate(object sender, ToolStripDropDownClosedEventArgs e) {
                resetNotifyIcon();
			};
        }

        private void resetNotifyIcon()
        {
            if (notify_tooltip_ != null)
                notifyIcon1.Text = notify_tooltip_;

            if (notify_icon_file_ != null && System.IO.File.Exists(notify_icon_file_))
            {
                System.Drawing.Icon icon = new Icon(notify_icon_file_);
                notifyIcon1.Icon = icon;
            }
            else
            {
                System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TrayMenuMainFrm));
                notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            }
            notifyIcon1.Visible = false;
            notifyIcon1.Visible = true;
        }

        private void HandleItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void HandleOpening(object sender, CancelEventArgs e)
        {
        	UpdateContextMenuStrip();
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
			try{
				notifyIcon1.GetType().InvokeMember(
	                "ShowContextMenu",
	                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
	                null,
	                notifyIcon1,
	                null
	            );
			}
			catch{
			}
        }

		private void UpdateContextMenuStrip()
		{
            cmenu.Items.Clear();

			try
			{
            	TrayMenu menu = tvwMenus.Nodes[0].Tag as TrayMenu;

				if (menu != null) {
	           	 	menu.AddToMenuStrip(cmenu);
				}
			}
			catch(Exception ex)
			{
				MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			if (cmenu.Items.Count > 0 && (!hideloadmenu_ || !hideexit_ || !hidesetting_))
            {
                cmenu.Items.Add(new ToolStripSeparator());
            }

            ToolStripItem item = null;

            if (!hidesetting_)
            {
                item = cmenu.Items.Add("Settings", null, new EventHandler(OnSettings));
                item.Tag = "SETTINGS_ITEM";
            }

            if (!hideloadmenu_)
            {
                item = cmenu.Items.Add("Load Menu...", null, new EventHandler(btnLoad_Click));
                item.Tag = "LOADMENU_ITEM";
            }

            if (!hideexit_)
            {
                cmenu.Items.Add(new ToolStripSeparator());
                item = cmenu.Items.Add("Exit", null, new EventHandler(OnExit));
                item.Tag = "EXIT_ITEM";
            }
		}
		
        private void OnExit(object sender, EventArgs e)
        {
            Close();
        }

        private void OnSettings(object sender, EventArgs e)
        {
            setting_ = true;

            Show();

            Activate();

            WindowState = FormWindowState.Normal;
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            if (!hidesetting_)
            {
                OnSettings(sender, e);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string name in Enum.GetNames(typeof(ProcessWindowStyle)))
            {
                cboShowFlags.Items.Add(name);
            }

            if (menu_file_name_ == null)
            {
                btnNew_Click(sender, e);
            }
            else
            {
                LoadMenuAndUpdate(menu_file_name_);
            }
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            if (CheckModifiedMenu())
            {
                return;
            }

            TrayMenu menu = new TrayMenu();
            menu.MenuType = TrayMenuTypeEnum.Root;
            menu.Name = "Root Menu";

            tvwMenus.Nodes.Clear();

            TreeNode node = menu.ToTreeNode();

            tvwMenus.Nodes.Add(node);
            tvwMenus.Refresh();

            tvwMenus.SelectedNode = node;

            UpdateControlStatus();

            modified_ = true;
            menu_file_name_ = null;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            TrayMenu submenu = new TrayMenu();
            submenu.MenuType = TrayMenuTypeEnum.ShortCut;
            submenu.Name = "MenuItem_" + menu.SubMenus.Count;

            menu.SubMenus.Add(submenu);

            TreeNode subnode = submenu.ToTreeNode();

            node.Nodes.Add(subnode);

            node.Expand();

            tvwMenus.SelectedNode = subnode;
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            TreeNode parentNode = node.Parent;

            if (parentNode != null)
            {
                TrayMenu parentMenu = parentNode.Tag as TrayMenu;

                if (parentMenu != null)
                {
                    parentMenu.SubMenus.Remove(menu);
                }
            }

            node.Remove();

            tvwMenus.SelectedNode = parentNode;
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            TreeNode parentNode = node.Parent;

            if (parentNode != null)
            {
                TrayMenu parentMenu = parentNode.Tag as TrayMenu;

                int index = node.PrevNode.Index;

                node.Remove();
                parentNode.Nodes.Insert(index, node);

                if (parentMenu != null)
                {
                    parentMenu.SubMenus.Remove(menu);
                    parentMenu.SubMenus.Insert(index, menu);
                }
            }

            modified_ = true;
            tvwMenus.SelectedNode = node;
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            TreeNode parentNode = node.Parent;

            if (parentNode != null)
            {
                TrayMenu parentMenu = parentNode.Tag as TrayMenu;

                int index = node.NextNode.Index;

                node.Remove();
                parentNode.Nodes.Insert(index, node);

                if (parentMenu != null)
                {
                    parentMenu.SubMenus.Remove(menu);
                    parentMenu.SubMenus.Insert(index, menu);
                }
            }

            modified_ = true;
            tvwMenus.SelectedNode = node;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveMenu();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (CheckModifiedMenu())
                return;

            LoadMenuAndUpdate(null);
        }

        private void LoadMenuAndUpdate(string filename)
        {
            TrayMenu menu = LoadMenu(filename);

            if (menu != null)
            {
                tvwMenus.Nodes.Clear();

                TreeNode node = menu.ToTreeNode();

                tvwMenus.Nodes.Add(node);
                tvwMenus.Refresh();
                tvwMenus.ExpandAll();

                tvwMenus.SelectedNode = node;

                UpdateControlStatus();

                notifyIcon1.BalloonTipText = menu.Name;
            }
			
			modified_ = false;
        }

        //return true to cancel the current request
        private bool CheckModifiedMenu()
        {
            if (!modified_)
                return false;

            DialogResult result =
                MessageBox.Show("Menu has been changed, Save?", "Save Changed Menu", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                return !SaveMenu();
            }
            else if (result == DialogResult.No)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private TrayMenu LoadMenu(string filename)
        {
            if (filename == null)
            {
                if (DialogResult.OK == openFileDialog1.ShowDialog(this))
                {
                    filename = openFileDialog1.FileName;
                }
            }

            if (!string.IsNullOrEmpty(filename))
            {
                filename = Environment.ExpandEnvironmentVariables(filename);

                try
                {
                    using (FileStream fs = new FileStream(filename,
                        FileMode.Open, FileAccess.Read))
                    {
                        TrayMenu menu = xs_.Deserialize(fs) as TrayMenu;

                        SetCurrentMenuFile(filename);

                        return menu;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Load Menu Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return null;
        }

        private void SetCurrentMenuFile(string filename)
        {
            menu_file_name_ = filename;

            Text = "Tray Menus - " + Path.GetFileName(menu_file_name_);
        }

        private bool SaveMenu()
        {
            if (menu_file_name_ == null)
            {
                if (DialogResult.OK == saveFileDialog1.ShowDialog(this))
                {
                    SetCurrentMenuFile(saveFileDialog1.FileName);
                }
                else
                {
                    return false;
                }
            }

            string tmp = Path.GetTempFileName();

            try
            {
                if (File.Exists(menu_file_name_))
                {
                    File.Copy(menu_file_name_, tmp, true);
                }

                using (FileStream fs = new FileStream(menu_file_name_, FileMode.Create, FileAccess.Write))
                {
                    xs_.Serialize(fs, tvwMenus.Nodes[0].Tag);
                }

                modified_ = false;
            }
            catch (Exception ex)
            {
                if (File.Exists(menu_file_name_))
                {
                    try
                    {
                        File.Copy(tmp, menu_file_name_, true);
                    }
                    catch
                    {
                    }
                }

                MessageBox.Show(ex.Message, "Save Menu Fail", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return true;
        }

        private void UpdateControlStatus()
        {
            try
            {
                this.SuspendLayout();

                btnAdd.Enabled = false;
                btnRemove.Enabled = false;
                btnDown.Enabled = false;
                btnUp.Enabled = false;

                EnableControls(splitContainer1.Panel2, false);

                TreeNode node = tvwMenus.SelectedNode;

                if (node == null)
                    return;

                TrayMenu menu = node.Tag as TrayMenu;

                if (menu == null)
                    return;

                if (menu.MenuType == TrayMenuTypeEnum.SubMenu ||
                    menu.MenuType == TrayMenuTypeEnum.Root)
                    btnAdd.Enabled = true;

                if (menu.MenuType != TrayMenuTypeEnum.Root)
                {
                    btnRemove.Enabled = true;

                    if (node.NextNode != null)
                    {
                        btnDown.Enabled = true;
                    }

                    if (node.PrevNode != null)
                    {
                        btnUp.Enabled = true;
                    }
                }

                UpdateRightPanel();
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void EnableControls(Control c, bool enable)
        {
            c.Enabled = enable;

            foreach (Control c1 in c.Controls)
            {
                EnableControls(c1, enable);
            }
        }

        private void tvwMenus_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateControlStatus();
        }

        private void UpdateRightPanel()
        {
            update_ui_ = true;

            try
            {
                EnableControls(splitContainer1.Panel2, false);

                TreeNode node = tvwMenus.SelectedNode;

                if (node == null)
                    return;

                TrayMenu menu = node.Tag as TrayMenu;

                if (menu == null)
                    return;

                txtName.Text = menu.Name;
                txtImage.Text = menu.ImageFileName;

                if (menu.MenuType == TrayMenuTypeEnum.Root)
                {
                    splitContainer1.Panel2.Enabled = true;
                    tableLayoutPanel1.Enabled = true;
                    txtName.Enabled = true;
                    txtImage.Enabled = true;
                    lblImage.Enabled = true;
                    lblName.Enabled = true;
                    btnSelectImage.Enabled = true;
                    return;
                }

                EnableControls(splitContainer1.Panel2, true);

                rdFolder.Checked = false;
                rdSeperator.Checked = false;
                rdShortCut.Checked = false;
                rdSubMenu.Checked = false;

                txtExecutable.Text = menu.Executable;
                txtParameters.Text = menu.Parameters;
                txtExecuteFolder.Text = menu.ExecFolder;
                txtMenuFolder.Text = menu.MenuFolder;
                cboShowFlags.SelectedItem = menu.ShowFlag.ToString();

                switch (menu.MenuType)
                {
                    case TrayMenuTypeEnum.Folder:
                        rdFolder.Checked = true;
                        break;
                    case TrayMenuTypeEnum.ShortCut:
                        rdShortCut.Checked = true;
                        break;
                    case TrayMenuTypeEnum.Seperator:
                        rdSeperator.Checked = true;
                        break;
                    case TrayMenuTypeEnum.SubMenu:
                        rdSubMenu.Checked = true;
                        break;
                    default:
                        return;
                }
            }
            finally
            {
                update_ui_ = false;
            }
        }

        private void rdSeperator_CheckedChanged(object sender, EventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            if (rdSeperator.Checked)
            {
                if (menu.MenuType != TrayMenuTypeEnum.Seperator)
                {
                    menu.MenuType = TrayMenuTypeEnum.Seperator;

                    modified_ = true;

                    UpdateControlStatus();
                }
            }
        }

        private void rdSubMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            if (rdSubMenu.Checked)
            {
                if (menu.MenuType != TrayMenuTypeEnum.SubMenu)
                {
                    menu.MenuType = TrayMenuTypeEnum.SubMenu;

                    modified_ = true;

                    UpdateControlStatus();
                }
            }
        }

        private void rdShortCut_CheckedChanged(object sender, EventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            if (rdShortCut.Checked)
            {
                if (menu.MenuType != TrayMenuTypeEnum.ShortCut)
                {
                    menu.MenuType = TrayMenuTypeEnum.ShortCut;

                    modified_ = true;

                    UpdateControlStatus();
                }
            }
        }

        private void rdFolder_CheckedChanged(object sender, EventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            if (rdFolder.Checked)
            {
                if (menu.MenuType != TrayMenuTypeEnum.Folder)
                {
                    menu.MenuType = TrayMenuTypeEnum.Folder;

                    modified_ = true;

                    UpdateControlStatus();
                }
            }
        }

        private void btnSelectImage_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == openFileDialog2.ShowDialog(this))
            {
                txtImage.Text = openFileDialog2.FileName;
                txtImage.Focus();
            }
        }

        private void btnBrowseExecuteFolder_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == folderBrowserDialog1.ShowDialog(this))
            {
                txtExecuteFolder.Text = folderBrowserDialog1.SelectedPath;

                txtExecuteFolder.Focus();
            }
        }

        private void btnBrowseFolder_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == folderBrowserDialog1.ShowDialog(this))
            {
                txtMenuFolder.Text = folderBrowserDialog1.SelectedPath;

                txtMenuFolder.Focus();
            }
        }

        private void txtName_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.Name = txtName.Text;
            node.Text = txtName.Text;

            if (menu.MenuType == TrayMenuTypeEnum.Root)
                notifyIcon1.BalloonTipText = menu.Name;

            modified_ = true;
        }

        private void txtImage_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.ImageFileName = txtImage.Text;

            modified_ = true;
        }

        private void txtExecutable_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.Executable = txtExecutable.Text;

            modified_ = true;
        }

        private void txtParameters_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.Parameters = txtParameters.Text;

            modified_ = true;
        }

        private void cboShowFlags_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            if (cboShowFlags.SelectedItem != null)
            {
                try
                {
                    menu.ShowFlag =
                        (ProcessWindowStyle)Enum.Parse(typeof(ProcessWindowStyle), cboShowFlags.SelectedItem.ToString());

                    modified_ = true;
                }
                catch
                {
                }
            }
        }

        private void txtExecuteFolder_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.ExecFolder = txtExecuteFolder.Text;

            modified_ = true;
        }

        private void txtMenuFolder_Validating(object sender, CancelEventArgs e)
        {
            if (update_ui_) return;

            TreeNode node = tvwMenus.SelectedNode;

            if (node == null)
                return;

            TrayMenu menu = node.Tag as TrayMenu;

            if (menu == null)
                return;

            menu.MenuFolder = txtMenuFolder.Text;

            modified_ = true;
        }

        private void TrayMenuMainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CheckModifiedMenu())
                e.Cancel = true;
        }

        private void btnSelectExecutable_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == openFileDialog3.ShowDialog(this))
            {
                txtExecutable.Text = openFileDialog3.FileName;
                txtExecutable.Focus();
            }
        }

        private void TrayMenuMainFrm_Activated(object sender, EventArgs e)
        {
            if (!setting_)
            {
                Hide();
            }
        }

        private void TrayMenuMainFrm_Deactivate(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }
    }
}