using System;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;
using MelonLoader;
using System.Linq;

namespace ThunderPacker
{
    public partial class ThunderPackerForm : Form
    {
        //-- Initialize form
        public ThunderPackerForm()
        {
            InitializeComponent();
        }

        //-- Variables
        public string ModPath = "";
        public string IconPath = "";
        public string Author = "";

        //-- Reject process
        public void RejectProcess(string Reason)
        {
            RejectedForm Popup = new RejectedForm();
            Popup.StartPosition = FormStartPosition.CenterParent;
            Popup.SetError(Reason);
            Popup.ShowDialog();
        }

        //-- Check manifest info
        public bool VerifyManifestInfo()
        {
            if (tb_ModName.Text != "" && tb_ModVersion.Text != "" && tb_ModDesc.Text != "") { return true; } else { return false; }
        }

        public bool ProjectBuildable(string ModFolderPath)
        {
            if (Directory.Exists(ModFolderPath)) { RejectProcess(String.Format("There is already a folder named \"{0}\" on your desktop", tb_ModName.Text)); return false; }
            if (!VerifyManifestInfo()) { RejectProcess("Missing required manifest data (name, version, desc)"); return false; }
            if (ModPath == "") { RejectProcess("No mod selected"); return false; }
            if (IconPath == "") { RejectProcess("No icon selected"); return false; }
            if (tb_ModVersion.Text.ToLower().Contains("v")) { RejectProcess(String.Format("Invalid version format. \"{0}\", try \"{1}\"", tb_ModVersion.Text, tb_ModVersion.Text.ToLower().Replace("v", ""))); return false; }
            if (tb_ModName.Text.Contains(" ")) { RejectProcess("Mod names can only contain a-z, A-Z, 0-9, and _ characers"); return false; }

            return true;
        }

        //-- Packaging handler
        private void b_Pack_Click(object sender, EventArgs e)
        {
            //.. Get file paths
            string FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string ModFolderPath = String.Format("{0}//{1}", FolderPath, tb_ModName.Text);

            //.. Initial checks
            if (!ProjectBuildable(ModFolderPath)) { return; }

            //.. Create package folder
            Directory.CreateDirectory(ModFolderPath);

            //.. Put the mod/icon into the package
            File.Copy(IconPath, String.Format("{0}//icon.png", ModFolderPath), true);
            File.Copy(ModPath, ModFolderPath + String.Format("//{0}.dll", tb_ModName.Text));

            //.. Create readme (optional)
            if (!cb_ExcludeRM.Checked)
            {
                var Readme = File.Create(String.Format("{0}//README.md", ModFolderPath));
                Readme.Close();

                File.WriteAllText(String.Format("{0}//README.md", ModFolderPath), tb_ReadmeEditor.Text);
            }

            //.. Get manifest dependencies
            string[] RawDependencies = tb_ModDeps.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            string Dependencies = "";

            if (RawDependencies.Length > 0)
            {
                foreach (string Dependency in RawDependencies)
                {
                    Dependencies += "\"" + Dependency + "\", ";
                }
                Dependencies = Dependencies.Substring(0, Dependencies.Length - 2); // remove last comma & space
            }

            //.. Create manifest
            var Manifest = File.Create(FolderPath + "//" + tb_ModName.Text + "//manifest.json");
            Manifest.Close();

            if (tb_ModWebsite.Text == "N/A") { tb_ModWebsite.Text = ""; }
            string JsonData = "{\n \"name\":\"" + tb_ModName.Text + "\",\n \"version_number\":\"" + tb_ModVersion.Text + "\",\n \"website_url\":\"" + tb_ModWebsite.Text + "\",\n \"description\":\"" + tb_ModDesc.Text + "\",\n" + " \"dependencies\":[" + Dependencies + "]\n}";
            File.WriteAllText(FolderPath + "//" + tb_ModName.Text + "//manifest.json", JsonData);

            //.. Zip contents of package
            string ZippedModName = Author + "-" + tb_ModName.Text + "-" + tb_ModVersion.Text;
            var ZippedMod = File.Create(String.Format("{0}//{1}.zip", ModFolderPath, ZippedModName));
            ZippedMod.Close();

            using (FileStream Zip = new FileStream(ZippedMod.Name, FileMode.Open, FileAccess.ReadWrite))
            {
                using (ZipArchive Archive = new ZipArchive(Zip, ZipArchiveMode.Update))
                {
                    ZipArchiveEntry ModEntry = Archive.CreateEntryFromFile(String.Format("{0}//{1}.dll", ModFolderPath, tb_ModName.Text), String.Format("{0}.dll", tb_ModName.Text));
                    ZipArchiveEntry IconEntry = Archive.CreateEntryFromFile(String.Format("{0}//icon.png", ModFolderPath), "icon.png");
                    ZipArchiveEntry ReadmeEntry = Archive.CreateEntryFromFile(String.Format("{0}//README.md", ModFolderPath), "README.md");
                    ZipArchiveEntry ManifestEntry = Archive.CreateEntryFromFile(String.Format("{0}//manifest.json", ModFolderPath), "manifest.json");
                }
            }

            //.. Open package folder
            ProcessStartInfo CmdPI = new ProcessStartInfo
            {
                Arguments = String.Format("/C start {0}", ModFolderPath),
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe"
            };

            Process.Start(CmdPI);
        }

