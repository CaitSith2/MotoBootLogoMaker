// ReSharper disable EmptyGeneralCatchClause
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Moto_Logo.Properties;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Ionic.Zip;


namespace Moto_Logo
{
    public partial class Form1 : Form
    {
        private bool _fileSaved;
        private int _maxFileSize = 4*1024*1024; //4MiB
        private readonly List<String> _loadedbitmapames = new List<string>(); 
        private readonly List<Bitmap> _loadedbitmaps = new List<Bitmap>();

        private readonly List<int> _deviceResolutionX = new List<int>();
        private readonly List<int> _deviceResolutionY = new List<int>();
        private readonly List<int> _deviceLogoBinSize = new List<int>();
        private readonly List<UInt32> _deviceLogoBinContents = new List<UInt32>();

        private Image FixedSizePreview(Image imgPhoto)
        {
            return FixedSize(!rdoAndroid44.Checked ? FixedSize(imgPhoto, 540, 540) : imgPhoto, 
                (int) udResolutionX.Value, (int) udResolutionY.Value,
                !rdoAndroid44.Checked);
        }

        private Bitmap FixedSizeSave(Image imgPhoto)
        {
            var xmax = rdoAndroid44.Checked ? (int)udResolutionX.Value : 540;
            var ymax = rdoAndroid44.Checked ? (int)udResolutionY.Value : 540;
            return (rdoImageCenter.Checked && (imgPhoto.Width <= xmax) 
                    && (imgPhoto.Height <= ymax) && rdoAndroid44.Checked)
                        ? (Bitmap)imgPhoto
                        : FixedSize(imgPhoto,xmax,ymax);
        }

        private Bitmap FixedSize(Image imgPhoto, int imgWidth, int imgHeight, bool forceCenter = false)
        {
            var sourceWidth = imgPhoto.Width;
            var sourceHeight = imgPhoto.Height;
            const int sourceX = 0;
            const int sourceY = 0;
            var destX = 0;
            var destY = 0;

            float nPercent = 0;


// ReSharper disable RedundantCast
            var nPercentW = ((float)imgWidth / (float)sourceWidth);
            var nPercentH = ((float)imgHeight / (float)sourceHeight);
// ReSharper restore RedundantCast

            if (((sourceWidth <= imgWidth) && (sourceHeight <= imgHeight)) && (rdoImageCenter.Checked || forceCenter))
            {
                nPercent = 1.0f;
                destX = (imgWidth - sourceWidth)/2;
                destY = (imgHeight - sourceHeight)/2;
            }
            else if ((nPercentH < nPercentW) && (!rdoImageFill.Checked || forceCenter))
            {
                nPercent = nPercentH;
                destX = Convert.ToInt16((imgWidth -
                              (sourceWidth * nPercent)) / 2);
            }
            else if (!rdoImageFill.Checked || forceCenter)
            {
                nPercent = nPercentW;
                destY = Convert.ToInt16((imgHeight -
                              (sourceHeight * nPercent)) / 2);
            }

            var destWidth = (int)(sourceWidth * ((rdoImageFill.Checked && !forceCenter) ? nPercentW : nPercent));
            var destHeight = (int)(sourceHeight * ((rdoImageFill.Checked && !forceCenter) ? nPercentH : nPercent));

            var bmPhoto = new Bitmap(imgWidth, imgHeight,
                              PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution,
                             imgPhoto.VerticalResolution);

            var grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(((Bitmap) imgPhoto).GetPixel(0, 0));
            grPhoto.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(imgPhoto,
                new Rectangle(destX, destY, destWidth, destHeight),
                new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                GraphicsUnit.Pixel);

            grPhoto.Dispose();
            return bmPhoto;
        }


        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ReSharper disable InconsistentNaming
        [Flags]
        private enum LOGO
        {
            LOGO_RAW = 0,
            LOGO_BOOT = 1,
            LOGO_BATTERY = 2,
            LOGO_UNLOCKED = 4,
            LOGO_LOWPOWER = 8,
            LOGO_UNPLUG = 0x10,
            LOGO_CHARGE = 0x20,
            KITKAT_DISABLED = 0x40000000
        };
        // ReSharper restore InconsistentNaming


