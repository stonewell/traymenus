using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace TrayMenus
{
    public enum TrayMenuTypeEnum
    {
        Seperator,
        SubMenu,
        ShortCut,
        Folder,
        Root,
    }

    [XmlRoot]
    public class TrayMenu
    {
        #region Fields
        private TreeNode node_ = null;
        private List<TrayMenu> submenus_ = new List<TrayMenu>();
        
        private string name_ = null;
        private byte[] image_ = null;
        private string image_file_name_ = null;
        private TrayMenuTypeEnum menu_type_ = TrayMenuTypeEnum.ShortCut;

        private string executable_ = null;
        private string parameters_ = null;
        private ProcessWindowStyle show_flag_ = ProcessWindowStyle.Normal;
        private string exec_folder_ = null;

        private string menu_folder_ = null;
        #endregion

        #region Properties
        public List<TrayMenu> SubMenus
        {
            get { return submenus_; }
            set { submenus_ = value; }
        }

        public string Name
        {
            get { return name_; }
            set { name_ = value; }
        }

        public byte[] ImageBuffer
        {
            get { return image_; }
            set { image_ = value; }
        }

        public string ImageFileName
        {
            get { return image_file_name_; }
            set 
			{ 
				image_file_name_ = value; 

				byte[] oldImage = image_;
				
				if (File.Exists(image_file_name_)) {
					try {
						using(FileStream fs = new FileStream(image_file_name_, FileMode.Open, FileAccess.Read)) {
							image_ = new byte[fs.Length];
							fs.Read(image_,0, (int)fs.Length);
						}
					} catch {
						image_ = oldImage;
					}
				}
			}
        }

        public TrayMenuTypeEnum MenuType
        {
            get { return menu_type_; }
            set { menu_type_ = value; }
        }

        public string Executable
        {
            get { return executable_; }
            set { executable_ = value; }
        }

        public string Parameters
        {
            get { return parameters_; }
            set { parameters_ = value; }
        }

        public ProcessWindowStyle ShowFlag
        {
            get { return show_flag_; }
            set { show_flag_ = value; }
        }

        public string ExecFolder
        {
            get { return exec_folder_; }
            set { exec_folder_ = value; }
        }

        public string MenuFolder
        {
            get { return menu_folder_; }
            set { menu_folder_ = value; }
        }
        #endregion

        #region Methods
        public TreeNode ToTreeNode()
        {
            if (node_ == null)
            {
                node_ = new TreeNode(Name);
                node_.Tag = this;
            }

            if ((MenuType == TrayMenuTypeEnum.SubMenu  || MenuType == TrayMenuTypeEnum.Root) && 
                submenus_ != null)
            {
                node_.Nodes.Clear();

                foreach (TrayMenu menu in submenus_)
                {
                    node_.Nodes.Add(menu.ToTreeNode());
                }
            }

            return node_;
        }

        public void AddToMenuStrip(ToolStripDropDownMenu menu)
        {
            switch (MenuType)
            {
                case TrayMenuTypeEnum.Root:
                    foreach (TrayMenu tm in SubMenus)
                    {
                        tm.AddToMenuStrip(menu);
                    }
                    break;
                case TrayMenuTypeEnum.SubMenu:
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(Name);

                        ToolStripDropDownMenu dropdownMenu =
                            new ToolStripDropDownMenu();

                        foreach(TrayMenu tm in submenus_)
                        {
                            tm.AddToMenuStrip(dropdownMenu);
                        }

                        item.Text = Name;
                        item.DropDown = dropdownMenu;
                        
                        menu.Items.Add(item);

                    }
                    break;
                case TrayMenuTypeEnum.Folder:
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(Name);

                        ToolStripDropDownMenu dropdownMenu = 
                            new ToolStripDropDownMenu();

                        dropdownMenu.Items.Add("temp");

                        dropdownMenu.Tag = MenuFolder;

                        dropdownMenu.Opening += 
                            new System.ComponentModel.CancelEventHandler(dropdownMenu_Opening);

                        item.DropDown = dropdownMenu;

                        menu.Items.Add(item);
                    }
                    break;
                case TrayMenuTypeEnum.Seperator:
                    {
                        ToolStripSeparator item = new ToolStripSeparator();

                        menu.Items.Add(item);
                    }
                    break;
                case TrayMenuTypeEnum.ShortCut:
                    {
                        ToolStripMenuItem item = new ToolStripMenuItem(Name);
                        item.Tag = this;

                        string exec = Executable;

			            if (!string.IsNullOrEmpty(exec))
                            exec = Environment.ExpandEnvironmentVariables(exec);

				
						if (image_ != null) {
							try {
								System.Drawing.Image image =
									System.Drawing.Image.FromStream(new System.IO.MemoryStream(image_));
								item.Image = image;
							} catch {
							}
						} else	if (!string.IsNullOrEmpty(image_file_name_)) {
					        if (File.Exists(image_file_name_)) {
								try {
									System.Drawing.Image image =
										System.Drawing.Image.FromFile(image_file_name_);
									item.Image = image;
								} catch {
								}
							}
						} else if (File.Exists(exec)) {
                            Icon icon = Icon.ExtractAssociatedIcon(exec);

                            if (icon != null)
                            {
                                item.Image = icon.ToBitmap();
                            }
                        }

                        item.Click += new EventHandler(ShortCutItem_Click);

                        menu.Items.Add(item);
                    }
                    break;
            }
        }

        void dropdownMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
			try{
	            ToolStripDropDownMenu menu = null;
	
	            if (sender is ToolStripDropDownMenu)
	            {
	                menu = sender as ToolStripDropDownMenu;
	            }
	
	            if (menu != null)
	            {
	                menu.Items.Clear();
	
	                string folder = menu.Tag as string;
	
	                if (folder != null && Directory.Exists(folder))
	                {
	                    foreach (string dir in Directory.GetDirectories(folder))
	                    {
	                        ToolStripMenuItem item = 
	                            new ToolStripMenuItem(Path.GetFileName(dir));
	
	                        ToolStripDropDownMenu dirMenu =
	                            new ToolStripDropDownMenu();
	
	                        item.DropDown = dirMenu;
	                        dirMenu.Items.Add("temp");
	
	                        dirMenu.Opening += dropdownMenu_Opening;
	                        dirMenu.Tag = dir;
	
	                        menu.Items.Add(item);
	                    }
	
	                    foreach (string file in Directory.GetFiles(folder))
	                    {
	                        ToolStripMenuItem item =
	                            new ToolStripMenuItem(Path.GetFileName(file));
	
	                        item.Tag = file;
	
	                        item.Click += new EventHandler(FileItem_Click);
	                        menu.Items.Add(item);
	                    }
	
	                    if (menu.Items.Count == 0)
	                        e.Cancel = true;
	                }
	                else
	                {
	                    e.Cancel = true;
	                }
	            }
	            else
	            {
	                e.Cancel = true;
	            }
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
        }

        private void FileItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;

            if (item == null) return;

            string filename = item.Tag as string;

            if (filename == null) return;

            Process p = new Process();
            p.StartInfo.FileName = filename;

            p.Start();
        }

        private void ShortCutItem_Click(object sender, EventArgs e)
        {
            string execute_file_name = "";

			try {
	            ToolStripMenuItem item = sender as ToolStripMenuItem;
	
	            if (item == null) return;
	
	            TrayMenu menu = item.Tag as TrayMenu;
	
	            if (menu == null) return;
	
	            if (menu.MenuType != TrayMenuTypeEnum.ShortCut)
	                return;
	
	            if (string.IsNullOrEmpty(menu.Executable))
	                return;
	
	            Process p = new Process();
	
	            p.StartInfo.FileName = Environment.ExpandEnvironmentVariables(menu.Executable);

                execute_file_name = p.StartInfo.FileName;
	
	            if (!string.IsNullOrEmpty(menu.Parameters))
	                p.StartInfo.Arguments = menu.Parameters;
	
	            if (!string.IsNullOrEmpty(menu.ExecFolder))
	            {
	                p.StartInfo.WorkingDirectory = menu.ExecFolder;
	            }
	
	            p.StartInfo.WindowStyle = menu.ShowFlag;
	            p.StartInfo.UseShellExecute = true;
	
	            p.Start();
			} catch(Exception ex)	{
				MessageBox.Show(ex.Message + ",Executable Path:" + execute_file_name, 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
        }
        #endregion
    }
}