        //-- Delete previously loaded projects on close
        private void ThunderPackerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!Directory.Exists(@"Extracted")) { return; }
            Directory.Delete(@"Extracted", true);
        }

        //-- Load previous project
        private void b_LoadPrevProject_Click(object sender, EventArgs e)
        {
            //.. Create new directory
            ThunderPackerForm_FormClosed(null, null);

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select Mod Zip File";
            ofd.Filter = "zip (*.zip)|*.zip";
            ofd.FilterIndex = 1;
            ofd.Multiselect = false;
            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                Directory.CreateDirectory(@"Extracted");
                ZipFile.ExtractToDirectory(ofd.FileName, @"Extracted");
            }

            //.. Set misc information
            b_ModIcon.Text = "Select icon (*)";
            IconPath = "Extracted//icon.png";

            //.. Get manifest information
            StreamReader Reader = new StreamReader("Extracted//manifest.json");
            string JsonContent = Reader.ReadToEnd();
            ModManifest Content = JsonConvert.DeserializeObject<ModManifest>(JsonContent);
            Reader.Close();

            //.. Autofill readme text
            tb_ReadmeEditor.Text = File.ReadAllText("Extracted//README.md");

            //.. Autofill dependencies
            string[] Dependencies = Content.Dependencies;
            
            foreach (string Dep in Dependencies) 
            {
                tb_ModDeps.AppendText(Dep);
            }
            
            //.. Autofill name/description
            tb_ModDesc.Text = Content.Description;
            tb_ModName.Text = Content.Name;
        }

        //-- Select mod icon (*.png)
        private void b_ModIcon_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select Mod Icon File";
            ofd.Filter = "PNG (*.png)|*.png";
            ofd.FilterIndex = 1;
            ofd.Multiselect = false;
            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                b_ModIcon.Text = "Select File (*)";
                IconPath = ofd.FileName;
            }
        }

        //-- Select actual mod (*.dll)
        private void b_SelectMod_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select Mod File";
            ofd.Filter = "DLL File (*.dll)|*.dll";
            ofd.FilterIndex = 1;
            ofd.Multiselect = false;
            ofd.ShowDialog();

            if (ofd.FileName != "")
            {
                b_SelectMod.Text = "Select File (*)";
                ModPath = ofd.FileName;

                //.. Autofill information
                if (cb_Autofill.Checked)
                {
                    Assembly Mod = Assembly.LoadFrom(ModPath);

                    try { tb_ModName.Text = Mod.GetName().Name; }
                    catch (Exception) { tb_ModName.Text = "N/A"; }

#pragma warning disable CS8602 // already handled null possibility
                    try { Author = Mod.GetCustomAttribute<MelonInfoAttribute>().Author; }
                    catch (Exception) { Author = "ZippedMod"; }

#pragma warning disable CS8602 // already handled null possibility
                    try { tb_ModVersion.Text = Mod.GetCustomAttribute<MelonInfoAttribute>().Version; }
                    catch (Exception) { tb_ModVersion.Text = "N/A"; }

#pragma warning disable CS8602 // already handled null possibility
                    try { tb_ModWebsite.Text = Mod.GetCustomAttribute<MelonInfoAttribute>().DownloadLink; }
                    catch (Exception) { tb_ModWebsite.Text = "N/A"; }
                    if (tb_ModWebsite.Text == "") { tb_ModWebsite.Text = "N/A"; }
                }
            }
        }

        //-- Toggle debug button
        private void cb_DebugMode_CheckedChanged(object sender, EventArgs e)
        {
            b_DebugButton.Visible = cb_DebugMode.Checked;
        }

        //-- Debug function
        private void b_DebugButton_Click(object sender, EventArgs e)
        {
            //.. Default debug msg
            RejectProcess("This is an up to date build :: 6");

            //.. Debug zone
        }
    }

    public class ModManifest 
    { 
        public string Name { get; set; }
        public string VersionNumber { get; set; }
        public string WebsiteURL { get; set; }
        public string Description { get; set; }
        public string[] Dependencies { get; set; }
    }
}