        private void init_tree(UInt32 logobincontents)
        {
            if (logobincontents == (int) LOGO.LOGO_RAW)
            {
                init_tree(false, false, true, false, false, false);
                rdoAndroid43.Enabled = false;
                rdoAndroid44.Enabled = false;
                rdoAndroidRAW.Checked = true;
                return;
            }
            var enableKitkat = ((logobincontents & (int)LOGO.KITKAT_DISABLED) == 0);
            rdoAndroid43.Enabled = true;
            rdoAndroid44.Enabled = enableKitkat;
            if (enableKitkat) rdoAndroid44.Checked = true;
            else rdoAndroid43.Checked = true;
            init_tree((logobincontents & (UInt32)LOGO.LOGO_BOOT) == (UInt32)LOGO.LOGO_BOOT,
                (logobincontents & (UInt32)LOGO.LOGO_BATTERY) == (UInt32)LOGO.LOGO_BATTERY,
                (logobincontents & (UInt32)LOGO.LOGO_UNLOCKED) == (UInt32)LOGO.LOGO_UNLOCKED,
                (logobincontents & (UInt32)LOGO.LOGO_LOWPOWER) == (UInt32)LOGO.LOGO_LOWPOWER,
                (logobincontents & (UInt32)LOGO.LOGO_UNPLUG) == (UInt32)LOGO.LOGO_UNPLUG,
                (logobincontents & (UInt32)LOGO.LOGO_CHARGE) == (UInt32)LOGO.LOGO_CHARGE);
        }

        private void init_tree(bool logoboot, bool logobattery, bool logounlocked, bool logolowpower, bool logounplug, bool logocharge)
        {
            var logoBoot = false;
            var logoBattery = false;
            var logoUnlocked = false;
            var logoLowpower = false;
            var logoUnplug = false;
            var logoCharge = false;
            for (var index = tvLogo.Nodes.Count - 1; index >= 0; index--)
            {
                var node = tvLogo.Nodes[index];
                var removenode = (cboMoto.SelectedIndex > 0);
                switch (node.Text)
                {
                        
                    case "logo_boot":
                        if (logoboot)
                            logoBoot = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                    case "logo_battery":
                        if(logobattery)
                            logoBattery = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                    case "logo_unlocked":
                        if(logounlocked)
                            logoUnlocked = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                    case "logo_lowpower":
                        if(logolowpower)
                            logoLowpower = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                    case "logo_unplug":
                        if(logounplug)
                            logoUnplug = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                    case "logo_charge":
                        if (logocharge)
                            logoCharge = true;
                        else removenode = (cboMoto.SelectedIndex > 0);
                        break;
                }
                if(removenode)
                    node.Remove();
            }
            if (!logoBoot && logoboot) tvLogo.Nodes.Add("logo_boot");
            if (!logoBattery && logobattery) tvLogo.Nodes.Add("logo_battery");
            if (!logoUnlocked && logounlocked) tvLogo.Nodes.Add("logo_unlocked");
            if (!logoLowpower && logolowpower) tvLogo.Nodes.Add("logo_lowpower");
            if (!logoUnplug && logounplug) tvLogo.Nodes.Add("logo_unplug");
            if (!logoCharge && logocharge) tvLogo.Nodes.Add("logo_charge");
            for (var index = tvLogo.Nodes.Count - 1; index >= 0; index--)
            {
                var node = tvLogo.Nodes[index];
                switch (node.Text)
                {

                    case "logo_boot":
                        node.ToolTipText = "Visible only with boot-loader locked phone.  It is suggested you remove" +
                                           " the picture that is in this entry, to save bytes in your logo.bin";
                        break;
                    case "logo_battery":
                        node.ToolTipText = "Visible when your phone has had its battery fully discharged, and you " +
                                           "plug your phone in to charge";
                        break;
                    case "logo_unlocked":
                        node.ToolTipText =
                            "Visible on boot-loader unlocked phones. What you put here is likely to look" +
                            " much better than the unlocked device warning. :)";
                        break;
                    case "logo_lowpower":
                        
                        break;
                    case "logo_unplug":
                        
                        break;
                    case "logo_charge":
                        break;
                }
            }

            
        }

        private void udResolutionX_ValueChanged(object sender, EventArgs e)
        {
            tvLogo_AfterSelect(sender, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if ((cboMoto.SelectedIndex > 0) && button1.Text == Resources.Append) return;
            if (txtLogoInternalFile.Text == "") return;

            if (button1.Text == Resources.Append)
                switch (txtLogoInternalFile.Text)
                {
                    case "logo_boot":
                        init_tree(true, false, false, false, false,false);
                        break;
                    case "logo_battery":
                        init_tree(false, true, false, false, false, false);
                        break;
                    case "logo_unlocked":
                        init_tree(false, false, true, false, false, false);
                        break;
                    case "logo_lowpower":
                        init_tree(false, false, false, true, false, false);
                        break;
                    case "logo_unplug":
                        init_tree(false, false, false, false, true, false);
                        break;
                    case "logo_charge":
                        init_tree(false, false, false, false, false, true);
                        break;
                }

            openFileDialog1.Filter = Resources.SelectImageFile;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var img = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialog1.FileName)));
                if (_loadedbitmaps.IndexOf(img) == -1)
                {
                    _loadedbitmaps.Add(img);
                    _loadedbitmapames.Add(Path.GetFileName(openFileDialog1.FileName));
                }
                var nodeFound = false;
                foreach (var node in tvLogo.Nodes.Cast<TreeNode>().Where(node => node.Text == txtLogoInternalFile.Text))
                {
                    node.Name = _loadedbitmaps.IndexOf(img).ToString();
                    nodeFound = true;
                }
                if (!nodeFound) tvLogo.Nodes.Add(_loadedbitmaps.IndexOf(img).ToString(), txtLogoInternalFile.Text);
                toolStripStatusLabel1.Text = openFileDialog1.FileName;
            }
            else
            {
                var nodeFound = false;
                foreach (var node in tvLogo.Nodes.Cast<TreeNode>().Where(node => node.Text == txtLogoInternalFile.Text))
                {
                    node.Name = "";
                    nodeFound = true;
                }
                if (!nodeFound) tvLogo.Nodes.Add(txtLogoInternalFile.Text);
            }
            button1.Text = Resources.Replace;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if((cboMoto.SelectedIndex == 0))
                tvLogo.SelectedNode.Remove();
            else
            {
                tvLogo.SelectedNode.Name = "";
                if (tvLogo.Nodes.Count == 0)
                {
                    _loadedbitmaps.Clear();
                    _loadedbitmapames.Clear();
                }
            }
        }

        private void tvLogo_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                var bitmap = File.Exists(tvLogo.SelectedNode.Name) 
                    ? new Bitmap(new MemoryStream(File.ReadAllBytes(tvLogo.SelectedNode.Name)))
                    : _loadedbitmaps[Convert.ToInt32(tvLogo.SelectedNode.Name)];
                if (bitmap == null) return;
                pictureBox1.Image = FixedSizePreview(bitmap);
                toolStripStatusLabel1.Text = File.Exists(tvLogo.SelectedNode.Name) 
                    ? Path.GetFileName(tvLogo.SelectedNode.Name)
                    : _loadedbitmapames[Convert.ToInt32(tvLogo.SelectedNode.Name)]
                    + @": " + bitmap.Width + @"x" + bitmap.Height;
                Application.DoEvents();
            }
            catch
            {
                pictureBox1.Image = new Bitmap(1, 1);
                toolStripStatusLabel1.Text = "";
                Application.DoEvents();
            }
            
        }

        private void tvLogo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            openFileDialog1.Filter = Resources.SelectImageFile;
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            var img = new Bitmap(new MemoryStream(File.ReadAllBytes(openFileDialog1.FileName)));
            if (_loadedbitmaps.IndexOf(img) == -1)
            {
                _loadedbitmaps.Add(img);
                _loadedbitmapames.Add(Path.GetFileName(openFileDialog1.FileName));
            }
            tvLogo.SelectedNode.Name = _loadedbitmaps.IndexOf(img).ToString();
            toolStripStatusLabel1.Text = openFileDialog1.FileName;
            Application.DoEvents();
            tvLogo_AfterSelect(sender, null);
        }

        private void txtLogoInternalFile_TextChanged(object sender, EventArgs e)
        {
            button1.Text = tvLogo.Nodes.Cast<TreeNode>().Any(node => node.Text == txtLogoInternalFile.Text) 
                ? Resources.Replace 
                : Resources.Append;
        }

        private static byte[] ExtractLogoBin(string zipfilename)
        {
            byte[] buffer = null;
            using (var input = new ZipInputStream(zipfilename))
            {
                ZipEntry e;
                while ((e = input.GetNextEntry()) != null)
                {
                    if (e.IsDirectory) continue;
                    if (Path.GetFileName(e.FileName) != "logo.bin") continue;
                    buffer = new byte[e.UncompressedSize];
                    input.Read(buffer, 0, buffer.Length);
                    break;
                }
            }
            return buffer;
        }

        private void OpenFile(string filename)
        {
            var zipFile = false;
            var openfilename = filename;
            byte[] logobin = null;

            try
            {
                ProgressBar.Visible = true;
                ProgressBar.Value = 0;
                Application.DoEvents();
                if (ZipFile.IsZipFile(filename))
                {
                    zipFile = true;
                    if ((logobin = ExtractLogoBin(filename)) == null)
                    {
                        toolStripStatusLabel1.Text = @"Error: Zip file " + filename +
                                                     @" Doesn't contain logo.bin";
                        Application.DoEvents();
                        ProgressBar.Visible = false;
                        return;
                    }
                    
                }
                Stream stream;

                try
                {
                    if (zipFile)
                        stream = new MemoryStream(logobin);
                    else
                        stream = new FileStream(openfilename, FileMode.Open);
                }
                catch (Exception)
                {
                    ProgressBar.Visible = false;
                    toolStripStatusLabel1.Text = @"Error Opening file: " + filename;
                    return;
                }


                using (var reader = new BinaryReader(stream))
                {
                    pictureBox1.Image = new Bitmap(1, 1);
                    _fileSaved = false;
                    var android43 = false;
                    cboMoto.SelectedIndex = 0;
                    rdoAndroid44.Checked = true;
                    udResolutionX.Value = 720;
                    udResolutionY.Value = 1280;
                    tvLogo.Nodes.Clear();
                    _loadedbitmaps.Clear();
                    _loadedbitmapames.Clear();
                    Bitmap img;
                    if ((reader.ReadInt64() != 0x6F676F4C6F746F4DL) || (reader.ReadByte() != 0x00))
                    {
                        if (reader.BaseStream.Length != 0xD5930)
                        {
                            toolStripStatusLabel1.Text = @"Invalid logo.bin file loaded";
                            ProgressBar.Visible = false;
                            return;
                        }
                        reader.BaseStream.Position = 0;
                        rdoAndroidRAW.Checked = true;

                        ProgressBar.Maximum = 540 * 540;
                        img = new Bitmap(540, 540, PixelFormat.Format24bppRgb);
                        for (var y = 0; y < 540; y++)
                        {
                            try
                            {
                                ProgressBar.Value += 540;
                                Application.DoEvents();
                            }
                            catch { }
                            for (var x = 0; x < 540; x++)
                            {
                                var blue = reader.ReadByte();
                                var green = reader.ReadByte();
                                var red = reader.ReadByte();
                                img.SetPixel(x, y,
                                    Color.FromArgb(red, green, blue));
                            }
                        }
                        if (_loadedbitmaps.IndexOf(img) == -1)
                        {
                            _loadedbitmaps.Add(img);
                            _loadedbitmapames.Add(Path.GetFileName(filename) +
                                                  (zipFile ? @"\logo.bin\logo_unlocked" : @"\logo_unlocked"));
                        }
                        tvLogo.Nodes.Add(_loadedbitmaps.IndexOf(img).ToString(), "logo_unlocked");
                        toolStripStatusLabel1.Text = @"Processing Complete :)";
                        ProgressBar.Visible = false;
                        return;
                    }
                    var count = (reader.ReadInt32() - 0x0D) / 0x20;
                    var name = new string[count];
                    var offset = new Int32[count];
                    var size = new Int32[count];
                    for (var i = 0; i < count; i++)
                    {
                        reader.BaseStream.Position = 0x0D + (0x20 * i);
                        name[i] = Encoding.ASCII.GetString(reader.ReadBytes(0x18)).Split('\0')[0];
                        offset[i] = reader.ReadInt32();
                        size[i] = reader.ReadInt32();
                    }
                    reader.BaseStream.Position = offset[0];
                    if (reader.ReadInt64() != 0x006E75526F746F4DL)
                    {
                        android43 = true;
                        rdoAndroid43.Checked = true;
                    }
                    for (var i = 0; i < count; i++)
                    {
                        toolStripStatusLabel1.Text = @"Processing " + name[i];
                        ProgressBar.Value = 0;
                        Application.DoEvents();

                        if (!android43)
                        {
                            reader.BaseStream.Position = offset[i] + 8;
                            var x = (UInt16)(reader.ReadByte() << 8);
                            x |= reader.ReadByte();
                            var y = (UInt16)(reader.ReadByte() << 8);
                            y |= reader.ReadByte();
                            img = new Bitmap(x, y, PixelFormat.Format24bppRgb);
                            var xx = 0;
                            var yy = 0;
                            ProgressBar.Maximum = x * y;
                            Application.DoEvents();
                            while (yy < y)
                            {
                                var pixelcount = (UInt16)(reader.ReadByte() << 8);
                                pixelcount |= reader.ReadByte();
                                var repeat = (pixelcount & 0x8000) == 0x8000;
                                pixelcount &= 0x7FFF;

                                int red, green, blue;

                                if (repeat)
                                {
                                    blue = reader.ReadByte();
                                    green = reader.ReadByte();
                                    red = reader.ReadByte();
                                    while (pixelcount-- > 0)
                                    {
                                        img.SetPixel(xx++, yy,
                                            Color.FromArgb(red, green, blue));
                                        if (xx != x) continue;
                                        try { ProgressBar.Value += x; }
                                        catch { }
                                        Application.DoEvents();
                                        xx = 0;
                                        yy++;
                                    }
                                }
                                else
                                {
                                    while (pixelcount-- > 0)
                                    {
                                        blue = reader.ReadByte();
                                        green = reader.ReadByte();
                                        red = reader.ReadByte();
                                        img.SetPixel(xx++, yy,
                                            Color.FromArgb(red, green, blue));
                                        if (xx != x) continue;
                                        try { ProgressBar.Value += x; }
                                        catch { }
                                        Application.DoEvents();
                                        xx = 0;
                                        yy++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            reader.BaseStream.Position = offset[i];
                            ProgressBar.Maximum = 540 * 540;
                            img = new Bitmap(540, 540, PixelFormat.Format24bppRgb);
                            for (var y = 0; y < 540; y++)
                            {
                                try
                                {
                                    ProgressBar.Value += 540;
                                    Application.DoEvents();
                                }
                                catch { }
                                for (var x = 0; x < 540; x++)
                                {
                                    var blue = reader.ReadByte();
                                    var green = reader.ReadByte();
                                    var red = reader.ReadByte();
                                    img.SetPixel(x, y,
                                        Color.FromArgb(red, green, blue));
                                }
                            }
                        }
                        try
                        {
                            if (_loadedbitmaps.IndexOf(img) == -1)
                            {
                                _loadedbitmaps.Add(img);
                                _loadedbitmapames.Add(Path.GetFileName(filename) +
                                                      (zipFile ? @"\logo.bin\" : @"\") + name[i]);
                            }
                            tvLogo.Nodes.Add(_loadedbitmaps.IndexOf(img).ToString(), name[i]);

                        }
                        catch
                        {
                            tvLogo.Nodes.Add(name[i]);
                        }


                    }

                }
            }
            catch (Exception ex)
            {
                ProgressBar.Visible = false;
                toolStripStatusLabel1.Text = @"Exception: " + ex.GetBaseException();
                return;
            }

            toolStripStatusLabel1.Text = @"File Load Complete :)";
            ProgressBar.Visible = false;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = @"Logo Files|*.zip;*.bin|Bin Files|*.bin|Flashable Zip files|*.zip|All Files|*.*";
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            OpenFile(openFileDialog1.FileName);
        }
        
        private void SaveFile()
        {
            var stream = new MemoryStream();
            var errorCount = 0;
            var blankCount = 0;
            var errorproceed = false;
            var fileext = Path.GetExtension(saveFileDialog1.FileName);

            using (var writer = new BinaryWriter(stream))
            {
                ProgressBar.Visible = true;
                ProgressBar.Minimum = 0;
                if (rdoAndroidRAW.Checked)
                {
                    ProgressBar.Maximum = 540*540;
                    Bitmap img;
                    try
                    {
                        img = FixedSizeSave(File.Exists(tvLogo.Nodes[0].Name)
                            ? new Bitmap(new MemoryStream(File.ReadAllBytes(tvLogo.Nodes[0].Name)))
                            : _loadedbitmaps[Convert.ToInt32(tvLogo.Nodes[0].Name)]);

                    }
                    catch
                    {
                        if (tvLogo.Nodes[0].Name != "")
                        {
                            toolStripStatusLabel1.Text = @"Error loading image - Processing Aborted :(";
                            ProgressBar.Visible = false;
                            writer.Close();
                            return;
                        }
                        if (MessageBox.Show(@"Are you sure you wish to proceed with a blank image?",
                            @"Motorola Boot Logo Maker",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question,
                            MessageBoxDefaultButton.Button2) == DialogResult.No)
                            {
                                toolStripStatusLabel1.Text = @"Processing Aborted";
                                ProgressBar.Visible = false;
                                writer.Close();
                                return;
                            }
                        img = FixedSizeSave(new Bitmap(100, 100));
                        var grPhoto = Graphics.FromImage(img);
                        grPhoto.Clear(Color.White);
                        grPhoto.Dispose();
                    }
                    Application.DoEvents();
                    for (var y = 0; y < 540; y++)
                    {
                        try
                        {
                            ProgressBar.Value += 540;
                            Application.DoEvents();
                        }
                        catch
                        {
                        }
                        for (var x = 0; x < 540; x++)
                        {
                            writer.Write(img.GetPixel(x, y).B);
                            writer.Write(img.GetPixel(x, y).G);
                            writer.Write(img.GetPixel(x, y).R);
                        }
                    }

                }
                else
                {
                    writer.Write(0x6F676F4C6F746F4DL);
                    writer.Write((byte) 0);
                    writer.Write(0x0D + (tvLogo.Nodes.Count*0x20));
                    var android43 = rdoAndroid43.Checked;
                    for (var i = 0; i < tvLogo.Nodes.Count; i++)
                    {
                        writer.BaseStream.Position = 0x0D + (i*0x20);
                        var name = Encoding.ASCII.GetBytes(tvLogo.Nodes[i].Text);
                        writer.Write(name);
                        writer.Write(new byte[0x20 - name.Length]);
                    }
                    for (var i = 0; i < tvLogo.Nodes.Count; i++)
                    {
                        while ((writer.BaseStream.Position%0x200) != 0)
                            writer.Write((byte) 0xFF);
                        writer.BaseStream.Position = 0x0D + 0x18 + (i*0x20);
                        writer.Write((int) writer.BaseStream.Length);
                        writer.BaseStream.Position = writer.BaseStream.Length;
                        Bitmap img;
                        try
                        {
                            img = FixedSizeSave(File.Exists(tvLogo.Nodes[i].Name)
                                ? new Bitmap(new MemoryStream(File.ReadAllBytes(tvLogo.Nodes[i].Name)))
                                : _loadedbitmaps[Convert.ToInt32(tvLogo.Nodes[i].Name)]);
                            if (!errorproceed && (errorCount > 0))
                            {
                                if (MessageBox.Show(@"At least one image failed to load, " +
                                    @"are you sure you wish to proceed?",
                                    @"Motorola Boot Logo Maker",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button2) == DialogResult.No)
                                    {
                                        toolStripStatusLabel1.Text = @"Processing Aborted";
                                        ProgressBar.Visible = false;
                                        writer.Close();
                                        return;
                                    }
                                errorproceed = true;
                            }
                        }
                        catch
                        {
                            if (tvLogo.Nodes[0].Name != "")
                                errorCount++;
                            else
                                blankCount++;
                            if ((blankCount == tvLogo.Nodes.Count) &&
                               (MessageBox.Show(@"No images were loaded, are you sure you wish to" +
                               @" proceed with blank images?",
                               @"Motorola Boot Logo Maker",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question,
                               MessageBoxDefaultButton.Button2) == DialogResult.No))
                            {
                                toolStripStatusLabel1.Text = @"Processing Aborted";
                                ProgressBar.Visible = false;
                                writer.Close();
                                return;
                            }
                            if ((errorCount + blankCount) == tvLogo.Nodes.Count)
                            {
                                toolStripStatusLabel1.Text = @"Every single image selected failed to load"+
                                                             @" - Processing Aborted :(";
                                ProgressBar.Visible = false;
                                writer.Close();
                                return;
                            }
                            img = FixedSizeSave(new Bitmap(100, 100));
                            var grPhoto = Graphics.FromImage(img);
                            grPhoto.Clear(Color.White);
                            grPhoto.Dispose();
                        }
                        toolStripStatusLabel1.Text = @"Processing " + tvLogo.Nodes[i].Text;
                        ProgressBar.Value = 0;
                        Application.DoEvents();
                        ProgressBar.Maximum = img.Width*img.Height;
                        var size = 0;
                        if (android43)
                        {
                            size = 0xD5930;
                            Application.DoEvents();
                            for (var y = 0; y < 540; y++)
                            {
                                try
                                {
                                    ProgressBar.Value += 540;
                                    Application.DoEvents();
                                }
                                catch
                                {
                                }
                                for (var x = 0; x < 540; x++)
                                {
                                    writer.Write(img.GetPixel(x, y).B);
                                    writer.Write(img.GetPixel(x, y).G);
                                    writer.Write(img.GetPixel(x, y).R);
                                }
                            }
                            while ((writer.BaseStream.Position%0x200) != 0)
                                writer.Write((byte) 0xFF);
                        }
                        else
                        {
                            writer.Write(0x006E75526F746F4DL);
                            writer.Write((byte) (img.Width >> 8));
                            writer.Write((byte) (img.Width & 0xFF));
                            writer.Write((byte) (img.Height >> 8));
                            writer.Write((byte) (img.Height & 0xFF));
                            size += 12;

                            for (var y = 0; y < img.Height; y++)
                            {
                                try
                                {
                                    ProgressBar.Value += img.Width;
                                }
                                catch
                                {
                                }
                                Application.DoEvents();
                                var colors = new int[img.Width];
                                for (var x = 0; x < img.Width; x++)
                                {
                                    colors[x] = img.GetPixel(x, y).R << 16;
                                    colors[x] |= img.GetPixel(x, y).G << 8;
                                    colors[x] |= img.GetPixel(x, y).B;
                                }
                                var j = 0;
                                while (j < img.Width)
                                {
                                    var k = j;
                                    while ((k < img.Width) && (colors[j] == colors[k]))
                                    {
                                        k++;
                                    }
                                    if ((k - j) > 1)
                                    {
                                        writer.Write((byte) (0x80 | ((k - j) >> 8)));
                                        writer.Write((byte) ((k - j) & 0xFF));
                                        writer.Write((byte) (colors[j] & 0xFF));
                                        writer.Write((byte) ((colors[j] >> 8) & 0xFF));
                                        writer.Write((byte) ((colors[j] >> 16) & 0xFF));
                                        size += 5;
                                        j = k;
                                    }
                                    else
                                    {
                                        var l = k;
                                        k = j;
                                        while ((l < img.Width) && (colors[k] != colors[l]))
                                        {
                                            k++;
                                            l++;
                                        }
                                        if ((k - j) == 0)
                                        {
                                            writer.Write((byte) 0);
                                            writer.Write((byte) 1);
                                            writer.Write((byte) (colors[img.Width - 1] & 0xFF));
                                            writer.Write((byte) ((colors[img.Width - 1] >> 8) & 0xFF));
                                            writer.Write((byte) ((colors[img.Width - 1] >> 16) & 0xFF));
                                            size += 5;
                                            break;
                                        }
                                        writer.Write((byte) ((k - j) >> 8));
                                        writer.Write((byte) ((k - j) & 0xFF));
                                        size += 2;
                                        for (l = 0; l < (k - j); l++)
                                        {
                                            writer.Write((byte) (colors[j + l] & 0xFF));
                                            writer.Write((byte) ((colors[j + l] >> 8) & 0xFF));
                                            writer.Write((byte) ((colors[j + l] >> 16) & 0xFF));
                                            size += 3;
                                        }
                                        j = k;
                                    }
                                }
                            }
                        }

                        writer.BaseStream.Position = 0x0D + (i*0x20) + 0x1C;
                        writer.Write(size);
                        writer.BaseStream.Position = writer.BaseStream.Length;
                        if (writer.BaseStream.Length > _maxFileSize)
                        {
                            ProgressBar.Visible = false;
                            toolStripStatusLabel1.Text =
                                @"Error: Images/options selected will not fit in logo.bin, Failed at " +
                                tvLogo.Nodes[i].Text + @" Produced file is " +
                                (writer.BaseStream.Length - _maxFileSize) + @" Bytes Too Large";
                            return;
                        }
                    }
                }
            }

            if (fileext == ".zip")
            {
                var zipfilename = saveFileDialog1.FileName;

                using (var zip = new ZipFile())
                {
                    zip.AddEntry("logo.bin", stream.ToArray());
                    zip.AddDirectoryByName("META-INF");
                    zip.AddDirectoryByName("META-INF\\com");
                    zip.AddDirectoryByName("META-INF\\com\\google");
                    zip.AddDirectoryByName("META-INF\\com\\google\\android");

                    zip.AddEntry("META-INF\\com\\google\\android\\updater-script", Resources.updater_script);
                    zip.AddEntry("META-INF\\com\\google\\android\\update-binary", Resources.update_binary);
                    zip.Save(zipfilename);
                }
            }
            else
                File.WriteAllBytes(saveFileDialog1.FileName, stream.ToArray());

            ProgressBar.Visible = false;
            toolStripStatusLabel1.Text = @"Processing Complete :)";
            _fileSaved = true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
            Application.DoEvents();
            saveFileDialog1.Filter = Resources.ZipBins;
            if ((_fileSaved) || (saveFileDialog1.ShowDialog() == DialogResult.OK))
                try
                {
                    SaveFile();
                }
                catch (Exception ex)
                {
                    ProgressBar.Visible = false;
                    toolStripStatusLabel1.Text = @"Exception during processing: " + ex.GetBaseException();
                }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
            Application.DoEvents();
            saveFileDialog1.Filter = Resources.ZipBins;
            if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
            try
            {
                SaveFile();
            }
            catch (Exception ex)
            {
                ProgressBar.Visible = false;
                toolStripStatusLabel1.Text = @"Exception during processing: " + ex.GetBaseException();
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _loadedbitmaps.Clear();
            _loadedbitmapames.Clear();
            _fileSaved = false;
            rdoAndroid44.Checked = true;
            cboMoto.SelectedIndex = 4;
            tvLogo.Nodes.Clear();
            cboMoto_SelectedIndexChanged(sender,e);
            toolStripStatusLabel1.Text = "";
            Application.DoEvents();
            pictureBox1.Image = new Bitmap(1, 1);
            rdoImageCenter.Checked = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Init_cboMoto("Custom",720,1280,4194304,0x3FFFFFFF);
            Init_cboMoto("Moto G 4G/LTE", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_CHARGE));
            Init_cboMoto("Moto E", 540,960,4194304,(int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED | LOGO.LOGO_LOWPOWER | LOGO.LOGO_UNPLUG));
            Init_cboMoto("Moto X", 720,1280,4194304,(int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED));
            Init_cboMoto("Moto G", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED));
            Init_cboMoto("Droid Ultra", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_BATTERY | LOGO.LOGO_UNLOCKED));
            Init_cboMoto("Droid RAZR HD", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("RAZR i", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Droid RAZR M", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Photon Q 4G LTE", 540, 960, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Atrix HD", 720, 1280, 4194304, (int)(LOGO.LOGO_BOOT | LOGO.LOGO_UNLOCKED | LOGO.KITKAT_DISABLED));
            Init_cboMoto("Droid 4", 540,960,1048576,(int)LOGO.LOGO_RAW);
            Init_cboMoto("Atrix 2", 540, 960, 1048576, (int)LOGO.LOGO_RAW);
            Init_cboMoto("Droid RAZR", 540, 960, 1048576, (int)LOGO.LOGO_RAW);
            Init_cboMoto("Photon 4G", 540, 960, 1048576, (int)LOGO.LOGO_RAW);

            newToolStripMenuItem_Click(sender, e);
        }

        private void rdoAndroid43_CheckedChanged(object sender, EventArgs e)
        {
            tvLogo_AfterSelect(sender, null);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox1().ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Bitmap img;
            try
            {
                img = File.Exists(tvLogo.SelectedNode.Name)
                    ? new Bitmap(new MemoryStream(File.ReadAllBytes(tvLogo.SelectedNode.Name)))
                    : _loadedbitmaps[Convert.ToInt32(tvLogo.SelectedNode.Name)];
            }
            catch (Exception)
            {

                return;
            }
            saveFileDialog2.Filter = @"Png file|*.png|Jpeg file|*.jpg|Bitmap File|*.bmp|Gif file|*.gif|All Files|*.*";
            if (saveFileDialog2.ShowDialog() != DialogResult.OK) return;
            try
            {
                

                switch (Path.GetExtension(saveFileDialog2.FileName))
                {
                    case ".gif":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Gif);
                        break;
                    case ".jpg":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Jpeg);
                        break;
                    case ".bmp":
                        img.Save(saveFileDialog2.FileName, ImageFormat.Bmp);
                        break;
                    default:
                        img.Save(saveFileDialog2.FileName, ImageFormat.Png);
                        break;

                }
                toolStripStatusLabel1.Text = @"Image saved as " + Path.GetFileName(saveFileDialog2.FileName) +
                                             @" Successfully :)";
                Application.DoEvents();
            }
            catch (Exception)
            {
                toolStripStatusLabel1.Text = @"Unable to Extract Image from bootlogo :(";
                Application.DoEvents();
            }
        }

        private void rdoAndroidRAW_CheckedChanged(object sender, EventArgs e)
        {
            if (rdoAndroidRAW.Checked)
            {
                if (tvLogo.Nodes.Count <= 1) return;
                for(var i = tvLogo.Nodes.Count-1;i >= 0; i--)
                {
                    if (tvLogo.Nodes[i].Text == @"logo_unlocked") continue;
                    if(tvLogo.Nodes.Count > 1)
                        tvLogo.Nodes[i].Remove();
                }
            }
            else
            {
                cboMoto_SelectedIndexChanged(sender, e);
            }
            tvLogo_AfterSelect(sender, null);
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if((files.Count() == 1) && ((Path.GetExtension(files[0]) == ".bin") || 
                                        (Path.GetExtension(files[0]) == ".zip")))
                e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            if ((files.Count() == 1) && ((Path.GetExtension(files[0]) == ".bin") ||
                                         (Path.GetExtension(files[0]) == ".zip")))
            {
                OpenFile(files[0]);
            }
        }

        private void udResolutionY_ValueChanged(object sender, EventArgs e)
        {
            tvLogo_AfterSelect(sender, null);
        }

        private void Init_cboMoto(string device, int resolutionX, int resolutionY, int logobinsize, UInt32 logoContents)
        {
            cboMoto.Items.Add(device);
            _deviceResolutionX.Add(resolutionX);
            _deviceResolutionY.Add(resolutionY);
            _deviceLogoBinSize.Add(logobinsize);
            _deviceLogoBinContents.Add(logoContents);
        }

        private void cboMoto_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = cboMoto.SelectedIndex;
            udResolutionX.Enabled = (idx == 0);
            udResolutionY.Enabled = (idx == 0);
            udResolutionX.Value = _deviceResolutionX[idx];
            udResolutionY.Value = _deviceResolutionY[idx];
            _maxFileSize = _deviceLogoBinSize[idx];
            init_tree(_deviceLogoBinContents[idx]);
        }
    }
}
